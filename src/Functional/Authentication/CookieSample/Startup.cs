using System.Linq;
using System.Security.Claims;
using CookieSample.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CookieSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.ClaimsIssuer = "Cookie";
                options.SessionStore = new MemoryCacheTicketStore();
            });

            services.AddSingleton<UserStore>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // 认证
            app.UseAuthentication();

            // 登录（允许匿名访问，故放在UseAuthorize前面）
            app.Map("/Account/Login", builder => builder.Run(async context =>
            {
                if (context.Request.Method == "GET")
                {
                    await context.Response.WriteHtmlAsync(async res =>
                    {
                        await res.WriteAsync($"<form method=\"post\">");
                        await res.WriteAsync($"<input type=\"hidden\" name=\"returnUrl\" value=\"{HttpResponseExtensions.HtmlEncode(context.Request.Query["ReturnUrl"])}\"/>");
                        await res.WriteAsync($"<div class=\"form-group\"><label>用户名：<input type=\"text\" name=\"userName\" class=\"form-control\"></label></div>");
                        await res.WriteAsync($"<div class=\"form-group\"><label>密码：<input type=\"password\" name=\"password\" class=\"form-control\"></label></div>");
                        await res.WriteAsync($"<button type=\"submit\" class=\"btn btn-default\">登录</button>");
                        await res.WriteAsync($"</form>");
                    });
                }
                else
                {
                    var userStore = context.RequestServices.GetService<UserStore>();
                    var user = userStore.FindUser(context.Request.Form["userName"], context.Request.Form["password"]);
                    if (user == null)
                    {
                        await context.Response.WriteHtmlAsync(async res =>
                       {
                           await res.WriteAsync($"<h1>用户名或密码错误。</h1>");
                           await res.WriteAsync("<a class=\"btn btn-default\" href=\"/Account/Login\">返回</a>");
                       });
                    }
                    else
                    {
                        //1.0 版本
                        var claimIdentity = new ClaimsIdentity("Cookie");
                        claimIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                        claimIdentity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                        claimIdentity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
                        claimIdentity.AddClaim(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
                        claimIdentity.AddClaim(new Claim(ClaimTypes.DateOfBirth, user.Birthday.ToString()));

                        //// 2.0 版本 
                        //var claimIdentity = new ClaimsIdentity("Cookie", JwtClaimTypes.Name, JwtClaimTypes.Role);
                        //claimIdentity.AddClaim(new Claim(JwtClaimTypes.Id, user.Id.ToString()));
                        //claimIdentity.AddClaim(new Claim(JwtClaimTypes.Name, user.Name));
                        //claimIdentity.AddClaim(new Claim(JwtClaimTypes.Email, user.Email));
                        //claimIdentity.AddClaim(new Claim(JwtClaimTypes.PhoneNumber, user.PhoneNumber));
                        //claimIdentity.AddClaim(new Claim(JwtClaimTypes.BirthDate, user.Birthday.ToString()));

                        var claimsPrincipal = new ClaimsPrincipal(claimIdentity);
                        // 在上面注册AddAuthentication时，指定了默认的Scheme，在这里便可以不再指定Scheme。
                        await context.SignInAsync(claimsPrincipal);
                        if (string.IsNullOrEmpty(context.Request.Form["ReturnUrl"])) context.Response.Redirect("/");
                        else context.Response.Redirect(context.Request.Form["ReturnUrl"]);
                    }
                }
            }));

            // 检查是否已认证
            app.UseAuthorize();

            // 访问个人信息
            app.Map("/profile", builder => builder.Run(async context =>
            {
                await context.Response.WriteHtmlAsync(async res =>
                {
                    await res.WriteAsync($"<h1>你好，当前登录用户： {HttpResponseExtensions.HtmlEncode(context.User.Identity.Name)}</h1>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/Account/Logout\">退出</a>");
                    await res.WriteAsync($"<h2>AuthenticationType：{context.User.Identity.AuthenticationType}</h2>");

                    await res.WriteAsync("<h2>Claims:</h2>");
                    await res.WriteTableHeader(new string[] { "Claim Type", "Value" }, context.User.Claims.Select(c => new string[] { c.Type, c.Value }));
                });
            }));

            // 退出
            app.Map("/Account/Logout", builder => builder.Run(async context =>
            {
                await context.SignOutAsync();
                context.Response.Redirect("/");
            }));

            // 首页
            app.Run(async context =>
            {
                await context.Response.WriteHtmlAsync(async res =>
                {
                    await res.WriteAsync($"<h2>Hello Cookie Authentication</h2>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/profile\">我的信息</a>");
                });
            });
        }
    }

    public static class MyAppBuilderExtensions
    {
        // 模拟授权实现
        public static IApplicationBuilder UseAuthorize(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    await next();
                }
                else
                {
                    if (context.User?.Identity?.IsAuthenticated ?? false)
                    {
                        await next();
                    }
                    else
                    {
                        await context.ChallengeAsync();
                    }
                }
            });
        }
    }
}
