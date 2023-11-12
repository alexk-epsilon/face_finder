using System.Threading;
using System.Threading.Tasks;
using Amazon.S3.Model;

namespace face_finder.Shared.Interfaces
{
    public interface IFileStorage
    {
        Task SaveRequestImageAsync(string base64Image, string bucketName, string requestId,
            CancellationToken cancellationToken);

        Task SaveResponseImageAsync(string base64Image, string bucketName, string requestId,
            CancellationToken cancellationToken);

        Task<GetObjectResponse> GetS3ObjectAsync(string bucketName, string fileName,
            CancellationToken cancellationToken = default);

        Task UploadFileAsync(string base64File, string bucketName, string keyName,
            CancellationToken cancellationToken = default);
    }
}