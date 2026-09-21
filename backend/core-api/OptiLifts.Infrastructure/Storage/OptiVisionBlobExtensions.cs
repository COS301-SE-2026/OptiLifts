using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace OptiLifts.Infrastructure.Storage;

public static class OptiVisionBlobExtensions
{
    public const string ExercisesContainerName = "exercises";
    public static string DefaultContainerName => Environment.GetEnvironmentVariable("AZURE_STORAGE_CONTAINER_NAME") ?? "jobs";

    public static async Task<string> SaveVisionAnalysisFramesAsync(
        this BlobServiceClient blobServiceClient,
        string jobId,
        string jsonPayload,
        CancellationToken cancellationToken = default)
    {
        if (blobServiceClient == null)
            throw new ArgumentNullException(nameof(blobServiceClient));
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("Job ID cannot be null or empty.", nameof(jobId));
        if (jsonPayload == null)
            throw new ArgumentNullException(nameof(jsonPayload));

        var containerClient = blobServiceClient.GetBlobContainerClient(DefaultContainerName);
        return await containerClient.SaveVisionAnalysisFramesAsync(jobId, jsonPayload, cancellationToken);
    }

    public static async Task<string> SaveVisionAnalysisFramesAsync(
        this BlobContainerClient containerClient,
        string jobId,
        string jsonPayload,
        CancellationToken cancellationToken = default)
    {
        if (containerClient == null)
            throw new ArgumentNullException(nameof(containerClient));
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("Job ID cannot be null or empty.", nameof(jobId));
        if (jsonPayload == null)
            throw new ArgumentNullException(nameof(jsonPayload));

        var blobName = FormatBlobName(jobId);

        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);

        var bytes = Encoding.UTF8.GetBytes(jsonPayload);
        using var stream = new MemoryStream(bytes);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
        };

        await blobClient.UploadAsync(stream, options, cancellationToken);
        return blobClient.Uri.ToString();
    }

    public static async Task<string?> GetVisionAnalysisFramesAsync(
        this BlobServiceClient blobServiceClient,
        string jobId,
        CancellationToken cancellationToken = default)
    {
        if (blobServiceClient == null)
            throw new ArgumentNullException(nameof(blobServiceClient));

        var containerClient = blobServiceClient.GetBlobContainerClient(ExercisesContainerName);
        return await containerClient.GetVisionAnalysisFramesAsync(jobId, cancellationToken);
    }

    public static async Task<string?> GetVisionAnalysisFramesAsync(
        this BlobContainerClient containerClient,
        string jobId,
        CancellationToken cancellationToken = default)
    {
        if (containerClient == null)
            throw new ArgumentNullException(nameof(containerClient));
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("Job ID cannot be null or empty.", nameof(jobId));

        var blobName = FormatBlobName(jobId);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blobClient.DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToString();
    }

    public static async Task<Stream?> OpenVisionAnalysisFramesReadStreamAsync(
        this BlobServiceClient blobServiceClient,
        string jobId,
        CancellationToken cancellationToken = default)
    {
        if (blobServiceClient == null)
            throw new ArgumentNullException(nameof(blobServiceClient));

        var containerClient = blobServiceClient.GetBlobContainerClient(ExercisesContainerName);
        return await containerClient.OpenVisionAnalysisFramesReadStreamAsync(jobId, cancellationToken);
    }

    public static async Task<Stream?> OpenVisionAnalysisFramesReadStreamAsync(
        this BlobContainerClient containerClient,
        string jobId,
        CancellationToken cancellationToken = default)
    {
        if (containerClient == null)
            throw new ArgumentNullException(nameof(containerClient));
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("Job ID cannot be null or empty.", nameof(jobId));

        var blobName = FormatBlobName(jobId);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        return await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
    }

    private static string FormatBlobName(string jobId)
    {
        var cleaned = jobId.Trim();
        foreach (var prefix in new[] { $"{DefaultContainerName}/", $"{ExercisesContainerName}/", "jobs/", "exercises/", "optivision-payloads/" })
        {
            if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(prefix.Length);
                break;
            }
        }

        return cleaned.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? cleaned
            : $"{cleaned}.json";
    }
}
