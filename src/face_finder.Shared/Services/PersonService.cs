using System;
using System.Text.Json;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using face_finder.Shared.Interfaces;
using face_finder.Shared.Models;

namespace face_finder.Shared.Services
{
    public class PersonService : IPersonService
    {
        private const string BucketName = "celeb-db";

        private readonly ILogger<PersonService> _logger;
        private readonly IFileStorage _fileStorage;

        public PersonService(ILogger<PersonService> logger, IFileStorage fileStorage)
        {
            _logger = logger;
            _fileStorage = fileStorage;
        }

        public async Task<string> GetFullNameAsync(string hashName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(hashName))
                throw new ArgumentException(nameof(hashName));

            var externalImageId = hashName; //.ConvertHexStringToAsciiString();

            var fullName = externalImageId;

            /*
            var descriptionObject = await _fileStorage.GetS3ObjectAsync(BucketName, $"{externalImageId}/personal_data.json",
                cancellationToken);

            if (descriptionObject != null)
            {
                using var descriptionStream = new StreamReader(descriptionObject.ResponseStream);
                var jsonString = await descriptionStream.ReadToEndAsync();

                return jsonString;
            }
            */

            return fullName;
        }

        public async Task<string> GetDetailsAsync(string hashName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(hashName))
                throw new ArgumentException(nameof(hashName));

            var externalImageId = hashName; //.ConvertHexStringToAsciiString();

            var descriptionObject =
                await _fileStorage.GetS3ObjectAsync(BucketName, $"{externalImageId}/personal_data.json", cancellationToken);

            var detailedInfo = $"Личный номер: {externalImageId}";

            if (descriptionObject != null)
            {
                using var descriptionStream = new StreamReader(descriptionObject.ResponseStream);
                var jsonString = await descriptionStream.ReadToEndAsync();

                return jsonString;
            }

            return detailedInfo;
        }

        public async Task<string> GetFaceAsync(string hashName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(hashName))
                throw new ArgumentException(nameof(hashName));

            var externalImageId = hashName.ConvertHexStringToAsciiString();

            var imageObject =
                await _fileStorage.GetS3ObjectAsync(BucketName, $"{externalImageId}/original.jpg", cancellationToken);

            await using var responseStream = imageObject.ResponseStream;
            var stream = new MemoryStream();
            await responseStream.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;
            return Convert.ToBase64String(stream.ToArray());
        }

        public async Task<Stream> GetFaceStreamAsync(string hashName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(hashName))
                throw new ArgumentException(nameof(hashName));

            var externalImageId = hashName; //.ConvertHexStringToAsciiString();

            var imageObject = await _fileStorage.GetS3ObjectAsync(BucketName, $"{externalImageId}/original.jpg",
                cancellationToken);

            return imageObject.ResponseStream;
        }
    }
}