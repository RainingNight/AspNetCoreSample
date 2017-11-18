using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AuthorizationSample.Data
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public DateTime Birthday { get; set; }

        public string Role { get; set; }

        public List<UserPermission> Permissions { get; set; }
    }
}
