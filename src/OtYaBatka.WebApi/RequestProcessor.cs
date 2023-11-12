using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OtYaBatka.Shared;
using OtYaBatka.Shared.Interfaces;
using OtYaBatka.WebApi.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace OtYaBatka.WebApi
{
    public class RequestProcessor : IRequestProcessor
    {
        private const string BucketName = "otyabatka-mobile-app-by";

        private readonly IFileStorage _fileStorage;
        private readonly IRecognitionService _recognitionService;
        private readonly IPersonService _personService;

        public RequestProcessor(IFileStorage fileStorage, IRecognitionService recognitionService, IPersonService personService)
        {
            _fileStorage = fileStorage;
            _recognitionService = recognitionService;
            _personService = personService;
        }

        public async Task<RecognitionResultDto> ProcessImageAsync(OriginalImage image, CancellationToken cancellationToken = default)
        {
            var city = string.IsNullOrWhiteSpace(image.City) ? "unknown" : image.City;
            var utcNow = DateTime.UtcNow;
            var serialNumber = $"00000-{utcNow.Hour}{utcNow.Minute}{utcNow.Second}";
            if (image.SerialNumber.HasValue)
                serialNumber = image.SerialNumber.ToString().PadLeft(5, '0');

            var keyName = $"{utcNow:yyyy'-'MM'-'dd}-{RoundHour(utcNow)}/{city}/{image.Uuid}/N{serialNumber}";

            if (image.IsForcedSave.HasValue && image.IsForcedSave.Value)
                await _fileStorage.UploadFileAsync(image.Tag1, BucketName, keyName, cancellationToken);

            var result = await _recognitionService.ProcessImageAsync(image.Tag1, cancellationToken);

            var resultDto = new RecognitionResultDto
            {
                Subjects = new List<SubjectDto>()
            };

            foreach (var subject in result.Subjects)
            {
                var box = new FacesBoxDto
                {
                    XMax = Convert.ToInt32(Math.Truncate(subject.Box.XMax)),
                    XMin = Convert.ToInt32(Math.Ceiling(subject.Box.XMin)),
                    YMax = Convert.ToInt32(Math.Ceiling(subject.Box.YMax)),
                    YMin = Convert.ToInt32(Math.Ceiling(subject.Box.YMin))
                };

                var person = new PersonDto
                {
                    FullName = string.IsNullOrEmpty(subject.Person.FullNameHash)
                        ? "Unknown"
                        : await _personService.GetFullNameAsync(subject.Person.FullNameHash, cancellationToken),
                    Similarity = subject.Person.Similarity,
                    Image = string.IsNullOrEmpty(subject.Person.FullNameHash)
                        ? string.Empty
                        : await _personService.GetFaceAsync(subject.Person.FullNameHash, cancellationToken),
                    Details = string.IsNullOrEmpty(subject.Person.FullNameHash)
                        ? string.Empty
                        : await _personService.GetDetailsAsync(subject.Person.FullNameHash, cancellationToken),
                };

                resultDto.Subjects.Add(new SubjectDto
                {
                    Id = subject.Id,
                    Box = box,
                    Person = person
                });
            }

            return resultDto;
        }

        private static int RoundHour(DateTime date)
        {
            var interval = TimeSpan.FromHours(2);

            return new DateTime(
                (long)Math.Truncate(date.Ticks / (double)interval.Ticks) * interval.Ticks).Hour
            ;
        }
    }

    public interface IRequestProcessor
    {
        Task<RecognitionResultDto> ProcessImageAsync(OriginalImage image, CancellationToken cancellationToken = default);
    }
}