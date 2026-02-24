using Npgsql;

namespace Backend.Alquila.Features.Properties;

public sealed class NpgsqlPropertiesRepository : IPropertiesRepository
{
    private readonly string _connectionString;

    public NpgsqlPropertiesRepository()
    {
        if (!Infrastructure.Persistence.Database.DatabaseUrlResolver.TryResolve(out var connectionString, out var errorMessage))
        {
            throw new InvalidOperationException($"Properties repository could not resolve DATABASE_URL. {errorMessage}");
        }

        _connectionString = connectionString;
    }

    public async Task<Guid?> FindUserIdByAuthUserIdAsync(Guid authUserId, CancellationToken cancellationToken)
    {
        const string sql = """
            select id
            from public.users
            where auth_user_id = @authUserId
            limit 1;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("authUserId", authUserId);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is Guid id ? id : null;
    }

    public async Task<PropertyRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, owner_user_id, title, description, city, neighborhood, address,
                   monthly_price, deposit_amount, bedrooms, bathrooms, area_m2,
                   is_furnished, available_from, contract_type, status,
                   created_at, updated_at
            from public.properties
            where id = @id
            limit 1;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadProperty(reader);
    }

    public async Task<PropertyRecord> CreateAsync(
        Guid ownerUserId,
        NormalizedPropertyCreateInput input,
        CancellationToken cancellationToken)
    {
        const string sql = """
            insert into public.properties (
                owner_user_id,
                title,
                description,
                city,
                neighborhood,
                address,
                monthly_price,
                deposit_amount,
                bedrooms,
                bathrooms,
                area_m2,
                is_furnished,
                available_from,
                contract_type,
                status
            )
            values (
                @ownerUserId,
                @title,
                @description,
                @city,
                @neighborhood,
                @address,
                @monthlyPrice,
                @depositAmount,
                @bedrooms,
                @bathrooms,
                @areaM2,
                @isFurnished,
                @availableFrom,
                @contractType,
                @status
            )
            returning id, owner_user_id, title, description, city, neighborhood, address,
                      monthly_price, deposit_amount, bedrooms, bathrooms, area_m2,
                      is_furnished, available_from, contract_type, status,
                      created_at, updated_at;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddCreateParameters(command, ownerUserId, input);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return ReadProperty(reader);
    }

