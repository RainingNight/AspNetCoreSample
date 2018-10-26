using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;

namespace IdentityServer4.Services
{
    /// <summary>
    /// Cookie-based session implementation
    /// </summary>
    /// <seealso cref="IdentityServer4.Services.IUserSession" />
    public class DefaultUserSession : IUserSession
    {
        internal const string SessionIdKey = "session_id";
        internal const string ClientListKey = "client_list";

        /// <summary>
        /// The HTTP context accessor
        /// </summary>
        protected readonly IHttpContextAccessor HttpContextAccessor;

        /// <summary>
        /// The schemes
        /// </summary>
        protected readonly IAuthenticationSchemeProvider Schemes;

        /// <summary>
        /// The handlers
        /// </summary>
        protected readonly IAuthenticationHandlerProvider Handlers;

        /// <summary>
        /// The options
        /// </summary>
        protected readonly IdentityServerOptions Options;

        /// <summary>
        /// The clock
        /// </summary>
        protected readonly ISystemClock Clock;

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Gets the HTTP context.
        /// </summary>
        /// <value>
        /// The HTTP context.
        /// </value>
        protected HttpContext HttpContext => HttpContextAccessor.HttpContext;

        /// <summary>
        /// Gets the name of the check session cookie.
        /// </summary>
        /// <value>
        /// The name of the check session cookie.
        /// </value>
        protected string CheckSessionCookieName => Options.Authentication.CheckSessionCookieName;

        /// <summary>
        /// The principal
        /// </summary>
        protected ClaimsPrincipal Principal;

        /// <summary>
        /// The properties
        /// </summary>
        protected AuthenticationProperties Properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultUserSession"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="schemes">The schemes.</param>
        /// <param name="handlers">The handlers.</param>
        /// <param name="options">The options.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="logger">The logger.</param>
        public DefaultUserSession(
            IHttpContextAccessor httpContextAccessor,
            IAuthenticationSchemeProvider schemes,
            IAuthenticationHandlerProvider handlers,
            IdentityServerOptions options,
            ISystemClock clock,
            ILogger<IUserSession> logger)
        {
            HttpContextAccessor = httpContextAccessor;
            Schemes = schemes;
            Handlers = handlers;
            Options = options;
            Clock = clock;
            Logger = logger;
        }

        // todo: remove this in 3.0 and use extension method on http context
        private async Task<string> GetCookieSchemeAsync()
        {
            if (Options.Authentication.CookieAuthenticationScheme != null)
            {
                return Options.Authentication.CookieAuthenticationScheme;
            }

            var defaultScheme = await Schemes.GetDefaultAuthenticateSchemeAsync();
            if (defaultScheme == null)
            {
                throw new InvalidOperationException("No DefaultAuthenticateScheme found.");
            }

            return defaultScheme.Name;
        }

        // we need this helper (and can't call HttpContext.AuthenticateAsync) so we don't run
        // claims transformation when we get the principal. this also ensures that we don't
        // re-issue a cookie that includes the claims from claims transformation.
        //
        // also, by caching the _principal/_properties it allows someone to issue a new
        // cookie (via HttpContext.SignInAsync) and we'll use those new values, rather than
        // just reading the incoming cookie
        //
        // this design requires this to be in DI as scoped

        /// <summary>
        /// Authenticates the authentication cookie for the current HTTP request and caches the user and properties results.
        /// </summary>
        protected virtual async Task AuthenticateAsync()
        {
            if (Principal == null || Properties == null)
            {
                var scheme = await GetCookieSchemeAsync();

                var handler = await Handlers.GetHandlerAsync(HttpContext, scheme);
                if (handler == null)
                {
                    throw new InvalidOperationException($"No authentication handler is configured to authenticate for the scheme: {scheme}");
                }

                var result = await handler.AuthenticateAsync();
                if (result != null && result.Succeeded)
                {
                    Principal = result.Principal;
                    Properties = result.Properties;
                }
            }
        }

        /// <summary>
        /// Creates a session identifier for the signin context and issues the session id cookie.
        /// </summary>
        /// <param name="principal"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// principal
        /// or
        /// properties
        /// </exception>
        public virtual async Task CreateSessionIdAsync(ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            if (principal == null) throw new ArgumentNullException(nameof(principal));
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            var currentSubjectId = (await GetUserAsync())?.GetSubjectId();
            var newSubjectId = principal.GetSubjectId();

            if (!properties.Items.ContainsKey(SessionIdKey) || currentSubjectId != newSubjectId)
            {
                properties.Items[SessionIdKey] = CryptoRandom.CreateUniqueId(16);
            }

            IssueSessionIdCookie(properties.Items[SessionIdKey]);

            Principal = principal;
            Properties = properties;
        }

