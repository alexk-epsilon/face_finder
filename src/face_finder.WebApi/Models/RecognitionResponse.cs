using System.Collections.Generic;

namespace face_finder.WebApi.Models
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