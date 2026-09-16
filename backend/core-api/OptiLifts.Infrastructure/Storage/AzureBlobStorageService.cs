using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Application.Storage;

namespace OptiLifts.Infrastructure.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    [ActivatorUtilitiesConstructor]
    public AzureBlobStorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
    }

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Support alternative env var names that users may set in .env
            connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__AZURESTORAGE")
                                ?? Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "UseDevelopmentStorage=true;";
        }

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public AzureBlobStorageService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "UseDevelopmentStorage=true;";
        }

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadFileAsync(Stream stream, string fileName, string contentType, string containerName, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var extension = Path.GetExtension(fileName);
        var blobName = $"{id}{extension}";

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        // Ensure container exists and allows public access to blobs
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        // Reset stream position if needed
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await blobClient.UploadAsync(stream, options, cancellationToken);

        // the local azure emulator url uses a different format, so just returning the uri works fine
        return blobClient.Uri.ToString();
    }

    public async Task DeleteFileAsync(string fileUrl, string containerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return;

        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
            return;

        var blobName = Path.GetFileName(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(blobName))
            return;

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public Task<string> SaveVisionAnalysisFramesAsync(string jobId, string jsonPayload, CancellationToken cancellationToken = default)
    {
        return _blobServiceClient.SaveVisionAnalysisFramesAsync(jobId, jsonPayload, cancellationToken);
    }
}