        /// <summary>
        /// Gets the current authenticated user.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<ClaimsPrincipal> GetUserAsync()
        {
            await AuthenticateAsync();

            return Principal;
        }

        /// <summary>
        /// Gets the current session identifier.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<string> GetSessionIdAsync()
        {
            await AuthenticateAsync();

            if (Properties?.Items.ContainsKey(SessionIdKey) == true)
            {
                return Properties.Items[SessionIdKey];
            }

            return null;
        }

        /// <summary>
        /// Ensures the session identifier cookie asynchronous.
        /// </summary>
        /// <returns></returns>
        public virtual async Task EnsureSessionIdCookieAsync()
        {
            var sid = await GetSessionIdAsync();
            if (sid != null)
            {
                IssueSessionIdCookie(sid);
            }
            else
            {
                await RemoveSessionIdCookieAsync();
            }
        }

        /// <summary>
        /// Removes the session identifier cookie.
        /// </summary>
        /// <returns></returns>
        public virtual Task RemoveSessionIdCookieAsync()
        {
            if (HttpContext.Request.Cookies.ContainsKey(CheckSessionCookieName))
            {
                // only remove it if we have it in the request
                var options = CreateSessionIdCookieOptions();
                options.Expires = Clock.UtcNow.UtcDateTime.AddYears(-1);

                HttpContext.Response.Cookies.Append(CheckSessionCookieName, ".", options);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds a client to the list of clients the user has signed into during their session.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">clientId</exception>
        public virtual async Task AddClientIdAsync(string clientId)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));

            var clients = await GetClientListAsync();
            if (!clients.Contains(clientId))
            {
                var update = clients.ToList();
                update.Add(clientId);

                await SetClientsAsync(update);
            }
        }

        /// <summary>
        /// Gets the list of clients the user has signed into during their session.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IEnumerable<string>> GetClientListAsync()
        {
            var value = await GetClientListPropertyValueAsync();
            try
            {
                return DecodeList(value);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error decoding client list");
                // clear so we don't keep failing
                await SetClientsAsync(null);
            }

            return Enumerable.Empty<string>();
        }

        // client list helpers
        private async Task<string> GetClientListPropertyValueAsync()
        {
            await AuthenticateAsync();

            if (Properties?.Items.ContainsKey(ClientListKey) == true)
            {
                return Properties.Items[ClientListKey];
            }

            return null;
        }

        private async Task SetClientsAsync(IEnumerable<string> clients)
        {
            var value = EncodeList(clients);
            await SetClientListPropertyValueAsync(value);
        }

        private async Task SetClientListPropertyValueAsync(string value)
        {
            await AuthenticateAsync();

            if (Principal == null || Properties == null) throw new InvalidOperationException("User is not currently authenticated");

            if (value == null)
            {
                Properties.Items.Remove(ClientListKey);
            }
            else
            {
                Properties.Items[ClientListKey] = value;
            }

            var scheme = await GetCookieSchemeAsync();
            await HttpContext.SignInAsync(scheme, Principal, Properties);
        }

        private IEnumerable<string> DecodeList(string value)
        {
            if (value.IsPresent())
            {
                var bytes = Base64Url.Decode(value);
                value = Encoding.UTF8.GetString(bytes);
                return ObjectSerializer.FromString<string[]>(value);
            }

            return Enumerable.Empty<string>();
        }

        private string EncodeList(IEnumerable<string> list)
        {
            if (list != null && list.Any())
            {
                var value = ObjectSerializer.ToString(list);
                var bytes = Encoding.UTF8.GetBytes(value);
                value = Base64Url.Encode(bytes);
                return value;
            }

            return null;
        }

        // session id cookie helpers
        private string GetSessionIdCookieValue()
        {
            return HttpContext.Request.Cookies[CheckSessionCookieName];
        }

        private void IssueSessionIdCookie(string sid)
        {
            if (Options.Endpoints.EnableCheckSessionEndpoint)
            {
                if (GetSessionIdCookieValue() != sid)
                {
                    HttpContext.Response.Cookies.Append(
                        Options.Authentication.CheckSessionCookieName,
                        sid,
                        CreateSessionIdCookieOptions());
                }
            }
        }

        private CookieOptions CreateSessionIdCookieOptions()
        {
            var secure = HttpContext.Request.IsHttps;
            var path = HttpContext.GetIdentityServerBasePath().CleanUrlPath();

            var options = new CookieOptions
            {
                HttpOnly = false,
                Secure = secure,
                Path = path,
                IsEssential = true,
                SameSite = SameSiteMode.None
            };

            return options;
        }
    }
}
