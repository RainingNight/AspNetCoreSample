using System;

namespace AuthorizationSample.Data
{
    public class UserPermission
    {
        public int UserId { get; set; }

        public string PermissionName { get; set; }

        public User User { get; set; }
    }
}
