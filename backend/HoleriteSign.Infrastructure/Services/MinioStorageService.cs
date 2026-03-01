using HoleriteSign.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace HoleriteSign.Infrastructure.Services;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _client;
    private readonly string _bucket;

    public MinioStorageService(IConfiguration config)
    {
        var endpoint = config["Storage:Endpoint"]!.Replace("http://", "").Replace("https://", "");
        var accessKey = config["Storage:AccessKey"]!;
        var secretKey = config["Storage:SecretKey"]!;
        var useSSL = bool.TryParse(config["Storage:UseSSL"], out var ssl) && ssl;
        _bucket = config["Storage:BucketName"] ?? "holeritesign";

        var builder = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey);

        if (useSSL)
            builder = builder.WithSSL();

        _client = builder.Build();
    }

    private async Task EnsureBucketAsync()
    {
        var exists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucket));
        if (!exists)
        {
            await _client.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucket));
        }
    }

    public async Task UploadAsync(string key, Stream stream, string contentType)
    {
        await EnsureBucketAsync();
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(key)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType));
    }

    public async Task UploadBytesAsync(string key, byte[] data, string contentType)
    {
        using var ms = new MemoryStream(data);
        await UploadAsync(key, ms, contentType);
    }

    public async Task<byte[]> DownloadAsync(string key)
    {
        using var ms = new MemoryStream();
        await _client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(key)
            .WithCallbackStream(stream => stream.CopyTo(ms)));
        return ms.ToArray();
    }

    public async Task<Stream> DownloadStreamAsync(string key)
    {
        var ms = new MemoryStream();
        await _client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(key)
            .WithCallbackStream(stream => stream.CopyTo(ms)));
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string key)
    {
        await _client.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_bucket)
            .WithObject(key));
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            await _client.StatObjectAsync(new StatObjectArgs()
                .WithBucket(_bucket)
                .WithObject(key));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
