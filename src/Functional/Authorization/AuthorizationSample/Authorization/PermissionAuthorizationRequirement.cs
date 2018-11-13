using AuthorizationSample.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

namespace AuthorizationSample.Authorization
{
    public class PermissionAuthorizationRequirement : IAuthorizationRequirement
    {
        public PermissionAuthorizationRequirement(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 权限名称
        /// </summary>
        public string Name { get; set; }
    }
}