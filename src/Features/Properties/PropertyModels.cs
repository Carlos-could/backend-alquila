namespace Backend.Alquila.Features.Properties;

public sealed record PropertyRecord(
    Guid Id,
    Guid OwnerUserId,
    string Title,
    string? Description,
    string City,
    string? Neighborhood,
    string? Address,
    decimal MonthlyPrice,
    decimal DepositAmount,
    int Bedrooms,
    int Bathrooms,
    decimal AreaM2,
    bool IsFurnished,
    DateOnly AvailableFrom,
    string ContractType,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PropertyResponse(
    Guid Id,
    Guid OwnerUserId,
    string Title,
    string? Description,
    string City,
    string? Neighborhood,
    string? Address,
    decimal MonthlyPrice,
    decimal DepositAmount,
    int Bedrooms,
    int Bathrooms,
    decimal AreaM2,
    bool IsFurnished,
    DateOnly AvailableFrom,
    string ContractType,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record NormalizedPropertyCreateInput(
    string Title,
    string? Description,
    string City,
    string? Neighborhood,
    string? Address,
    decimal MonthlyPrice,
    decimal DepositAmount,
    int Bedrooms,
    int Bathrooms,
    decimal AreaM2,
    bool IsFurnished,
    DateOnly AvailableFrom,
    string ContractType,
    string Status);

public sealed record NormalizedPropertyPatchInput(
    string? Title,
    string? Description,
    string? City,
    string? Neighborhood,
    string? Address,
    decimal? MonthlyPrice,
    decimal? DepositAmount,
    int? Bedrooms,
    int? Bathrooms,
    decimal? AreaM2,
    bool? IsFurnished,
    DateOnly? AvailableFrom,
    string? ContractType,
    string? Status)
{
    public bool HasAnyField =>
        Title is not null ||
        Description is not null ||
        City is not null ||
        Neighborhood is not null ||
        Address is not null ||
        MonthlyPrice is not null ||
        DepositAmount is not null ||
        Bedrooms is not null ||
        Bathrooms is not null ||
        AreaM2 is not null ||
        IsFurnished is not null ||
        AvailableFrom is not null ||
        ContractType is not null ||
        Status is not null;
}

public sealed record NewPropertyImageInput(
    string StoragePath,
    string PublicUrl,
    string MimeType,
    int FileSizeBytes,
    int DisplayOrder);

public sealed record PropertyImageRecord(
    Guid Id,
    Guid PropertyId,
    string StoragePath,
    string PublicUrl,
    string MimeType,
    int FileSizeBytes,
    int DisplayOrder,
    DateTimeOffset CreatedAt);

public sealed record PropertyImageResponse(
    Guid Id,
    Guid PropertyId,
    string PublicUrl,
    string MimeType,
    int FileSizeBytes,
    int DisplayOrder,
    DateTimeOffset CreatedAt);

public sealed record PropertyImageOrderItemRequest(
    Guid ImageId,
    int DisplayOrder);

public sealed record PropertyImageOrderPatchRequest(
    IReadOnlyList<PropertyImageOrderItemRequest> Items);

public static class PropertyMappings
{
    public static NormalizedPropertyCreateInput NormalizeForCreate(PropertyUpsertRequest request) =>
        new(
            Title: request.Title.Trim(),
            Description: OptionalText(request.Description),
            City: request.City.Trim(),
            Neighborhood: OptionalText(request.Neighborhood),
            Address: OptionalText(request.Address),
            MonthlyPrice: request.MonthlyPrice,
            DepositAmount: request.DepositAmount,
            Bedrooms: request.Bedrooms,
            Bathrooms: request.Bathrooms,
            AreaM2: request.AreaM2,
            IsFurnished: request.IsFurnished,
            AvailableFrom: request.AvailableFrom,
            ContractType: request.ContractType.Trim().ToLowerInvariant(),
            Status: request.Status.Trim().ToLowerInvariant());

    public static NormalizedPropertyPatchInput NormalizeForPatch(PropertyPatchRequest request) =>
        new(
            Title: request.Title?.Trim(),
            Description: request.Description is null ? null : OptionalText(request.Description),
            City: request.City?.Trim(),
            Neighborhood: request.Neighborhood is null ? null : OptionalText(request.Neighborhood),
            Address: request.Address is null ? null : OptionalText(request.Address),
            MonthlyPrice: request.MonthlyPrice,
            DepositAmount: request.DepositAmount,
            Bedrooms: request.Bedrooms,
            Bathrooms: request.Bathrooms,
            AreaM2: request.AreaM2,
            IsFurnished: request.IsFurnished,
            AvailableFrom: request.AvailableFrom,
            ContractType: request.ContractType?.Trim().ToLowerInvariant(),
            Status: request.Status?.Trim().ToLowerInvariant());

    public static PropertyResponse ToResponse(PropertyRecord property) =>
        new(
            property.Id,
            property.OwnerUserId,
            property.Title,
            property.Description,
            property.City,
            property.Neighborhood,
            property.Address,
            property.MonthlyPrice,
            property.DepositAmount,
            property.Bedrooms,
            property.Bathrooms,
            property.AreaM2,
            property.IsFurnished,
            property.AvailableFrom,
            property.ContractType,
            property.Status,
            property.CreatedAt,
            property.UpdatedAt);

    public static PropertyImageResponse ToImageResponse(PropertyImageRecord image) =>
        new(
            image.Id,
            image.PropertyId,
            image.PublicUrl,
            image.MimeType,
            image.FileSizeBytes,
            image.DisplayOrder,
            image.CreatedAt);

    private static string? OptionalText(string raw)
    {
        var normalized = raw.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
