using Backend.Alquila.Features.Properties;

namespace Backend.Alquila.Tests;

public sealed class PropertyValidatorTests
{
    [Fact]
    public void ValidateForCreate_WithValidPayload_ReturnsNoErrors()
    {
        var request = new PropertyUpsertRequest(
            Title: "Apartamento luminoso",
            Description: "Ideal para pareja.",
            City: "Madrid",
            Neighborhood: "Chamberi",
            Address: "Calle Falsa 123",
            MonthlyPrice: 1250,
            DepositAmount: 1250,
            Bedrooms: 2,
            Bathrooms: 1,
            AreaM2: 75,
            IsFurnished: true,
            AvailableFrom: DateOnly.FromDateTime(DateTime.UtcNow.Date),
            ContractType: PropertyContractTypes.LongTerm,
            Status: PropertyStatuses.Pendiente);

        var errors = PropertyValidator.ValidateForCreate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateForCreate_WithInvalidPayload_ReturnsExpectedErrors()
    {
        var request = new PropertyUpsertRequest(
            Title: " ",
            Description: "ok",
            City: "",
            Neighborhood: "barrio",
            Address: "direccion",
            MonthlyPrice: 0,
            DepositAmount: -1,
            Bedrooms: -1,
            Bathrooms: -1,
            AreaM2: 0,
            IsFurnished: false,
            AvailableFrom: DateOnly.FromDateTime(DateTime.UtcNow.Date),
            ContractType: "invalid_contract",
            Status: "invalid_status");

        var errors = PropertyValidator.ValidateForCreate(request);

        Assert.Contains("title", errors.Keys);
        Assert.Contains("city", errors.Keys);
        Assert.Contains("monthlyPrice", errors.Keys);
        Assert.Contains("depositAmount", errors.Keys);
        Assert.Contains("bedrooms", errors.Keys);
        Assert.Contains("bathrooms", errors.Keys);
        Assert.Contains("areaM2", errors.Keys);
        Assert.Contains("contractType", errors.Keys);
        Assert.Contains("status", errors.Keys);
    }

    [Fact]
    public void ValidateForPatch_WithSubsetInvalidFields_ReturnsOnlyFieldErrors()
    {
        var patch = new PropertyPatchRequest(
            Title: null,
            Description: null,
            City: " ",
            Neighborhood: null,
            Address: null,
            MonthlyPrice: -50,
            DepositAmount: null,
            Bedrooms: null,
            Bathrooms: null,
            AreaM2: null,
            IsFurnished: null,
            AvailableFrom: null,
            ContractType: null,
            Status: null);

        var errors = PropertyValidator.ValidateForPatch(patch);

        Assert.Equal(2, errors.Count);
        Assert.Contains("city", errors.Keys);
        Assert.Contains("monthlyPrice", errors.Keys);
    }
}
