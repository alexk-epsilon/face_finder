using System.Collections.Generic;

namespace OtYaBatka.WebApi.Models
{
    public class RecognitionRequest
    {
        /// <summary>
        ///     Images encoded with Base64
        /// </summary>
        public List<OriginalImage> Data { get; set; }
    }

    public class OriginalImage
    {
        public string Tag1 { get; set; }

        public string Tag2 { get; set; }

        public string City { get; set; }

        public long? DateTime { get; set; }

        public string Uuid { get; set; }

        public long? SerialNumber { get; set; }

        public bool? IsForcedSave { get; set; }

        public string Feed { get; set; }
    }
}