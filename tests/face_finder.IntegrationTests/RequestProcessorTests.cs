using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Moq;
using face_finder.Shared.Services;
using face_finder.WebApi;
using face_finder.WebApi.Models;
using SixLabors.ImageSharp;
using Xunit;
using Extensions = face_finder.Shared.Extensions;

namespace face_finder.IntegrationTests
{
    public class RequestProcessorTests
    {
        [Fact]
        public async Task ProcessImageAsyncTest()
        {
            var credentials = new BasicAWSCredentials("", "");
            var fileStorage = new FileStorage(Mock.Of<ILogger<FileStorage>>(), credentials);
            var recognitionService = new RecognitionService(Mock.Of<ILogger<RecognitionService>>(), credentials);
            var personService = new PersonService(Mock.Of<ILogger<PersonService>>(), fileStorage);
            var sut = new RequestProcessor(fileStorage, recognitionService, personService);

            var image = Image.Load("E://face_finder//test7.jpg", out var format);
            var base64Image = Extensions.ToBase64String(image, format);
            var request = new OriginalImage
            {
                Tag1 = base64Image,
                Tag2 = "testImage",
                City = "Minsk",
                Uuid = Guid.NewGuid().ToString(),
                SerialNumber = 1,
                IsForcedSave = false
            };

            var result = await sut.ProcessImageAsync(request);
        }
    }
}