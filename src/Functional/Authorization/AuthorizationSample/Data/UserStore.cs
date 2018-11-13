using System;
using System.Collections.Generic;
using System.Linq;
using AuthorizationSample.Authorization;

namespace AuthorizationSample.Data
{
    public class UserStore
    {
        private static List<User> _users = new List<User>() {
            new User {  Id=1, Name="admin", Password="111111", Role="admin", Email="admin@gmail.com", PhoneNumber="18800000000", Birthday = DateTime.Now },
            new User {  Id=2, Name="alice", Password="111111", Role="user", Email="alice@gmail.com", PhoneNumber="18800000001", Birthday = DateTime.Now.AddDays(-1), Permissions = new List<UserPermission> {
                    new UserPermission { UserId = 1, PermissionName = Permissions.User },
                    new UserPermission { UserId = 1, PermissionName = Permissions.Role }
                }
            },
            new User {  Id=3, Name="bob", Password="111111", Role = "user", Email="bob@gmail.com", PhoneNumber="18800000002", Birthday = DateTime.Now.AddDays(-10), Permissions = new List<UserPermission> {
                    new UserPermission { UserId = 2, PermissionName = Permissions.UserRead },
                    new UserPermission { UserId = 2, PermissionName = Permissions.RoleRead }
                }
            },
        };


        public List<User> GetAll()
        {
            return _users;
        }

        public User Find(int id)
        {
            return _users.Find(_ => _.Id == id);
        }

        public User Find(string userName, string password)
        {
            return _users.FirstOrDefault(_ => _.Name == userName && _.Password == password);
        }

        public bool Exists(int id)
        {
            return _users.Any(_ => _.Id == id);
        }

        public void Add(User doc)
        {
            doc.Id = _users.Max(_ => _.Id) + 1;
            _users.Add(doc);
        }

        public void Update(int id, User doc)
        {
            var oldDoc = _users.Find(_ => _.Id == id);
            if (oldDoc != null)
            {
                oldDoc.Name = doc.Name;
                oldDoc.Email = doc.Email;
                oldDoc.Password = doc.Password;
            }
        }

        public void Remove(User doc)
        {
            if (doc != null)
            {
                _users.Remove(doc);
            }
        }

        public bool CheckPermission(int userId, string permissionName)
        {
            var user = Find(userId);
            if (user == null) return false;
            return user.Permissions.Any(p => permissionName.StartsWith(p.PermissionName));
        }
    }
}
