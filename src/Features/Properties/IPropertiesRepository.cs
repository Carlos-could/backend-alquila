namespace Backend.Alquila.Features.Properties;

public interface IPropertiesRepository
{
    Task<Guid?> FindUserIdByAuthUserIdAsync(Guid authUserId, CancellationToken cancellationToken);

    Task<PropertyRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PropertyRecord> CreateAsync(
        Guid ownerUserId,
        NormalizedPropertyCreateInput input,
        CancellationToken cancellationToken);

    Task<PropertyRecord?> UpdateAsync(
        Guid id,
        NormalizedPropertyPatchInput input,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PropertyImageRecord>> GetImagesByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken);

    Task<int> CountImagesByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PropertyImageRecord>> AddImagesAsync(
        Guid propertyId,
        IReadOnlyList<NewPropertyImageInput> images,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PropertyImageRecord>> UpdateImageOrderAsync(
        Guid propertyId,
        IReadOnlyList<PropertyImageOrderItemRequest> items,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PropertyModerationQueueItemResponse>> ListPendingModerationAsync(CancellationToken cancellationToken);

    Task<PublicPropertySearchResponse> SearchPublishedForPublicAsync(
        PublicPropertySearchParams searchParams,
        CancellationToken cancellationToken);

    Task<PropertyRecord?> UpdateStatusAsync(
        Guid propertyId,
        string newStatus,
        Guid? changedByUserId,
        string changedByRole,
        string? reason,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PropertyStatusHistoryRecord>> GetStatusHistoryAsync(Guid propertyId, CancellationToken cancellationToken);
}
