namespace Backend.Alquila.Features.Properties;

public static class PropertyContractTypes
{
    public const string LongTerm = "long_term";
    public const string Temporary = "temporary";
    public const string Monthly = "monthly";

    public static readonly ISet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        LongTerm,
        Temporary,
        Monthly
    };
}

public static class PropertyStatuses
{
    public const string Pendiente = "pendiente";
    public const string Publicado = "publicado";
    public const string Rechazado = "rechazado";

    public static readonly ISet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Pendiente,
        Publicado,
        Rechazado
    };
}

public sealed record PropertyUpsertRequest(
    string Title,
    string Description,
    string City,
    string Neighborhood,
    string Address,
    decimal MonthlyPrice,
    decimal DepositAmount,
    int Bedrooms,
    int Bathrooms,
    decimal AreaM2,
    bool IsFurnished,
    DateOnly AvailableFrom,
    string ContractType,
    string Status);

public sealed record PropertyPatchRequest(
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
    string? Status);
