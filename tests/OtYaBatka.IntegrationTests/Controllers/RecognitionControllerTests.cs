using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OtYaBatka.Shared;
using OtYaBatka.WebApi.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace OtYaBatka.IntegrationTests.Controllers
{
    public class RecognitionControllerTests
    {
        [Fact]
        public async Task RecognizeImageTest()
        {
            var image = Image.Load("E://OtYaBatka//test7.jpg", out var format);
            var base64Image = Extensions.ToBase64String(image, format);
            var request = new RecognitionRequest
            {
                Data = new List<OriginalImage>
                {
                    new()
                    {
                        Tag1 = base64Image, Tag2 = "testImage", City = "Minsk",
                        Uuid = Guid.NewGuid().ToString(), SerialNumber = 1,
                        IsForcedSave = false
                    }
                }
            };

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://mbxk87jo5d.execute-api.us-east-1.amazonaws.com")
            };

            var todoItemJson = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.PostAsync("Prod/api/v1/Recognition", todoItemJson);

            response.EnsureSuccessStatusCode();

            var subjects = (await response.Content.ReadFromJsonAsync<RecognitionResponse>())?.Results[0].Subjects;

            using var img2 = image.Clone(ctx => ctx.ApplyScalingWaterMark());
            await img2.DrawFrames(subjects.Select(subject => subject.ToSubject())).SaveAsync("E:\\test1.jpg");
        }
    }
}