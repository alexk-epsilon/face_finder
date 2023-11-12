using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OtYaBatka.Shared.Interfaces;
using OtYaBatka.Shared.Services;
using Telegram.Bot.Types;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OtYaBatka.TelegramBot
{
    public class Functions
    {
        private readonly TelegramClient _svc;
        private readonly ILogger<Functions> _logger;

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            serviceCollection.AddLogging(logging =>
            {
                logging.AddLambdaLogger();
                logging.SetMinimumLevel(LogLevel.Trace);
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();

            _svc = serviceProvider.GetService<TelegramClient>();
            _logger = serviceProvider.GetService<ILogger<Functions>>();
        }

        public async Task<APIGatewayProxyResponse> Post(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                _logger.LogTrace($"Message received: {request.Body}");
                var webhookData = JsonConvert.DeserializeObject<Update>(request.Body);
                await _svc.HandleWebhookUpdateAsync(webhookData);
            }
            catch (Exception e)
            {
                context.Logger.LogLine(e.ToString());
            }

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.NoContent,
            };

            return response;
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IFileStorage, FileStorage>();
            serviceCollection.AddTransient<IPersonService, PersonService>();
            serviceCollection.AddTransient<IRecognitionService, RecognitionService>();
            serviceCollection.AddTransient<TelegramClient>();
        }
    }
}