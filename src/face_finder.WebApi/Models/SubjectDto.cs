using face_finder.Shared.Models;

namespace face_finder.WebApi.Models
{
    public class SubjectDto
    {
        public int Id { get; set; }

        public FacesBoxDto Box { get; set; }

        public PersonDto Person { get; set; }

        public Subject ToSubject()
        {
            var person = new Person
            {
                Similarity = Person.Similarity
            };

            var box = new FacesBox
            {
                XMax = Box.XMax,
                XMin = Box.XMin,
                YMax = Box.YMax,
                YMin = Box.YMin
            };

            return new Subject
            {
                Id = Id,
                Person = person,
                Box = box
            };
        }
    }
}