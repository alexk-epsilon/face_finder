using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using OtYaBatka.Shared;
using OtYaBatka.Shared.Services;
using OtYaBatka.TelegramBot;
using SixLabors.ImageSharp;
using Telegram.Bot.Types;
using Xunit;
using Xunit.Abstractions;

namespace OtYaBatka.IntegrationTests.Services
{
    public class RecognitionServiceTests
    {
        private readonly ITestOutputHelper _output;
        private readonly RecognitionService _recognitionService;
        private readonly FileStorage _fileStorage;
        private readonly TelegramClient _telegramClient;
        private readonly PersonService _personService;

        public RecognitionServiceTests(ITestOutputHelper output)
        {
            _output = output;
            var credentials = new BasicAWSCredentials("", "");
            _fileStorage = new FileStorage(Mock.Of<ILogger<FileStorage>>(), credentials);
            _personService = new PersonService(Mock.Of<ILogger<PersonService>>(), _fileStorage);
            _recognitionService = new RecognitionService(Mock.Of<ILogger<RecognitionService>>(), credentials);
            _telegramClient = new TelegramClient(Mock.Of<ILogger<TelegramClient>>(), _recognitionService, _personService);
        }

        [Fact()]
        public async Task ProcessImagesAsyncTest()
        {
            const string imagePath = "E:\\OtYaBatka\\test7.jpg";
            var image = Image.Load(imagePath, out var format);

            var result = await _recognitionService.ProcessImageAsync(Extensions.ToBase64String(image, format).RotateAndRecodeImage());

            await image.DrawFrames(result.Subjects).SaveAsync("E:\\test1.jpg");
        }

        [Fact()]
        public async Task HandleWebhookUpdateAsyncTest()
        {
            const string request = @"{
    'update_id': 490176380,
    'message': {
        'message_id': 1885,
        'from': {
            'id': 1527686051,
            'is_bot': false,
            'first_name': 'Bialorusin',
            'username': 'bialorusin73',
            'language_code': 'en'
        },
        'chat': {
            'id': 1527686051,
            'first_name': 'Bialorusin',
            'username': 'bialorusin73',
            'type': 'private'
        },
        'date': 1641743259,
        'forward_from': {
            'id': 1527686051,
            'is_bot': false,
            'first_name': 'Bialorusin',
            'username': 'bialorusin73',
            'language_code': 'en'
        },
        'forward_date': 1641735057,
        'document': {
            'file_name': 'test7.jpg',

            'mime_type': 'image/jpeg',
            'thumb': {
                'file_id': 'AAMCBAADGQEAAgddYdsDm-iBjhjPr4FdoqoaxZHKUdsAAuMMAALBWdlSsfwWoOBvXgsBAAdtAAMjBA',
                'file_unique_id': 'AQAD4wwAAsFZ2VJy',
                'file_size': 11622,
                'width': 180,
                'height': 320
            },
            'file_id': 'BQACAgQAAxkBAAIHXWHbA5vogY4Yz6-BXaKqGsWRylHbAALjDAACwVnZUrH8FqDgb14LIwQ',
            'file_unique_id': 'AgAD4wwAAsFZ2VI',
            'file_size': 192004
        }
    }
}";
            var webhookData = JsonConvert.DeserializeObject<Update>(request);
            await _telegramClient.HandleWebhookUpdateAsync(webhookData);
            Assert.True(true);
        }

