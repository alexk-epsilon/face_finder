using System.Collections.Generic;

namespace OtYaBatka.WebApi.Models
{
    public class RecognitionResponse
    {
        public RecognitionResponse()
        {
            Results = new List<RecognitionResultDto>();
        }

        public List<RecognitionResultDto> Results { get; set; }
    }
}