using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;

namespace OIDCSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                //options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(o =>
            {
                o.ClientId = "oidc.hybrid";
                o.ClientSecret = "secret";
                o.Authority = "https://oidc.faasx.com";
                o.RequireHttpsMetadata = false;
                o.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                o.SaveTokens = true;
                o.GetClaimsFromUserInfoEndpoint = true;
                o.Events = new OpenIdConnectEvents()
                {
                    OnAuthenticationFailed = c =>
                    {
                        c.HandleResponse();

                        c.Response.StatusCode = 500;
                        c.Response.ContentType = "text/plain";
                        return c.Response.WriteAsync(c.Exception.ToString());
                    }
                };
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IOptionsMonitor<OpenIdConnectOptions> optionsMonitor)
        {
            app.UseAuthentication();

            // 本地退出
            app.Map("/signout", builder => builder.Run(async context =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await context.Response.WriteHtmlAsync(async res =>
                {
                    await res.WriteAsync($"<h1>Signed out {HttpResponseExtensions.HtmlEncode(context.User.Identity.Name)}</h1>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/\">Home</a>");
                });
            }));

            // 远程退出
            app.Map("/signout-remote", builder => builder.Run(async context =>
            {
                await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties()
                {
                    RedirectUri = "/signout"
                });
            }));

            // 检查是否已认证
            app.UseAuthorize();

            // 认证通过，但是授权失败
            app.Map("/restricted", builder => builder.Run(async context =>
            {
                if (!context.User.Identities.Any(identity => identity.HasClaim("special", "true")))
                {
                    await context.ForbidAsync();
                }
                else
                {
                    await context.Response.WriteAsync($"<h1>Hello Authorized User {HttpResponseExtensions.HtmlEncode(context.User.Identity.Name)}</h1>");
                }
            }));

            // 刷新令牌
            app.Map("/refresh", builder => builder.Run(async context =>
            {
                var userResult = await context.AuthenticateAsync();
                var props = userResult.Properties;
                var refreshToken = props.GetTokenValue("refresh_token");

                if (string.IsNullOrEmpty(refreshToken))
                {
                    await context.Response.WriteHtmlAsync(async res =>
                    {
                        await res.WriteAsync($"No refresh_token is available.<br>");
                        await res.WriteAsync("<a class=\"btn btn-link\" href=\"/signout\">Sign Out</a>");
                    });
                    return;
                }

                var options = optionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
                var metadata = await options.ConfigurationManager.GetConfigurationAsync(context.RequestAborted);

                var pairs = new Dictionary<string, string>()
                    {
                        { "client_id", options.ClientId },
                        { "client_secret", options.ClientSecret },
                        { "grant_type", "refresh_token" },
                        { "refresh_token", refreshToken }
                    };
                var content = new FormUrlEncodedContent(pairs);
                var tokenResponse = await options.Backchannel.PostAsync(metadata.TokenEndpoint, content, context.RequestAborted);
                tokenResponse.EnsureSuccessStatusCode();

                var payload = JObject.Parse(await tokenResponse.Content.ReadAsStringAsync());

                // Persist the new acess token
                props.UpdateTokenValue("access_token", payload.Value<string>("access_token"));
                props.UpdateTokenValue("refresh_token", payload.Value<string>("refresh_token"));
                if (int.TryParse(payload.Value<string>("expires_in"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
                {
                    var expiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(seconds);
                    props.UpdateTokenValue("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture));
                }
                await context.SignInAsync(userResult.Principal, props);

                await context.Response.WriteHtmlAsync(async res =>
                {
                    await res.WriteAsync($"<h1>Refreshed.</h1>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/refresh\">Refresh tokens</a>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/\">Home</a>");

                    await res.WriteAsync("<h2>Tokens:</h2>");
                    await res.WriteTableHeader(new string[] { "Token Type", "Value" }, props.GetTokens().Select(token => new string[] { token.Name, token.Value }));

                    await res.WriteAsync("<h2>Payload:</h2>");
                    await res.WriteAsync(HtmlEncoder.Default.Encode(payload.ToString()).Replace(",", ",<br>") + "<br>");
                });
            }));


            // 我的信息
            app.Map("/profile", builder => builder.Run(async context =>
            {
                await context.Response.WriteHtmlAsync(async res =>
                {

                    await res.WriteAsync($"<h1>你好，当前登录用户： {HttpResponseExtensions.HtmlEncode(context.User.Identity.Name)}</h1>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/refresh\">刷新令牌</a>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/restricted\">无权访问</a>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/signout\">本地退出</a>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/signout-remote\">远程退出</a>");

                    await res.WriteAsync("<h2>Claims:</h2>");
                    await res.WriteTableHeader(new string[] { "Claim Type", "Value" }, context.User.Claims.Select(c => new string[] { c.Type, c.Value }));

                    var userResult = await context.AuthenticateAsync();
                    await res.WriteAsync("<h2>Tokens:</h2>");
                    await res.WriteTableHeader(new string[] { "Token Type", "Value" }, userResult.Properties.GetTokens().Select(token => new string[] { token.Name, token.Value }));
                });
            }));

            // 首页
            app.Run(async context =>
            {
                await context.Response.WriteHtmlAsync(async res =>
                {
                    await res.WriteAsync($"<h2>Hello OAuth Authentication</h2>");
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
                    var user = context.User;
                    if (user?.Identity?.IsAuthenticated ?? false)
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
