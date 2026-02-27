using System.Security.Claims;
using Backend.Alquila.Features.Auth;

namespace Backend.Alquila.Features.Properties;

public static class PropertyEndpoints
{
    private const int MaxImagesPerProperty = 15;
    private const int MaxImageSizeBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public static IEndpointRouteBuilder MapPropertyEndpoints(this IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup("/properties");
        publicGroup.MapGet("/public", ListPublicPropertiesAsync);

        var group = app.MapGroup("/properties").RequireAuthorization();

        group.MapPost("/", CreatePropertyAsync);
        group.MapPatch("/{id:guid}", PatchPropertyAsync);
        group.MapGet("/moderation/pending", ListPendingModerationAsync);
        group.MapPatch("/{id:guid}/moderation", ModeratePropertyStatusAsync);
        group.MapGet("/{id:guid}/status-history", GetPropertyStatusHistoryAsync);
        group.MapGet("/{id:guid}/images", GetPropertyImagesAsync);
        group.MapPost("/{id:guid}/images", UploadPropertyImagesAsync).DisableAntiforgery();
        group.MapPatch("/{id:guid}/images/order", PatchPropertyImageOrderAsync);

        return app;
    }

    private static async Task<IResult> CreatePropertyAsync(
        ClaimsPrincipal user,
        PropertyUpsertRequest request,
        IPropertiesRepository repository,
        CancellationToken cancellationToken)
    {
        if (!RoleClaimResolver.IsInAnyRole(user, UserRoles.Propietario, UserRoles.Admin))
        {
            return Results.Forbid();
        }

        var errors = PropertyValidator.ValidateForCreate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        if (!TryGetAuthUserId(user, out var authUserId))
        {
            return Results.Unauthorized();
        }

        var ownerUserId = await repository.FindUserIdByAuthUserIdAsync(authUserId, cancellationToken);
        if (!ownerUserId.HasValue)
        {
            return Results.Problem(
                title: "Missing user profile",
                detail: "Authenticated user does not have an internal profile in users table.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var created = await repository.CreateAsync(
            ownerUserId.Value,
            PropertyMappings.NormalizeForCreate(request),
            cancellationToken);

        return Results.Created($"/properties/{created.Id}", PropertyMappings.ToResponse(created));
    }

    private static async Task<IResult> PatchPropertyAsync(
        Guid id,
        ClaimsPrincipal user,
        PropertyPatchRequest request,
        IPropertiesRepository repository,
        CancellationToken cancellationToken)
    {
        if (!RoleClaimResolver.IsInAnyRole(user, UserRoles.Propietario, UserRoles.Admin))
        {
            return Results.Forbid();
        }

        var errors = PropertyValidator.ValidateForPatch(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var normalizedPatch = PropertyMappings.NormalizeForPatch(request);
        if (!normalizedPatch.HasAnyField)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = new[] { "At least one field is required for patch." }
            });
        }

        var current = await repository.GetByIdAsync(id, cancellationToken);
        if (current is null)
        {
            return Results.NotFound();
        }

        var isAdmin = RoleClaimResolver.IsInAnyRole(user, UserRoles.Admin);
        if (!isAdmin)
        {
            if (!TryGetAuthUserId(user, out var authUserId))
            {
                return Results.Unauthorized();
            }

            var ownerUserId = await repository.FindUserIdByAuthUserIdAsync(authUserId, cancellationToken);
            if (!ownerUserId.HasValue || ownerUserId.Value != current.OwnerUserId)
            {
                return Results.Forbid();
            }
        }

        var updated = await repository.UpdateAsync(id, normalizedPatch, cancellationToken);
        if (updated is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(PropertyMappings.ToResponse(updated));
    }

    private static async Task<IResult> ListPendingModerationAsync(
        ClaimsPrincipal user,
        IPropertiesRepository repository,
        CancellationToken cancellationToken)
    {
        if (!RoleClaimResolver.IsInAnyRole(user, UserRoles.Admin))
        {
            return Results.Forbid();
        }

        var pending = await repository.ListPendingModerationAsync(cancellationToken);
        return Results.Ok(pending);
    }

