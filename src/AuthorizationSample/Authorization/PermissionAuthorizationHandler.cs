using AuthorizationSample.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Threading.Tasks;
using System.Security.Claims;

namespace AuthorizationSample.Authorization
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
    {
        private readonly UserStore _userStore;

        public PermissionAuthorizationHandler(UserStore userStore)
        {
            _userStore = userStore;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionAuthorizationRequirement requirement)
        {
            if (context.User != null)
            {
                if (context.User.IsInRole("admin"))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    var userIdClaim = context.User.FindFirst(_ => _.Type == ClaimTypes.NameIdentifier);
                    if (userIdClaim != null)
                    {
                        if (_userStore.CheckPermission(int.Parse(userIdClaim.Value), requirement.Name))
                        {
                            context.Succeed(requirement);
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
