using Microsoft.AspNetCore.Http;

namespace Backend.Alquila.Features.Properties;

public interface IPropertyImageStorage
{
    Task<StoredPropertyImageFile> SaveAsync(Guid propertyId, IFormFile file, CancellationToken cancellationToken);
}

public sealed record StoredPropertyImageFile(
    string StoragePath,
    string PublicUrl,
    string MimeType,
    int FileSizeBytes);

public sealed class LocalPropertyImageStorage : IPropertyImageStorage
{
    private readonly string _contentRootPath;

    public LocalPropertyImageStorage(IHostEnvironment hostEnvironment)
    {
        _contentRootPath = hostEnvironment.ContentRootPath;
    }

    public async Task<StoredPropertyImageFile> SaveAsync(Guid propertyId, IFormFile file, CancellationToken cancellationToken)
    {
        var extension = ResolveExtension(file.ContentType);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativeDirectory = Path.Combine("uploads", "properties", propertyId.ToString("N"));
        var absoluteDirectory = Path.Combine(_contentRootPath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var absolutePath = Path.Combine(absoluteDirectory, fileName);
        await using (var output = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        await using (var input = file.OpenReadStream())
        {
            await input.CopyToAsync(output, cancellationToken);
        }

        var relativePath = Path.Combine(relativeDirectory, fileName).Replace('\\', '/');
        return new StoredPropertyImageFile(
            StoragePath: relativePath,
            PublicUrl: $"/{relativePath}",
            MimeType: file.ContentType,
            FileSizeBytes: checked((int)file.Length));
    }

    private static string ResolveExtension(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".bin"
        };
    }
}
