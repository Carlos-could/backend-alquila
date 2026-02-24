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
}