    public async Task<PropertyRecord?> UpdateAsync(Guid id, NormalizedPropertyPatchInput input, CancellationToken cancellationToken)
    {
        var setClauses = new List<string>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand { Connection = connection };

        if (input.Title is not null)
        {
            setClauses.Add("title = @title");
            command.Parameters.AddWithValue("title", input.Title);
        }

        if (input.Description is not null)
        {
            setClauses.Add("description = @description");
            command.Parameters.AddWithValue("description", (object?)input.Description ?? DBNull.Value);
        }

        if (input.City is not null)
        {
            setClauses.Add("city = @city");
            command.Parameters.AddWithValue("city", input.City);
        }

        if (input.Neighborhood is not null)
        {
            setClauses.Add("neighborhood = @neighborhood");
            command.Parameters.AddWithValue("neighborhood", (object?)input.Neighborhood ?? DBNull.Value);
        }

        if (input.Address is not null)
        {
            setClauses.Add("address = @address");
            command.Parameters.AddWithValue("address", (object?)input.Address ?? DBNull.Value);
        }

        if (input.MonthlyPrice is not null)
        {
            setClauses.Add("monthly_price = @monthlyPrice");
            command.Parameters.AddWithValue("monthlyPrice", input.MonthlyPrice.Value);
        }

        if (input.DepositAmount is not null)
        {
            setClauses.Add("deposit_amount = @depositAmount");
            command.Parameters.AddWithValue("depositAmount", input.DepositAmount.Value);
        }

        if (input.Bedrooms is not null)
        {
            setClauses.Add("bedrooms = @bedrooms");
            command.Parameters.AddWithValue("bedrooms", input.Bedrooms.Value);
        }

        if (input.Bathrooms is not null)
        {
            setClauses.Add("bathrooms = @bathrooms");
            command.Parameters.AddWithValue("bathrooms", input.Bathrooms.Value);
        }

        if (input.AreaM2 is not null)
        {
            setClauses.Add("area_m2 = @areaM2");
            command.Parameters.AddWithValue("areaM2", input.AreaM2.Value);
        }

        if (input.IsFurnished is not null)
        {
            setClauses.Add("is_furnished = @isFurnished");
            command.Parameters.AddWithValue("isFurnished", input.IsFurnished.Value);
        }

        if (input.AvailableFrom is not null)
        {
            setClauses.Add("available_from = @availableFrom");
            command.Parameters.AddWithValue("availableFrom", input.AvailableFrom.Value);
        }

        if (input.ContractType is not null)
        {
            setClauses.Add("contract_type = @contractType");
            command.Parameters.AddWithValue("contractType", input.ContractType);
        }

        if (input.Status is not null)
        {
            setClauses.Add("status = @status");
            command.Parameters.AddWithValue("status", input.Status);
        }

        if (setClauses.Count == 0)
        {
            return await GetByIdAsync(id, cancellationToken);
        }

        setClauses.Add("updated_at = now()");

        command.Parameters.AddWithValue("id", id);
        command.CommandText = $"""
            update public.properties
            set {string.Join(", ", setClauses)}
            where id = @id
            returning id, owner_user_id, title, description, city, neighborhood, address,
                      monthly_price, deposit_amount, bedrooms, bathrooms, area_m2,
                      is_furnished, available_from, contract_type, status,
                      created_at, updated_at;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadProperty(reader);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static void AddCreateParameters(NpgsqlCommand command, Guid ownerUserId, NormalizedPropertyCreateInput input)
    {
        command.Parameters.AddWithValue("ownerUserId", ownerUserId);
        command.Parameters.AddWithValue("title", input.Title);
        command.Parameters.AddWithValue("description", (object?)input.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("city", input.City);
        command.Parameters.AddWithValue("neighborhood", (object?)input.Neighborhood ?? DBNull.Value);
        command.Parameters.AddWithValue("address", (object?)input.Address ?? DBNull.Value);
        command.Parameters.AddWithValue("monthlyPrice", input.MonthlyPrice);
        command.Parameters.AddWithValue("depositAmount", input.DepositAmount);
        command.Parameters.AddWithValue("bedrooms", input.Bedrooms);
        command.Parameters.AddWithValue("bathrooms", input.Bathrooms);
        command.Parameters.AddWithValue("areaM2", input.AreaM2);
        command.Parameters.AddWithValue("isFurnished", input.IsFurnished);
        command.Parameters.AddWithValue("availableFrom", input.AvailableFrom);
        command.Parameters.AddWithValue("contractType", input.ContractType);
        command.Parameters.AddWithValue("status", input.Status);
    }

    private static PropertyRecord ReadProperty(NpgsqlDataReader reader)
    {
        return new PropertyRecord(
            Id: reader.GetGuid(0),
            OwnerUserId: reader.GetGuid(1),
            Title: reader.GetString(2),
            Description: reader.IsDBNull(3) ? null : reader.GetString(3),
            City: reader.GetString(4),
            Neighborhood: reader.IsDBNull(5) ? null : reader.GetString(5),
            Address: reader.IsDBNull(6) ? null : reader.GetString(6),
            MonthlyPrice: reader.GetDecimal(7),
            DepositAmount: reader.GetDecimal(8),
            Bedrooms: reader.GetInt32(9),
            Bathrooms: reader.GetInt32(10),
            AreaM2: reader.GetDecimal(11),
            IsFurnished: reader.GetBoolean(12),
            AvailableFrom: reader.GetFieldValue<DateOnly>(13),
            ContractType: reader.GetString(14),
            Status: reader.GetString(15),
            CreatedAt: reader.GetFieldValue<DateTimeOffset>(16),
            UpdatedAt: reader.GetFieldValue<DateTimeOffset>(17));
    }
}
