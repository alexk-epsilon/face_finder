using System;

namespace OtYaBatka.Shared.Models
{
    public class PersonalData
    {
        public string FullName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Patronym { get; set; }

        public string IdentityNumber { get; set; }

        public DateTime Dob { get; set; }

        public string Rank { get; set; }

        public string Position { get; set; }

        public Address[] Addresses { get; set; }

        public PhoneNumber[] PhoneNumbers { get; set; }

        public Email[] Emails { get; set; }

        public SocialMedia[] SocialMedia { get; set; }

        public string Other { get; set; }

        public string Source { get; set; }
    }

    public class Address
    {
        public string Country { get; set; }

        public string City { get; set; }

        public string Street { get; set; }

        public string Building { get; set; }

        public string Apartment { get; set; }

        public string Note { get; set; }
    }

    public class PhoneNumber
    {
        public string Number { get; set; }

        public string Note { get; set; }
    }

    public class Email
    {
        public string EmailAddress { get; set; }

        public string Note { get; set; }
    }

    public class SocialMedia
    {
        public string Link { get; set; }

        public string Note { get; set; }
    }
}