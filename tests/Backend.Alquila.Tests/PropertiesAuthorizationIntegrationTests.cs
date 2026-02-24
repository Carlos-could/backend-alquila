using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Backend.Alquila.Features.Properties;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Alquila.Tests;

public sealed class PropertiesAuthorizationIntegrationTests : IClassFixture<PropertiesTestApplicationFactory>
{
    private readonly PropertiesTestApplicationFactory _factory;

    public PropertiesAuthorizationIntegrationTests(PropertiesTestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateProperty_WithPropietarioRole_ReturnsCreated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "propietario");

        var payload = new
        {
            title = "Depto en Centro",
            description = "Cerca del metro",
            city = "Madrid",
            neighborhood = "Centro",
            address = "Calle 1",
            monthlyPrice = 1000,
            depositAmount = 1000,
            bedrooms = 2,
            bathrooms = 1,
            areaM2 = 70,
            isFurnished = true,
            availableFrom = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            contractType = "long_term",
            status = "pendiente"
        };

        var response = await client.PostAsJsonAsync("/properties", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateProperty_WithInquilinoRole_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "inquilino");

        var payload = new
        {
            title = "Depto en Centro",
            description = "Cerca del metro",
            city = "Madrid",
            neighborhood = "Centro",
            address = "Calle 1",
            monthlyPrice = 1000,
            depositAmount = 1000,
            bedrooms = 2,
            bathrooms = 1,
            areaM2 = 70,
            isFurnished = true,
            availableFrom = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            contractType = "long_term",
            status = "pendiente"
        };

        var response = await client.PostAsJsonAsync("/properties", payload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PatchProperty_WithOwnerPropietario_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "propietario");

        var response = await client.PatchAsJsonAsync($"/properties/{InMemoryPropertiesRepository.OwnedPropertyId}", new
        {
            monthlyPrice = 1300,
            status = "publicado"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PatchProperty_WithDifferentPropietario_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "propietario");
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, InMemoryPropertiesRepository.NonOwnerAuthUserId.ToString());

        var response = await client.PatchAsJsonAsync($"/properties/{InMemoryPropertiesRepository.OwnedPropertyId}", new
        {
            monthlyPrice = 1400
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PatchProperty_WithAdminRole_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "admin");
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, InMemoryPropertiesRepository.NonOwnerAuthUserId.ToString());

        var response = await client.PatchAsJsonAsync($"/properties/{InMemoryPropertiesRepository.OwnedPropertyId}", new
        {
            status = "rechazado"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public sealed class PropertiesTestApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("SUPABASE_URL", "https://example.supabase.co");
        Environment.SetEnvironmentVariable("SUPABASE_ANON_KEY", "test-anon-key");

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.RemoveAll<IPropertiesRepository>();
            services.AddSingleton<IPropertiesRepository, InMemoryPropertiesRepository>();
        });
    }
}

public sealed class InMemoryPropertiesRepository : IPropertiesRepository
{
    public static readonly Guid OwnerAuthUserId = Guid.Parse(TestAuthHandler.DefaultUserId);
    public static readonly Guid NonOwnerAuthUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid OwnerUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid NonOwnerUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid OwnedPropertyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly object _gate = new();
    private readonly Dictionary<Guid, Guid> _usersByAuthUserId = new();
    private readonly Dictionary<Guid, PropertyRecord> _propertiesById = new();

    public InMemoryPropertiesRepository()
    {
        _usersByAuthUserId[OwnerAuthUserId] = OwnerUserId;
        _usersByAuthUserId[NonOwnerAuthUserId] = NonOwnerUserId;

        _propertiesById[OwnedPropertyId] = new PropertyRecord(
            Id: OwnedPropertyId,
            OwnerUserId: OwnerUserId,
            Title: "Inicial",
            Description: "Desc",
            City: "Madrid",
            Neighborhood: "Centro",
            Address: "Calle 1",
            MonthlyPrice: 1000,
            DepositAmount: 1000,
            Bedrooms: 2,
            Bathrooms: 1,
            AreaM2: 70,
            IsFurnished: true,
            AvailableFrom: DateOnly.FromDateTime(DateTime.UtcNow.Date),
            ContractType: "long_term",
            Status: "pendiente",
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow);
    }

    public Task<Guid?> FindUserIdByAuthUserIdAsync(Guid authUserId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_usersByAuthUserId.TryGetValue(authUserId, out var userId)
                ? (Guid?)userId
                : null);
        }
    }

    public Task<PropertyRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_propertiesById.TryGetValue(id, out var property)
                ? property
                : null);
        }
    }

    public Task<PropertyRecord> CreateAsync(Guid ownerUserId, NormalizedPropertyCreateInput input, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            var created = new PropertyRecord(
                Id: Guid.NewGuid(),
                OwnerUserId: ownerUserId,
                Title: input.Title,
                Description: input.Description,
                City: input.City,
                Neighborhood: input.Neighborhood,
                Address: input.Address,
                MonthlyPrice: input.MonthlyPrice,
                DepositAmount: input.DepositAmount,
                Bedrooms: input.Bedrooms,
                Bathrooms: input.Bathrooms,
                AreaM2: input.AreaM2,
                IsFurnished: input.IsFurnished,
                AvailableFrom: input.AvailableFrom,
                ContractType: input.ContractType,
                Status: input.Status,
                CreatedAt: now,
                UpdatedAt: now);

            _propertiesById[created.Id] = created;
            return Task.FromResult(created);
        }
    }

    public Task<PropertyRecord?> UpdateAsync(Guid id, NormalizedPropertyPatchInput input, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_propertiesById.TryGetValue(id, out var current))
            {
                return Task.FromResult<PropertyRecord?>(null);
            }

            var updated = current with
            {
                Title = input.Title ?? current.Title,
                Description = input.Description ?? current.Description,
                City = input.City ?? current.City,
                Neighborhood = input.Neighborhood ?? current.Neighborhood,
                Address = input.Address ?? current.Address,
                MonthlyPrice = input.MonthlyPrice ?? current.MonthlyPrice,
                DepositAmount = input.DepositAmount ?? current.DepositAmount,
                Bedrooms = input.Bedrooms ?? current.Bedrooms,
                Bathrooms = input.Bathrooms ?? current.Bathrooms,
                AreaM2 = input.AreaM2 ?? current.AreaM2,
                IsFurnished = input.IsFurnished ?? current.IsFurnished,
                AvailableFrom = input.AvailableFrom ?? current.AvailableFrom,
                ContractType = input.ContractType ?? current.ContractType,
                Status = input.Status ?? current.Status,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _propertiesById[id] = updated;
            return Task.FromResult<PropertyRecord?>(updated);
        }
    }
}

