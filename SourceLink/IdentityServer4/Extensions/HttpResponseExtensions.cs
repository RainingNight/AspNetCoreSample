// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace Microsoft.AspNetCore.Http
{
    public static class HttpResponseExtensions
    {
        public static async Task WriteJsonAsync(this HttpResponse response, object o, string contentType = null)
        {
            var json = ObjectSerializer.ToString(o);
            await response.WriteJsonAsync(json, contentType);
        }

        public static async Task WriteJsonAsync(this HttpResponse response, string json, string contentType = null)
        {
            response.ContentType = contentType ?? "application/json; charset=UTF-8";
            await response.WriteAsync(json);
        }

        public static void SetCache(this HttpResponse response, int maxAge)
        {
            if (maxAge == 0)
            {
                SetNoCache(response);
            }
            else if (maxAge > 0)
            {
                if (!response.Headers.ContainsKey("Cache-Control"))
                {
                    response.Headers.Add("Cache-Control", $"max-age={maxAge}");
                }
            }
        }

        public static void SetNoCache(this HttpResponse response)
        {
            if (!response.Headers.ContainsKey("Cache-Control"))
            {
                response.Headers.Add("Cache-Control", "no-store, no-cache, max-age=0");
            }
            else
            {
                response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
            }

            if (!response.Headers.ContainsKey("Pragma"))
            {
                response.Headers.Add("Pragma", "no-cache");
            }
        }

        public static async Task WriteHtmlAsync(this HttpResponse response, string html)
        {
            response.ContentType = "text/html; charset=UTF-8";
            await response.WriteAsync(html, Encoding.UTF8);
        }

        public static void RedirectToAbsoluteUrl(this HttpResponse response, string url)
        {
            if (url.IsLocalUrl())
            {
                if (url.StartsWith("~/")) url = url.Substring(1);
                url = response.HttpContext.GetIdentityServerBaseUrl().EnsureTrailingSlash() + url.RemoveLeadingSlash();
            }
            response.Redirect(url);
        }

        public static void AddScriptCspHeaders(this HttpResponse response, CspOptions options, string hash)
        {
            var csp1part = options.Level == CspLevel.One ? "'unsafe-inline' " : string.Empty;
            var cspHeader = $"default-src 'none'; script-src {csp1part}'{hash}'";

            AddCspHeaders(response.Headers, options, cspHeader);
        }

        public static void AddStyleCspHeaders(this HttpResponse response, CspOptions options, string hash, string frameSources)
        {
            var csp1part = options.Level == CspLevel.One ? "'unsafe-inline' " : string.Empty;
            var cspHeader = $"default-src 'none'; style-src {csp1part}'{hash}'";

            if (!string.IsNullOrEmpty(frameSources))
            {
                cspHeader += $"; frame-src {frameSources}";
            }

            AddCspHeaders(response.Headers, options, cspHeader);
        }

        public static void AddCspHeaders(IHeaderDictionary headers, CspOptions options, string cspHeader)
        {
            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                headers.Add("Content-Security-Policy", cspHeader);
            }
            if (options.AddDeprecatedHeader && !headers.ContainsKey("X-Content-Security-Policy"))
            {
                headers.Add("X-Content-Security-Policy", cspHeader);
            }
        }
    }
}