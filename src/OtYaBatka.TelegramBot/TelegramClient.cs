using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OtYaBatka.Shared;
using OtYaBatka.Shared.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace OtYaBatka.TelegramBot
{
    public class TelegramClient
    {
        private const string UnknownCommandMessage = "Just upload a photo to me.\nПросто загрузи в меня фото.";
        private const string BotApiKey = "6183107391:AAFlSpKLMAFtojbi1DtxEDSyRYkNRgFZj_U";
        private const int MaxFileSize = 5242880;

        private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".JPEG", ".BMP", ".GIF", ".PNG" };
        private readonly ILogger<TelegramClient> _logger;
        private readonly IRecognitionService _recognitionService;
        private readonly IPersonService _personService;
        private readonly WebClient _webClient;
        private readonly TelegramBotClient _botClient;

        public TelegramClient(ILogger<TelegramClient> logger, IRecognitionService recognitionService, IPersonService personService)
        {
            _logger = logger;
            _recognitionService = recognitionService;
            _personService = personService;
            _webClient = new WebClient();
            _botClient = new TelegramBotClient(BotApiKey);
        }

        public async Task<string> BotUserName() => $"@{(await _botClient.GetMeAsync()).Username}";

        public async Task HandleWebhookUpdateAsync(Update webhook)
        {
            try
            {
                switch (webhook.Type)
                {
                    case UpdateType.Message:
                        if (webhook.Message.Type == MessageType.Photo)
                        {
                            _logger.LogTrace("Message type: Message");
                            await HandlePhotoAsync(webhook.Message);
                            return;
                        }
                        else if (webhook.Message.Type == MessageType.Document)
                        {
                            _logger.LogTrace("Message type: Document");
                            await HandleDocumentAsync(webhook.Message);
                            return;
                        }
                        break;

                    case UpdateType.CallbackQuery:
                        _logger.LogTrace("Message type: CallbackQuery");
                        await HandleCallbackQueryAsync(webhook.CallbackQuery);
                        return;
                }

                await _botClient.SendTextMessageAsync(webhook.Message.Chat.Id, UnknownCommandMessage);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
            }
        }

        public async Task HandlePhotoAsync(Message message)
        {
            var file = await _botClient.GetFileAsync(message.Photo[message.Photo.Count() - 1].FileId);
            if (file.FileSize > MaxFileSize)
            {
                _logger.LogError($"Cannot process file large than 5 Mb: {file.FileSize}");
                await _botClient.SendTextMessageAsync(message.Chat.Id, $"Unfortunately, I cannot process a file larger than 5 MB.\nК сожалению я не могу обработать файл больше, чем 5 Мб.");
                return;
            }
            var downloadUrl = $@"https://api.telegram.org/file/bot{BotApiKey}/{file.FilePath}";
            var stream = await _webClient.OpenReadTaskAsync(downloadUrl);
            var (image, format) = await Image.LoadWithFormatAsync(stream);
            image.Mutate(x => x.AutoOrient());
            var base64Image = Extensions.ToBase64String(image, format);
            var result = await _recognitionService.ProcessImageAsync(base64Image);
            if (result == null)
            {
                _logger.LogError($"Cannot process image: {base64Image}");
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Unfortunately I couldn't process the photo, please try again.\nК сожалению я не смог обработать фото, попробуй еще раз.");
                return;
            }

            if (result.Subjects == null || result.Subjects.Count == 0)
            {
                _logger.LogError($"No faces detected: {base64Image}");
                await _botClient.SendTextMessageAsync(message.Chat.Id, "No faces detected.\nЛиц не обнаружено.");
                return;
            }

            using var resultImage = image.Clone(ctx => ctx.ApplyScalingWaterMark());
            var resultImageStream = resultImage.DrawFrames(result.Subjects).ToStream(format);

            await _botClient.SendPhotoAsync(message.Chat.Id, new InputOnlineFile(resultImageStream));

            foreach (var subject in result.Subjects.OrderBy(x => x.Id))
            {
                var messageText = $"Фигурант {subject.Id}: Неизвестный";

                if (!string.IsNullOrEmpty(subject.Person.FullNameHash))
                {
                    var fullName = await _personService.GetFullNameAsync(subject.Person.FullNameHash);

                    var ikm = new InlineKeyboardMarkup(new[]
                    {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Подробнее", subject.Person.FullNameHash),
                            }
                        });

                    messageText = $"Фигурант {subject.Id}: {fullName}\nСовпадение: {subject.Person.Similarity}%";
                    try
                    {
                        await _botClient.SendTextMessageAsync(message.Chat.Id, messageText, replyMarkup: ikm);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.ToString());
                        await _botClient.SendTextMessageAsync(message.Chat.Id, messageText);
                    }
                }
                else
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, messageText);
                }
            }
        }

        public async Task HandleDocumentAsync(Message message)
        {
            var file = await _botClient.GetFileAsync(message.Document.FileId);
            if (file.FileSize > MaxFileSize)
            {
                _logger.LogError($"Cannot process file large than 5 Mb: {file.FileSize}");
                await _botClient.SendTextMessageAsync(message.Chat.Id, $"Unfortunately, I cannot process a file larger than 5 MB.\nК сожалению я не могу обработать файл больше, чем 5 Мб.");
                return;
            }

            var downloadUrl = $@"https://api.telegram.org/file/bot{BotApiKey}/{file.FilePath}";
            var fileExtension = Path.GetExtension(downloadUrl);
            if (!ImageExtensions.Contains(fileExtension.ToUpperInvariant()))
            {
                _logger.LogError($"Cannot process file extension: {fileExtension}");
                await _botClient.SendTextMessageAsync(message.Chat.Id, $"Unfortunately I couldn't process {fileExtension} file, I need an image.\nК сожалению я не смог обработать {fileExtension} файл, мне нужно фото.");
                return;
            }
            var stream = await _webClient.OpenReadTaskAsync(downloadUrl);
            var (image, format) = await Image.LoadWithFormatAsync(stream);
            image.Mutate(x => x.AutoOrient());
            var base64Image = Extensions.ToBase64String(image, format);
            //var result = await _recognitionService.ProcessImageAsync(base64Image.RotateAndRecodeImage());
            var result = await _recognitionService.ProcessImageAsync(base64Image);
            if (result == null)
            {
                _logger.LogError($"Cannot process image: {base64Image}");
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Unfortunately I couldn't process the photo, please try again.\nК сожалению я не смог обработать фото, попробуй еще раз.");
                return;
            }
            if (result.Subjects == null || result.Subjects.Count == 0)
            {
                _logger.LogError($"No faces detected: {base64Image}");
                await _botClient.SendTextMessageAsync(message.Chat.Id, "No faces detected.\nЛиц не обнаружено.");
                return;
            }

            using var resultImage = image.Clone(ctx => ctx.ApplyScalingWaterMark());
            var resultImageStream = resultImage.DrawFrames(result.Subjects).ToStream(format);

            await _botClient.SendPhotoAsync(message.Chat.Id, new InputOnlineFile(resultImageStream));

            foreach (var subject in result.Subjects.OrderBy(x => x.Id))
            {
                var messageText = $"Фигурант {subject.Id}: Неизвестный";

                if (!string.IsNullOrEmpty(subject.Person.FullNameHash))
                {
                    var fullName = await _personService.GetFullNameAsync(subject.Person.FullNameHash);

                    var ikm = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Подробнее", subject.Person.FullNameHash),
                        }
                    });

                    messageText = $"Фигурант {subject.Id}: {fullName}\nСовпадение: {subject.Person.Similarity}%";
                    await _botClient.SendTextMessageAsync(message.Chat.Id, messageText, replyMarkup: ikm);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, messageText);
                }
            }
        }

        public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            var detailedInfo = await _personService.GetDetailsAsync(callbackQuery.Data);
            var image = await _personService.GetFaceStreamAsync(callbackQuery.Data);

            await _botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId, $"{callbackQuery.Message.Text}\n\n{detailedInfo}",
                disableWebPagePreview: false);

            //await _botClient.SendPhotoAsync(callbackQuery.Message.Chat.Id, new InputOnlineFile(image, $"{callbackQuery.Message.Text}"));
            await _botClient.SendPhotoAsync(callbackQuery.Message.Chat.Id, new InputOnlineFile(image));
        }
    }
}