    private static async Task<IResult> ModeratePropertyStatusAsync(
        Guid id,
        ClaimsPrincipal user,
        PropertyModerationRequest request,
        IPropertiesRepository repository,
        CancellationToken cancellationToken)
    {
        if (!RoleClaimResolver.IsInAnyRole(user, UserRoles.Admin))
        {
            return Results.Forbid();
        }

        var normalizedStatus = request.Status.Trim().ToLowerInvariant();
        if (normalizedStatus is not PropertyStatuses.Publicado and not PropertyStatuses.Rechazado)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = new[] { "status must be one of: publicado, rechazado." }
            });
        }

        Guid? changedByUserId = null;
        if (TryGetAuthUserId(user, out var authUserId))
        {
            changedByUserId = await repository.FindUserIdByAuthUserIdAsync(authUserId, cancellationToken);
        }

        var updated = await repository.UpdateStatusAsync(
            propertyId: id,
            newStatus: normalizedStatus,
            changedByUserId: changedByUserId,
            changedByRole: UserRoles.Admin,
            reason: string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            cancellationToken: cancellationToken);

        if (updated is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(PropertyMappings.ToResponse(updated));
    }

    private static async Task<IResult> GetPropertyStatusHistoryAsync(
        Guid id,
        ClaimsPrincipal user,
        IPropertiesRepository repository,
        CancellationToken cancellationToken)
    {
        if (!RoleClaimResolver.IsInAnyRole(user, UserRoles.Admin))
        {
            return Results.Forbid();
        }

        var property = await repository.GetByIdAsync(id, cancellationToken);
        if (property is null)
        {
            return Results.NotFound();
        }

        var history = await repository.GetStatusHistoryAsync(id, cancellationToken);
        return Results.Ok(history.Select(PropertyMappings.ToStatusHistoryResponse));
    }

    private static async Task<IResult> ListPublicPropertiesAsync(
        IPropertiesRepository repository,
        CancellationToken cancellationToken)
    {
        var items = await repository.ListPublishedForPublicAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetPropertyImagesAsync(
        Guid id,
        IPropertiesRepository repository,
        CancellationToken cancellationToken)
    {
        var property = await repository.GetByIdAsync(id, cancellationToken);
        if (property is null)
        {
            return Results.NotFound();
        }

        var images = await repository.GetImagesByPropertyIdAsync(id, cancellationToken);
        return Results.Ok(images.Select(PropertyMappings.ToImageResponse));
    }

    private static async Task<IResult> UploadPropertyImagesAsync(
        Guid id,
        HttpRequest request,
        ClaimsPrincipal user,
        IPropertiesRepository repository,
        IPropertyImageStorage imageStorage,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await EnsurePropertyWriteAccessAsync(id, user, repository, cancellationToken);
        if (authorizationResult.Result is not null)
        {
            return authorizationResult.Result;
        }

        var form = await request.ReadFormAsync(cancellationToken);
        if (form.Files.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["files"] = new[] { "At least one image file is required." }
            });
        }

        var validationErrors = ValidateImageFiles(form.Files);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var existingCount = await repository.CountImagesByPropertyIdAsync(id, cancellationToken);
        if (existingCount + form.Files.Count > MaxImagesPerProperty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["files"] = new[] { $"Maximum {MaxImagesPerProperty} images per property." }
            });
        }

        var newImages = new List<NewPropertyImageInput>(form.Files.Count);
        var order = existingCount;
        foreach (var file in form.Files)
        {
            var stored = await imageStorage.SaveAsync(id, file, cancellationToken);
            newImages.Add(new NewPropertyImageInput(
                StoragePath: stored.StoragePath,
                PublicUrl: stored.PublicUrl,
                MimeType: stored.MimeType,
                FileSizeBytes: stored.FileSizeBytes,
                DisplayOrder: order));
            order++;
        }

        var created = await repository.AddImagesAsync(id, newImages, cancellationToken);
        return Results.Ok(created.Select(PropertyMappings.ToImageResponse));
    }

    private static async Task<IResult> PatchPropertyImageOrderAsync(
        Guid id,
        PropertyImageOrderPatchRequest request,
        ClaimsPrincipal user,
        IPropertiesRepository repository,
        CancellationToken cancellationToken)
    {
        var authorizationResult = await EnsurePropertyWriteAccessAsync(id, user, repository, cancellationToken);
        if (authorizationResult.Result is not null)
        {
            return authorizationResult.Result;
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = new[] { "At least one order item is required." }
            });
        }

        if (request.Items.Any(item => item.DisplayOrder < 0))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["displayOrder"] = new[] { "displayOrder cannot be negative." }
            });
        }

        if (request.Items.Select(item => item.ImageId).Distinct().Count() != request.Items.Count)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = new[] { "imageId values must be unique." }
            });
        }

        if (request.Items.Select(item => item.DisplayOrder).Distinct().Count() != request.Items.Count)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = new[] { "displayOrder values must be unique." }
            });
        }

        try
        {
            var reordered = await repository.UpdateImageOrderAsync(id, request.Items, cancellationToken);
            return Results.Ok(reordered.Select(PropertyMappings.ToImageResponse));
        }
        catch (InvalidOperationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = new[] { exception.Message }
            });
        }
    }

    private static Dictionary<string, string[]> ValidateImageFiles(IFormFileCollection files)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (files.Count > MaxImagesPerProperty)
        {
            errors["files"] = new List<string> { $"You can upload at most {MaxImagesPerProperty} images per request." };
        }

        foreach (var file in files)
        {
            if (!AllowedMimeTypes.Contains(file.ContentType))
            {
                AddValidationError(errors, file.FileName, "Unsupported format. Allowed: image/jpeg, image/png, image/webp.");
            }

            if (file.Length <= 0)
            {
                AddValidationError(errors, file.FileName, "Empty file is not allowed.");
            }

            if (file.Length > MaxImageSizeBytes)
            {
                AddValidationError(errors, file.FileName, $"File exceeds max size of {MaxImageSizeBytes / (1024 * 1024)} MB.");
            }
        }

        return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    private static void AddValidationError(IDictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = new List<string>();
            errors[key] = messages;
        }

        messages.Add(message);
    }

    private static async Task<PropertyWriteAccessResult> EnsurePropertyWriteAccessAsync(
        Guid propertyId,
        ClaimsPrincipal user,
        IPropertiesRepository repository,
        CancellationToken cancellationToken)
    {
        if (!RoleClaimResolver.IsInAnyRole(user, UserRoles.Propietario, UserRoles.Admin))
        {
            return new PropertyWriteAccessResult(Results.Forbid(), null);
        }

        var property = await repository.GetByIdAsync(propertyId, cancellationToken);
        if (property is null)
        {
            return new PropertyWriteAccessResult(Results.NotFound(), null);
        }

        var isAdmin = RoleClaimResolver.IsInAnyRole(user, UserRoles.Admin);
        if (isAdmin)
        {
            return new PropertyWriteAccessResult(null, property);
        }

        if (!TryGetAuthUserId(user, out var authUserId))
        {
            return new PropertyWriteAccessResult(Results.Unauthorized(), null);
        }

        var ownerUserId = await repository.FindUserIdByAuthUserIdAsync(authUserId, cancellationToken);
        if (!ownerUserId.HasValue || ownerUserId.Value != property.OwnerUserId)
        {
            return new PropertyWriteAccessResult(Results.Forbid(), null);
        }

        return new PropertyWriteAccessResult(null, property);
    }

    private static bool TryGetAuthUserId(ClaimsPrincipal user, out Guid authUserId)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claimValue, out authUserId);
    }

    private sealed record PropertyWriteAccessResult(IResult? Result, PropertyRecord? Property);
}
