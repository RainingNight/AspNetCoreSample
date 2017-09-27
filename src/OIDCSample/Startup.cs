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


            app.Run(async context =>
            {
                var response = context.Response;

                if (context.Request.Path.Equals("/signedout"))
                {
                    await response.WriteHtmlAsync(async res =>
                    {
                        await res.WriteAsync($"<h1>You have been signed out.</h1>");
                        await res.WriteAsync("<a class=\"btn btn-default\" href=\"/\">Home</a>");
                    });
                    return;
                }

                if (context.Request.Path.Equals("/signout"))
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await response.WriteHtmlAsync(async res =>
                    {
                        await res.WriteAsync($"<h1>Signed out {HttpResponseExtensions.HtmlEncode(context.User.Identity.Name)}</h1>");
                        await res.WriteAsync("<a class=\"btn btn-default\" href=\"/\">Home</a>");
                    });
                    return;
                }

                if (context.Request.Path.Equals("/signout-remote"))
                {
                    // Redirects
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties()
                    {
                        RedirectUri = "/signedout"
                    });
                    return;
                }

                if (context.Request.Path.Equals("/Account/AccessDenied"))
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await response.WriteHtmlAsync(async res =>
                    {
                        await res.WriteAsync($"<h1>Access Denied for user {HttpResponseExtensions.HtmlEncode(context.User.Identity.Name)} to resource '{HttpResponseExtensions.HtmlEncode(context.Request.Query["ReturnUrl"])}'</h1>");
                        await res.WriteAsync("<a class=\"btn btn-default\" href=\"/signout\">Sign Out</a>");
                        await res.WriteAsync("<a class=\"btn btn-default\" href=\"/\">Home</a>");
                    });
                    return;
                }

                // DefaultAuthenticateScheme causes User to be set
                // var user = context.User;

                // This is what [Authorize] calls
                var userResult = await context.AuthenticateAsync();
                var user = userResult.Principal;
                var props = userResult.Properties;

                // This is what [Authorize(ActiveAuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme)] calls
                // var user = await context.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);

                // Not authenticated
                if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                {
                    // This is what [Authorize] calls
                    await context.ChallengeAsync();

                    // This is what [Authorize(ActiveAuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme)] calls
                    // await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);

                    return;
                }

                // Authenticated, but not authorized
                if (context.Request.Path.Equals("/restricted") && !user.Identities.Any(identity => identity.HasClaim("special", "true")))
                {
                    await context.ForbidAsync();
                    return;
                }

                if (context.Request.Path.Equals("/refresh"))
                {
                    var refreshToken = props.GetTokenValue("refresh_token");

                    if (string.IsNullOrEmpty(refreshToken))
                    {
                        await response.WriteHtmlAsync(async res =>
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
                    await context.SignInAsync(user, props);

                    await response.WriteHtmlAsync(async res =>
                    {
                        await res.WriteAsync($"<h1>Refreshed.</h1>");
                        await res.WriteAsync("<a class=\"btn btn-default\" href=\"/refresh\">Refresh tokens</a>");
                        await res.WriteAsync("<a class=\"btn btn-default\" href=\"/\">Home</a>");

                        await res.WriteAsync("<h2>Tokens:</h2>");
                        await res.WriteTableHeader(new string[] { "Token Type", "Value" }, props.GetTokens().Select(token => new string[] { token.Name, token.Value }));

                        await res.WriteAsync("<h2>Payload:</h2>");
                        await res.WriteAsync(HtmlEncoder.Default.Encode(payload.ToString()).Replace(",", ",<br>") + "<br>");
                    });

                    return;
                }

                await response.WriteHtmlAsync(async res =>
                {
                    await res.WriteAsync($"<h1>Hello Authenticated User {HttpResponseExtensions.HtmlEncode(user.Identity.Name)}</h1>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/refresh\">Refresh tokens</a>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/restricted\">Restricted</a>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/signout\">Sign Out</a>");
                    await res.WriteAsync("<a class=\"btn btn-default\" href=\"/signout-remote\">Sign Out Remote</a>");

                    await res.WriteAsync("<h2>Claims:</h2>");
                    await res.WriteTableHeader(new string[] { "Claim Type", "Value" }, context.User.Claims.Select(c => new string[] { c.Type, c.Value }));

                    await res.WriteAsync("<h2>Tokens:</h2>");
                    await res.WriteTableHeader(new string[] { "Token Type", "Value" }, props.GetTokens().Select(token => new string[] { token.Name, token.Value }));
                });
            });
        }
    }
}
