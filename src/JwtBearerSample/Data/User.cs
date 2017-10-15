using System;

namespace JwtBearerSample.Data
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Password { get; set; }

        public DateTime Birthday { get; set; }
    }
}
