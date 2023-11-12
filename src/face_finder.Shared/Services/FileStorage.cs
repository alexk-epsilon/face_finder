using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using face_finder.Shared.Interfaces;

namespace face_finder.Shared.Services
{
    public class FileStorage : IFileStorage, IDisposable
    {
        private readonly ILogger<FileStorage> _logger;
        private readonly AmazonS3Client _s3Client;
        private readonly TransferUtility _fileTransferUtility;

        public FileStorage(ILogger<FileStorage> logger)
        {
            _logger = logger;
            var config = new AmazonS3Config
            {
                MaxErrorRetry = 5,
                RegionEndpoint = RegionEndpoint.USEast1,
            };
            _s3Client = new AmazonS3Client(config);
            _fileTransferUtility = new TransferUtility(_s3Client);
        }

        public FileStorage(ILogger<FileStorage> logger, AWSCredentials credentials)
        {
            _logger = logger;
            var config = new AmazonS3Config
            {
                MaxErrorRetry = 5,
                RegionEndpoint = RegionEndpoint.USEast1,
            };
            _s3Client = new AmazonS3Client(credentials, config);
            _fileTransferUtility = new TransferUtility(_s3Client);
        }

        public Task SaveRequestImageAsync(string base64Image, string bucketName, string requestId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(base64Image))
                throw new ArgumentException("Value cannot be null or empty", nameof(base64Image));

            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("Value cannot be null or empty", nameof(requestId));

            return UploadFileAsync(base64Image, bucketName, $"{requestId}/request", cancellationToken);
        }

        public Task SaveResponseImageAsync(string base64Image, string bucketName, string requestId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(base64Image))
                throw new ArgumentException("Value cannot be null or empty", nameof(base64Image));

            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("Value cannot be null or empty", nameof(requestId));

            return UploadFileAsync(base64Image, bucketName, $"{requestId}/response", cancellationToken);
        }

        public async Task<List<S3Object>> GetFileInfos(string bucketName, CancellationToken cancellationToken = default)
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName
            };

            ListObjectsV2Response response;
            var items = new List<S3Object>();

            do
            {
                response = await _s3Client.ListObjectsV2Async(request, cancellationToken);
                items.AddRange(response.S3Objects);
                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);

            return items;
        }

        public async Task<GetObjectResponse> GetS3ObjectAsync(string bucketName, string fileName,
            CancellationToken cancellationToken = default)
        {
            var request = new GetObjectRequest { BucketName = bucketName, Key = fileName };

            GetObjectResponse response = null;
            try
            {
                response = await _s3Client.GetObjectAsync(request, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError($"Cannot get the object '{fileName}' from the bucket {bucketName}. Exception: {e}");
            }

            return response;
        }

        public async Task UploadFileAsync(string base64File, string bucketName, string keyName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64File);
                await using var fileStream = new MemoryStream(bytes);
                await _fileTransferUtility.UploadAsync(fileStream, bucketName, keyName, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError($"Cannot upload file {base64File} to S3. Exception: {e}");
            }
        }

        public void Dispose()
        {
            _s3Client?.Dispose();
            _fileTransferUtility?.Dispose();
        }
    }
}