using System.Collections.Generic;

namespace face_finder.WebApi.Models
{
    public class RecognitionResultDto
    {
        public List<SubjectDto> Subjects { get; set; }
    }
}