using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthorizationSample.Data
{
    public class UserStore
    {
        private static List<User> _users = new List<User>() {
            new User {  Id=1, Name="admin", Password="111111", Role="admin", Email="admin@gmail.com", PhoneNumber="18800000000", Birthday=DateTime.Now },
            new User {  Id=2, Name="alice", Password="111111", Role="user", Email="alice@gmail.com", PhoneNumber="18800000001", Birthday=DateTime.Now.AddDays(-1) },
            new User {  Id=3, Name="bob", Password="111111", Role = "user", Email="bob@gmail.com", PhoneNumber="18800000002", Birthday=DateTime.Now.AddDays(-10) }
        };

        public User FindUser(string userName, string password)
        {
            return _users.FirstOrDefault(_ => _.Name == userName && _.Password == password);
        }
    }
}
