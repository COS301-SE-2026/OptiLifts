using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OptiLifts.Application.Storage;

public interface IBlobStorageService
{
    Task<string> UploadFileAsync(Stream stream, string fileName, string contentType, string containerName, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string fileUrl, string containerName, CancellationToken cancellationToken = default);
}
