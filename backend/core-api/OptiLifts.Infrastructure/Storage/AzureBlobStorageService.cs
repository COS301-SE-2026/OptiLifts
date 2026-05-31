using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using OptiLifts.Application.Storage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace OptiLifts.Infrastructure.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly string _connectionString;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AzureStorage");

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            // Support alternative env var names that users may set in .env
            _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AzureStorage")
                                ?? Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
                                ?? string.Empty;
        }
    }

    public async Task<string> UploadFileAsync(Stream stream, string fileName, string contentType, string containerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("Azure Storage connection string is missing.");

        var id = Guid.NewGuid();
        var extension = Path.GetExtension(fileName);
        var blobName = $"{id}{extension}";

        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

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
}