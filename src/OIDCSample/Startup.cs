using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
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
            .AddCookie(o =>
            {
                o.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(o =>
            {
                o.ClientId = "oidc.hybrid";
                o.ClientSecret = "secret";

                o.Authority = "https://oidc.faasx.com/";
                //o.MetadataAddress = "https://oidc.faasx.com/.well-known/openid-configuration";
                //o.RequireHttpsMetadata = false;

                // 使用混合流
                o.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                // 是否将Tokens保存到AuthenticationProperties中
                o.SaveTokens = true;
                // 是否从UserInfoEndpoint获取Claims
                o.GetClaimsFromUserInfoEndpoint = true;
                // 在本示例中，使用的是IdentityServer，而它的ClaimType使用的是JwtClaimTypes。
                o.TokenValidationParameters.NameClaimType = "name"; //JwtClaimTypes.Name;

                // 以下参数均有对应的默认值，通常无需设置。
                //o.CallbackPath = new PathString("/signin-oidc");
                //o.SignedOutCallbackPath = new PathString("/signout-callback-oidc");
                //o.RemoteSignOutPath = new PathString("/signout-oidc");
                //o.Scope.Add("openid");
                //o.Scope.Add("profile");
                //o.ResponseMode = OpenIdConnectResponseMode.FormPost; 

                /***********************************相关事件***********************************/
                // 未授权时，重定向到OIDC服务器时触发
                //o.Events.OnRedirectToIdentityProvider = context => Task.CompletedTask;

                // 获取到授权码时触发
                //o.Events.OnAuthorizationCodeReceived = context => Task.CompletedTask;
                // 接收到OIDC服务器返回的认证信息（包含Code, ID Token等）时触发
                //o.Events.OnMessageReceived = context => Task.CompletedTask;
                // 接收到TokenEndpoint返回的信息时触发
                //o.Events.OnTokenResponseReceived = context => Task.CompletedTask;
                // 验证Token时触发
                //o.Events.OnTokenValidated = context => Task.CompletedTask;
                // 接收到UserInfoEndpoint返回的信息时触发
                //o.Events.OnUserInformationReceived = context => Task.CompletedTask;
                // 出现异常时触发
                //o.Events.OnAuthenticationFailed = context => Task.CompletedTask;

                // 退出时，重定向到OIDC服务器时触发
                //o.Events.OnRedirectToIdentityProviderForSignOut = context => Task.CompletedTask;
                // OIDC服务器退出后，服务端回调时触发
                //o.Events.OnRemoteSignOut = context => Task.CompletedTask;
                // OIDC服务器退出后，客户端重定向时触发
                //o.Events.OnSignedOutCallbackRedirect = context => Task.CompletedTask;
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


            // 未授权页面
            app.Map("/Account/AccessDenied", builder => builder.Run(async context =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await context.Response.WriteHtmlAsync(async res =>
                {
                    await res.WriteAsync($"<h1>Access Denied for user {HttpResponseExtensions.HtmlEncode(context.User.Identity.Name)} to resource '{HttpResponseExtensions.HtmlEncode(context.Request.Query["ReturnUrl"])}'</h1>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/signout\">退出</a>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/\">首页</a>");
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
                        await res.WriteAsync("<a class=\"btn btn-link\" href=\"/signout\">退出</a>");
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
                if (context.Request.Path == "/" || context.Request.Path == "/favicon.ico")
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
