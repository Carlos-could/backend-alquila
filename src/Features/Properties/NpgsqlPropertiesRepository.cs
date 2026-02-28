using Npgsql;

namespace Backend.Alquila.Features.Properties;

public sealed class NpgsqlPropertiesRepository : IPropertiesRepository
{
    private const int MaxTransientRetries = 2;
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
        return await ExecuteWithTransientRetryAsync(async token =>
        {
            const string sql = """
                select id
                from public.users
                where auth_user_id = @authUserId
                limit 1;
                """;

            await using var connection = await OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("authUserId", authUserId);

            var scalar = await command.ExecuteScalarAsync(token);
            return scalar is Guid id ? (Guid?)id : null;
        }, cancellationToken);
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
        return await ExecuteWithTransientRetryAsync(async token =>
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

            await using var connection = await OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand(sql, connection);
            AddCreateParameters(command, ownerUserId, input);

            await using var reader = await command.ExecuteReaderAsync(token);
            await reader.ReadAsync(token);
            return ReadProperty(reader);
        }, cancellationToken);
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

    public async Task<IReadOnlyList<PropertyImageRecord>> GetImagesByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, property_id, storage_path, public_url, mime_type, file_size_bytes, display_order, created_at
            from public.property_images
            where property_id = @propertyId
            order by display_order asc, created_at asc;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("propertyId", propertyId);

        var images = new List<PropertyImageRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            images.Add(ReadImage(reader));
        }

        return images;
    }

    public async Task<int> CountImagesByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        const string sql = """
            select count(*)
            from public.property_images
            where property_id = @propertyId;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("propertyId", propertyId);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is long count ? checked((int)count) : 0;
    }

    public async Task<IReadOnlyList<PropertyImageRecord>> AddImagesAsync(
        Guid propertyId,
        IReadOnlyList<NewPropertyImageInput> images,
        CancellationToken cancellationToken)
    {
        if (images.Count == 0)
        {
            return Array.Empty<PropertyImageRecord>();
        }

        const string sql = """
            insert into public.property_images (
                property_id,
                storage_path,
                public_url,
                mime_type,
                file_size_bytes,
                display_order
            )
            values (
                @propertyId,
                @storagePath,
                @publicUrl,
                @mimeType,
                @fileSizeBytes,
                @displayOrder
            )
            returning id, property_id, storage_path, public_url, mime_type, file_size_bytes, display_order, created_at;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var created = new List<PropertyImageRecord>(images.Count);
        foreach (var image in images)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("propertyId", propertyId);
            command.Parameters.AddWithValue("storagePath", image.StoragePath);
            command.Parameters.AddWithValue("publicUrl", image.PublicUrl);
            command.Parameters.AddWithValue("mimeType", image.MimeType);
            command.Parameters.AddWithValue("fileSizeBytes", image.FileSizeBytes);
            command.Parameters.AddWithValue("displayOrder", image.DisplayOrder);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            created.Add(ReadImage(reader));
        }

        await transaction.CommitAsync(cancellationToken);
        return created;
    }

    public async Task<IReadOnlyList<PropertyImageRecord>> UpdateImageOrderAsync(
        Guid propertyId,
        IReadOnlyList<PropertyImageOrderItemRequest> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return await GetImagesByPropertyIdAsync(propertyId, cancellationToken);
        }

        const string sql = """
            update public.property_images
            set display_order = @displayOrder
            where id = @imageId and property_id = @propertyId;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var item in items)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("displayOrder", item.DisplayOrder);
            command.Parameters.AddWithValue("imageId", item.ImageId);
            command.Parameters.AddWithValue("propertyId", propertyId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                throw new InvalidOperationException($"Image '{item.ImageId}' does not belong to property '{propertyId}'.");
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetImagesByPropertyIdAsync(propertyId, cancellationToken);
    }

    public async Task<IReadOnlyList<PropertyModerationQueueItemResponse>> ListPendingModerationAsync(CancellationToken cancellationToken)
    {
        return await ExecuteWithTransientRetryAsync(async token =>
        {
            const string sql = """
                select id, owner_user_id, title, city, status, updated_at
                from public.properties
                where status = 'pendiente'
                order by updated_at desc;
                """;

            await using var connection = await OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(token);

            var items = new List<PropertyModerationQueueItemResponse>();
            while (await reader.ReadAsync(token))
            {
                items.Add(new PropertyModerationQueueItemResponse(
                    Id: reader.GetGuid(0),
                    OwnerUserId: reader.GetGuid(1),
                    Title: reader.GetString(2),
                    City: reader.GetString(3),
                    Status: reader.GetString(4),
                    UpdatedAt: reader.GetFieldValue<DateTimeOffset>(5)));
            }

            return (IReadOnlyList<PropertyModerationQueueItemResponse>)items;
        }, cancellationToken);
    }

    public async Task<PublicPropertySearchResponse> SearchPublishedForPublicAsync(
        PublicPropertySearchParams searchParams,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithTransientRetryAsync(async token =>
        {
            var whereClauses = new List<string> { "p.status = 'publicado'" };

            await using var connection = await OpenConnectionAsync(token);

            if (!string.IsNullOrWhiteSpace(searchParams.City))
            {
                whereClauses.Add("lower(p.city) = lower(@city)");
            }

            if (searchParams.MinPrice.HasValue)
            {
                whereClauses.Add("p.monthly_price >= @minPrice");
            }

            if (searchParams.MaxPrice.HasValue)
            {
                whereClauses.Add("p.monthly_price <= @maxPrice");
            }

            if (searchParams.Bedrooms.HasValue)
            {
                whereClauses.Add("p.bedrooms = @bedrooms");
            }

            if (searchParams.IsFurnished.HasValue)
            {
                whereClauses.Add("p.is_furnished = @isFurnished");
            }

            var whereSql = string.Join(" and ", whereClauses);
            var orderSql = searchParams.Sort switch
            {
                PublicPropertySortOptions.PriceAsc => "p.monthly_price asc, p.updated_at desc",
                PublicPropertySortOptions.PriceDesc => "p.monthly_price desc, p.updated_at desc",
                _ => "p.updated_at desc"
            };

            const string countSqlTemplate = """
                select count(*)
                from public.properties p
                where __WHERE__;
                """;

            const string dataSqlTemplate = """
                select p.id, p.title, p.description, p.city, p.neighborhood, p.address,
                       p.monthly_price, p.bedrooms, p.bathrooms,
                       (
                         select i.public_url
                         from public.property_images i
                         where i.property_id = p.id
                         order by i.display_order asc, i.created_at asc
                         limit 1
                       ) as cover_image_url
                from public.properties p
                where __WHERE__
                order by __ORDER__
                limit @limit
                offset @offset;
                """;

            await using var countCommand = new NpgsqlCommand(countSqlTemplate.Replace("__WHERE__", whereSql), connection);
            await using var dataCommand = new NpgsqlCommand(
                dataSqlTemplate.Replace("__WHERE__", whereSql).Replace("__ORDER__", orderSql),
                connection);

            AddPublicSearchParameters(countCommand, searchParams);
            AddPublicSearchParameters(dataCommand, searchParams);
            dataCommand.Parameters.AddWithValue("limit", searchParams.PageSize);
            dataCommand.Parameters.AddWithValue("offset", (searchParams.Page - 1) * searchParams.PageSize);

            var countScalar = await countCommand.ExecuteScalarAsync(token);
            var totalItems = countScalar is long count ? checked((int)count) : 0;

            var items = new List<PublicPropertyListItemResponse>();
            await using (var reader = await dataCommand.ExecuteReaderAsync(token))
            {
                while (await reader.ReadAsync(token))
                {
                    items.Add(new PublicPropertyListItemResponse(
                        Id: reader.GetGuid(0),
                        Title: reader.GetString(1),
                        Description: reader.IsDBNull(2) ? null : reader.GetString(2),
                        City: reader.GetString(3),
                        Neighborhood: reader.IsDBNull(4) ? null : reader.GetString(4),
                        Address: reader.IsDBNull(5) ? null : reader.GetString(5),
                        MonthlyPrice: reader.GetDecimal(6),
                        Bedrooms: reader.GetInt32(7),
                        Bathrooms: reader.GetInt32(8),
                        CoverImageUrl: reader.IsDBNull(9) ? null : reader.GetString(9)));
                }
            }

            var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)searchParams.PageSize);
            return new PublicPropertySearchResponse(
                Items: items,
                Page: searchParams.Page,
                PageSize: searchParams.PageSize,
                TotalItems: totalItems,
                TotalPages: totalPages);
        }, cancellationToken);
    }

    public async Task<PropertyRecord?> UpdateStatusAsync(
        Guid propertyId,
        string newStatus,
        Guid? changedByUserId,
        string changedByRole,
        string? reason,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string getSql = """
            select id, owner_user_id, title, description, city, neighborhood, address,
                   monthly_price, deposit_amount, bedrooms, bathrooms, area_m2,
                   is_furnished, available_from, contract_type, status,
                   created_at, updated_at
            from public.properties
            where id = @id
            limit 1;
            """;

        PropertyRecord? current;
        await using (var getCommand = new NpgsqlCommand(getSql, connection, transaction))
        {
            getCommand.Parameters.AddWithValue("id", propertyId);
            await using var reader = await getCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            current = ReadProperty(reader);
        }

        if (string.Equals(current.Status, newStatus, StringComparison.OrdinalIgnoreCase))
        {
            await transaction.CommitAsync(cancellationToken);
            return current;
        }

        const string updateSql = """
            update public.properties
            set status = @status, updated_at = now()
            where id = @id
            returning id, owner_user_id, title, description, city, neighborhood, address,
                      monthly_price, deposit_amount, bedrooms, bathrooms, area_m2,
                      is_furnished, available_from, contract_type, status,
                      created_at, updated_at;
            """;

        PropertyRecord updated;
        await using (var updateCommand = new NpgsqlCommand(updateSql, connection, transaction))
        {
            updateCommand.Parameters.AddWithValue("status", newStatus);
            updateCommand.Parameters.AddWithValue("id", propertyId);
            await using var reader = await updateCommand.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            updated = ReadProperty(reader);
        }

        const string historySql = """
            insert into public.property_status_history (
                property_id,
                previous_status,
                new_status,
                changed_by_user_id,
                changed_by_role,
                reason
            )
            values (
                @propertyId,
                @previousStatus,
                @newStatus,
                @changedByUserId,
                @changedByRole,
                @reason
            );
            """;

        await using (var historyCommand = new NpgsqlCommand(historySql, connection, transaction))
        {
            historyCommand.Parameters.AddWithValue("propertyId", propertyId);
            historyCommand.Parameters.AddWithValue("previousStatus", current.Status);
            historyCommand.Parameters.AddWithValue("newStatus", newStatus);
            historyCommand.Parameters.AddWithValue("changedByUserId", (object?)changedByUserId ?? DBNull.Value);
            historyCommand.Parameters.AddWithValue("changedByRole", changedByRole);
            historyCommand.Parameters.AddWithValue("reason", (object?)reason ?? DBNull.Value);
            await historyCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return updated;
    }

    public async Task<IReadOnlyList<PropertyStatusHistoryRecord>> GetStatusHistoryAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransientRetryAsync(async token =>
        {
            const string sql = """
                select id, property_id, previous_status, new_status, changed_by_user_id, changed_by_role, reason, changed_at
                from public.property_status_history
                where property_id = @propertyId
                order by changed_at desc;
                """;

            await using var connection = await OpenConnectionAsync(token);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("propertyId", propertyId);

            var items = new List<PropertyStatusHistoryRecord>();
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                items.Add(new PropertyStatusHistoryRecord(
                    Id: reader.GetGuid(0),
                    PropertyId: reader.GetGuid(1),
                    PreviousStatus: reader.GetString(2),
                    NewStatus: reader.GetString(3),
                    ChangedByUserId: reader.IsDBNull(4) ? null : reader.GetGuid(4),
                    ChangedByRole: reader.GetString(5),
                    Reason: reader.IsDBNull(6) ? null : reader.GetString(6),
                    ChangedAt: reader.GetFieldValue<DateTimeOffset>(7)));
            }

            return (IReadOnlyList<PropertyStatusHistoryRecord>)items;
        }, cancellationToken);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static bool IsTransient(Exception exception)
    {
        if (exception is TimeoutException)
        {
            return true;
        }

        if (exception is NpgsqlException npgsqlException)
        {
            if (npgsqlException.InnerException is TimeoutException)
            {
                return true;
            }

            // SQLSTATE class 08 => connection exception
            if (!string.IsNullOrWhiteSpace(npgsqlException.SqlState) &&
                npgsqlException.SqlState.StartsWith("08", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<T> ExecuteWithTransientRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception exception) when (attempt < MaxTransientRetries && IsTransient(exception))
            {
                var delayMs = (attempt + 1) * 250;
                await Task.Delay(delayMs, cancellationToken);
            }
        }
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

    private static void AddPublicSearchParameters(NpgsqlCommand command, PublicPropertySearchParams searchParams)
    {
        if (!string.IsNullOrWhiteSpace(searchParams.City))
        {
            command.Parameters.AddWithValue("city", searchParams.City);
        }

        if (searchParams.MinPrice.HasValue)
        {
            command.Parameters.AddWithValue("minPrice", searchParams.MinPrice.Value);
        }

        if (searchParams.MaxPrice.HasValue)
        {
            command.Parameters.AddWithValue("maxPrice", searchParams.MaxPrice.Value);
        }

        if (searchParams.Bedrooms.HasValue)
        {
            command.Parameters.AddWithValue("bedrooms", searchParams.Bedrooms.Value);
        }

        if (searchParams.IsFurnished.HasValue)
        {
            command.Parameters.AddWithValue("isFurnished", searchParams.IsFurnished.Value);
        }
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

    private static PropertyImageRecord ReadImage(NpgsqlDataReader reader)
    {
        return new PropertyImageRecord(
            Id: reader.GetGuid(0),
            PropertyId: reader.GetGuid(1),
            StoragePath: reader.GetString(2),
            PublicUrl: reader.GetString(3),
            MimeType: reader.GetString(4),
            FileSizeBytes: reader.GetInt32(5),
            DisplayOrder: reader.GetInt32(6),
            CreatedAt: reader.GetFieldValue<DateTimeOffset>(7));
    }
}
