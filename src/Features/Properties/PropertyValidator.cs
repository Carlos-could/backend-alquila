namespace Backend.Alquila.Features.Properties;

public static class PropertyValidator
{
    private const int MaxTitleLength = 140;
    private const int MaxDescriptionLength = 4000;
    private const int MaxCityLength = 120;
    private const int MaxNeighborhoodLength = 120;
    private const int MaxAddressLength = 255;

    public static IReadOnlyDictionary<string, string[]> ValidateForCreate(PropertyUpsertRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        ValidateShared(
            errors,
            request.Title,
            request.Description,
            request.City,
            request.Neighborhood,
            request.Address,
            request.MonthlyPrice,
            request.DepositAmount,
            request.Bedrooms,
            request.Bathrooms,
            request.AreaM2,
            request.ContractType,
            request.Status);

        return ToReadOnlyErrors(errors);
    }

    public static IReadOnlyDictionary<string, string[]> ValidateForPatch(PropertyPatchRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (request.Title is not null)
        {
            ValidateText(errors, "title", request.Title, MaxTitleLength, required: true);
        }

        if (request.Description is not null)
        {
            ValidateText(errors, "description", request.Description, MaxDescriptionLength, required: false);
        }

        if (request.City is not null)
        {
            ValidateText(errors, "city", request.City, MaxCityLength, required: true);
        }

        if (request.Neighborhood is not null)
        {
            ValidateText(errors, "neighborhood", request.Neighborhood, MaxNeighborhoodLength, required: false);
        }

        if (request.Address is not null)
        {
            ValidateText(errors, "address", request.Address, MaxAddressLength, required: false);
        }

        if (request.MonthlyPrice is not null && request.MonthlyPrice <= 0)
        {
            AddError(errors, "monthlyPrice", "monthlyPrice must be greater than 0.");
        }

        if (request.DepositAmount is not null && request.DepositAmount < 0)
        {
            AddError(errors, "depositAmount", "depositAmount cannot be negative.");
        }

        if (request.Bedrooms is not null && request.Bedrooms < 0)
        {
            AddError(errors, "bedrooms", "bedrooms cannot be negative.");
        }

        if (request.Bathrooms is not null && request.Bathrooms < 0)
        {
            AddError(errors, "bathrooms", "bathrooms cannot be negative.");
        }

        if (request.AreaM2 is not null && request.AreaM2 <= 0)
        {
            AddError(errors, "areaM2", "areaM2 must be greater than 0.");
        }

        if (request.ContractType is not null && !PropertyContractTypes.Allowed.Contains(request.ContractType.Trim()))
        {
            AddError(errors, "contractType", $"contractType must be one of: {string.Join(", ", PropertyContractTypes.Allowed)}.");
        }

        if (request.Status is not null && !PropertyStatuses.Allowed.Contains(request.Status.Trim()))
        {
            AddError(errors, "status", $"status must be one of: {string.Join(", ", PropertyStatuses.Allowed)}.");
        }

        return ToReadOnlyErrors(errors);
    }

    private static void ValidateShared(
        IDictionary<string, List<string>> errors,
        string title,
        string description,
        string city,
        string neighborhood,
        string address,
        decimal monthlyPrice,
        decimal depositAmount,
        int bedrooms,
        int bathrooms,
        decimal areaM2,
        string contractType,
        string status)
    {
        ValidateText(errors, "title", title, MaxTitleLength, required: true);
        ValidateText(errors, "description", description, MaxDescriptionLength, required: false);
        ValidateText(errors, "city", city, MaxCityLength, required: true);
        ValidateText(errors, "neighborhood", neighborhood, MaxNeighborhoodLength, required: false);
        ValidateText(errors, "address", address, MaxAddressLength, required: false);

        if (monthlyPrice <= 0)
        {
            AddError(errors, "monthlyPrice", "monthlyPrice must be greater than 0.");
        }

        if (depositAmount < 0)
        {
            AddError(errors, "depositAmount", "depositAmount cannot be negative.");
        }

        if (bedrooms < 0)
        {
            AddError(errors, "bedrooms", "bedrooms cannot be negative.");
        }

        if (bathrooms < 0)
        {
            AddError(errors, "bathrooms", "bathrooms cannot be negative.");
        }

        if (areaM2 <= 0)
        {
            AddError(errors, "areaM2", "areaM2 must be greater than 0.");
        }

        if (!PropertyContractTypes.Allowed.Contains(contractType.Trim()))
        {
            AddError(errors, "contractType", $"contractType must be one of: {string.Join(", ", PropertyContractTypes.Allowed)}.");
        }

        if (!PropertyStatuses.Allowed.Contains(status.Trim()))
        {
            AddError(errors, "status", $"status must be one of: {string.Join(", ", PropertyStatuses.Allowed)}.");
        }
    }

    private static void ValidateText(
        IDictionary<string, List<string>> errors,
        string key,
        string value,
        int maxLength,
        bool required)
    {
        var normalized = value.Trim();

        if (required && string.IsNullOrWhiteSpace(normalized))
        {
            AddError(errors, key, $"{key} is required.");
            return;
        }

        if (normalized.Length > maxLength)
        {
            AddError(errors, key, $"{key} cannot exceed {maxLength} characters.");
        }
    }

    private static void AddError(IDictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = new List<string>();
            errors[key] = messages;
        }

        messages.Add(message);
    }

    private static IReadOnlyDictionary<string, string[]> ToReadOnlyErrors(IDictionary<string, List<string>> errors) =>
        errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
}
