namespace HoleriteSign.Core.Interfaces;

/// <summary>
/// Abstraction for object storage (S3/R2/MinIO).
/// </summary>
public interface IStorageService
{
    Task UploadAsync(string key, Stream stream, string contentType);
    Task UploadBytesAsync(string key, byte[] data, string contentType);
    Task<byte[]> DownloadAsync(string key);
    Task<Stream> DownloadStreamAsync(string key);
    Task DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
}