        [Fact]
        public async Task HandleWebhookUpdateAsync_WithCallbackTest()
        {
            const string request = @"{
    'update_id': 490176377,
    'message': {
        'message_id': 1877,
        'from': {
            'id': 1527686051,
            'is_bot': false,
            'first_name': 'Bialorusin',
            'username': 'bialorusin73',
            'language_code': 'en'
        },
        'chat': {
            'id': 1527686051,
            'first_name': 'Bialorusin',
            'username': 'bialorusin73',
            'type': 'private'
        },
        'date': 1641735131,
        'photo': [
            {
                'file_id': 'AgACAgQAAxkBAAIHVWHa49vmK44qaASChWNoch7VbmKEAAI0tzEbwVnZUouHlqsQjcIwAQADAgADcwADIwQ',
                'file_unique_id': 'AQADNLcxG8FZ2VJ4',
                'file_size': 1251,
                'width': 51,
                'height': 90
            },
            {
                'file_id': 'AgACAgQAAxkBAAIHVWHa49vmK44qaASChWNoch7VbmKEAAI0tzEbwVnZUouHlqsQjcIwAQADAgADbQADIwQ',
                'file_unique_id': 'AQADNLcxG8FZ2VJy',
                'file_size': 12958,
                'width': 180,
                'height': 320
            },
            {
                'file_id': 'AgACAgQAAxkBAAIHVWHa49vmK44qaASChWNoch7VbmKEAAI0tzEbwVnZUouHlqsQjcIwAQADAgADeAADIwQ',
                'file_unique_id': 'AQADNLcxG8FZ2VJ9',
                'file_size': 43779,
                'width': 450,
                'height': 800
            },
            {
                'file_id': 'AgACAgQAAxkBAAIHVWHa49vmK44qaASChWNoch7VbmKEAAI0tzEbwVnZUouHlqsQjcIwAQADAgADeQADIwQ',
                'file_unique_id': 'AQADNLcxG8FZ2VJ-',
                'file_size': 72133,
                'width': 720,
                'height': 1280
            }
        ]
    }
}";
            var webhookData = JsonConvert.DeserializeObject<Update>(request);
            await _telegramClient.HandleWebhookUpdateAsync(webhookData);
            Assert.True(true);
        }

        [Fact()]
        public async Task CreateCollectionAsyncTest()
        {
            var collectionId = "BlackBookBelarusCollection";
            var result = await _recognitionService.CreateCollectionAsync(collectionId);
            Assert.True(result);
        }

        [Fact()]
        public async Task AddFaceToCollectionAsyncTest()
        {
            var collectionId = "BlackBookBelarusCollection";
            const string imagePath = "E:\\Lukashenko.png";
            var result =
                await _recognitionService.AddFaceToCollectionAsync(
                    Image.Load(imagePath, out var format).GetStream(format), "Lukashenko", collectionId);
            Assert.True(result);
        }

        [Fact()]
        public async Task ListFacesAsyncTest()
        {
            var collectionId = "BlackBookBelarusCollection";
            var result = await _recognitionService.ListFacesAsync(collectionId);
            Assert.NotNull(result);
        }

        [Fact()]
        public async Task DetectFacesAsyncTest()
        {
            var originalImage = Image.Load("E:\\test.jpg", out var format);
            var result = await _recognitionService.DetectFacesAsync(originalImage.GetStream(format));
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AddFacesFromS3ToBlackBookCollectionAsyncTest()
        {
            var collectionId = "BlackBookBelarusCollection";
            var collectionWasDeleted = await _recognitionService.DeleteCollectionAsync(collectionId);
            Assert.True(collectionWasDeleted);
            var collectionWasCreated = await _recognitionService.CreateCollectionAsync(collectionId);
            Assert.True(collectionWasCreated);

            const string bucketName = "blackbookbelarus-may-2021-bucket";
            var result = await _fileStorage.GetFileInfos(bucketName);

            var faceCount = 0;
            foreach (var s3Object in result.Where(x => Path.GetFileName(x.Key) == "pivot.jpg"))
            {
                var faceWasAdded =
                    await _recognitionService.AddFaceToCollectionAsync(bucketName, s3Object.Key, collectionId);
                Assert.True(faceWasAdded);
                faceCount++;
            }

            _output.WriteLine($"{faceCount} faces were added");
        }

        [Fact]
        public async Task AddFacesFromS3ToPassportBaseCollectionAsyncTest()
        {
            var collectionId = "PassportCollection";
            var collectionWasDeleted = await _recognitionService.DeleteCollectionAsync(collectionId);
            Assert.True(collectionWasDeleted);
            var collectionWasCreated = await _recognitionService.CreateCollectionAsync(collectionId);
            Assert.True(collectionWasCreated);

            const string bucketName = "otyabatka-passport-base-collection";
            var result = await _fileStorage.GetFileInfos(bucketName);

            var faceCount = 0;
            foreach (var s3Object in result.Where(x => Path.GetFileName(x.Key) == "original.jpeg"))
            {
                var faceWasAdded =
                    await _recognitionService.AddFaceToCollectionAsync(bucketName, s3Object.Key, collectionId);
                Assert.True(faceWasAdded);
                faceCount++;
            }

            _output.WriteLine($"{faceCount} faces were added");
        }

        //[Fact]
        //public async Task GetFaceAsyncTest()
        //{
        //    var hashString = "AbeliSkaja_Irina_Stepanovna".ConvertAsciiStringToHexString();
        //    var result = await _personService.GetFaceAsync(hashString);
        //    var originalImageInBytes = Convert.FromBase64String(result);
        //    var image = Image.Load(originalImageInBytes, new JpegDecoder());
        //    await image.SaveAsync("E:\\test10.jpg");
        //}

        [Fact()]
        public async Task GetFullNameAsyncTest()
        {
            var result = await _personService.GetFullNameAsync("3330313032383745303230504233");
            Assert.True(false, "This test needs an implementation");
        }
    }
}