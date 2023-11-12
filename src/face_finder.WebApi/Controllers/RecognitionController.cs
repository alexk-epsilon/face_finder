using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using face_finder.WebApi.Models;

namespace face_finder.WebApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiVersion("2")]
    public class RecognitionController : ControllerBase
    {
        private readonly ILogger<RecognitionController> _logger;
        private readonly IRequestProcessor _requestProcessor;

        public RecognitionController(ILogger<RecognitionController> logger, IRequestProcessor requestProcessor)
        {
            _logger = logger;
            _requestProcessor = requestProcessor;
        }

        /// <summary>
        /// Detects and recognizes faces in the image.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/v1/Recognition
        ///     {
        ///        "image": ""
        ///     }
        ///
        /// </remarks>
        /// <returns>Recognition result</returns>
        /// <response code="400">Image successfully processed</response>
        /// <response code="400">Bad request</response>
        /// <response code="500">Internal server error</response>
        ///
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RecognitionResponse>> RecognizeImage(RecognitionRequest request, CancellationToken cancellationToken)
        {
            if (request.Data == null || request.Data.Count < 1)
                return BadRequest("At least one image must be loaded");

            foreach (var image in request.Data)
            {
                if (string.IsNullOrWhiteSpace(image.Tag1) || !image.Tag1.IsBase64String())
                    return BadRequest("The image must be loaded as Base64 string");
            }

            _logger.LogDebug("===== New request =====");
            _logger.LogDebug($"Tag2: {request.Data[0].Tag2}; City: {request.Data[0].City}; DateTime: {request.Data[0].DateTime}; SerialNumber: {request.Data[0].SerialNumber}; Uuid: {request.Data[0].Uuid};");

            var resultDto = await _requestProcessor.ProcessImageAsync(request.Data[0], cancellationToken);

            var response = new RecognitionResponse
            {
                Results = new List<RecognitionResultDto> { resultDto }
            };

            _logger.LogDebug("===== Response =====");
            _logger.LogDebug(JsonConvert.SerializeObject(response));

            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [MapToApiVersion("2")]
        public async Task<IActionResult> RecognizeImageV2(CancellationToken cancellationToken)
        {
            try
            {
                var file = Request.Form.Files[0];
                if (file.Length > 0)
                {
                    var result = new RecognitionResultDto
                    {
                        Subjects = new List<SubjectDto>
                        {
                            new SubjectDto()
                            {
                                Box = new FacesBoxDto
                                {
                                    XMax = 812,
                                    YMax = 433,
                                    XMin = 587,
                                    YMin = 147
                                },
                                Person = new PersonDto
                                {
                                    FullName = "Lukashenko",
                                    Similarity = 0.55897
                                }
                            },
                            new SubjectDto()
                            {
                                Box = new FacesBoxDto
                                {
                                    XMax = 468,
                                    YMax = 401,
                                    XMin = 248,
                                    YMin = 104
                                },
                                Person = new PersonDto
                                {
                                    FullName = "Putin",
                                    Similarity = 0.92171
                                }
                            }
                        }
                    };

                    return Ok(result);
                }

                return BadRequest();
            }
            catch (Exception e)
            {
                return StatusCode(500, $"Internal server error: {e}");
            }
        }
    }
}