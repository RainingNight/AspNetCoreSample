using System.Collections.Generic;
using System.Security.Claims;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;

namespace IdentityServerSample
{
    public class Config
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
            };
        }

        public static IEnumerable<ApiResource> GetApis()
        {
            return new List<ApiResource>
            {
                new ApiResource("api", "Demo API", new[] { JwtClaimTypes.Subject, JwtClaimTypes.Email, JwtClaimTypes.Name, JwtClaimTypes.Role, JwtClaimTypes.PhoneNumber })
            };
        }


        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>
            {
                // RegularWebApplications
                new Client
                {
                    ClientId = "RegularWebApplication",
                    ClientSecrets = { new Secret("secret".Sha256()) },

                    // Client的显示名称（用于登录和权限确认页面）
                    ClientName = "Server-based Regular Web Application Client",
                    // Client的介绍地址（用于权限确认页面）
                    ClientUri = "http://localhost:5002",
                    // Client的Logo（用于权限确认页面）
                    LogoUri = "http://localhost:5002/favicon.ico",

                    RedirectUris = { "http://localhost:5002/signin-oidc" },
                    FrontChannelLogoutUri = "http://localhost:5002/signout-oidc",
                    PostLogoutRedirectUris = { "http://localhost:5002/signout-callback-oidc" },

                    AllowedGrantTypes = GrantTypes.Hybrid,
                    AllowedScopes = { "openid", "profile", "email", "api" },
                    // 允许获取刷新Token
                    AllowOfflineAccess = true,
                    AllowedCorsOrigins = { "http://localhost:5002" },

                    // IdToken的有效期，默认5分钟
                    IdentityTokenLifetime = 300,
                    // AccessToken的有效期，默认1小时
                    AccessTokenLifetime = 3600
                },


                new Client
                {
                    ClientId = "oauth.code",
                    ClientName = "Server-based Client (Code)",

                    RedirectUris = { "http://localhost:5001/signin-oauth" },
                    PostLogoutRedirectUris = { "http://localhost:5001/signout-oauth" },

                    ClientSecrets = { new Secret("secret".Sha256()) },

                    AllowedGrantTypes = GrantTypes.Code,
                    AllowedScopes = { "openid", "profile", "email", "api" },
                    AllowOfflineAccess = true
                },
                new Client
                {
                    ClientId = "oidc.hybrid",
                    ClientName = "Server-based Client (Hybrid)",

                    RedirectUris = { "http://localhost:5002/signin-oidc" },
                    FrontChannelLogoutUri = "http://localhost:5002/signout-oidc",
                    PostLogoutRedirectUris = { "http://localhost:5002/signout-callback-oidc" },

                    ClientSecrets = { new Secret("secret".Sha256()) },

                    AllowedGrantTypes = GrantTypes.Hybrid,
                    AllowedScopes = { "openid", "profile", "email", "api" },
                    AllowOfflineAccess = true,
                    AllowAccessTokensViaBrowser = true
                },
                new Client
                {
                    ClientId = "jwt.implicit",
                    ClientName = "Implicit Client (Web)",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,

                    //AccessTokenLifetime = 70,

                    RedirectUris = { "http://localhost:5200/callback" },
                    PostLogoutRedirectUris = { "http://localhost:5200/home" },
                    AllowedCorsOrigins = { "http://localhost:5200" },

                    AllowedScopes = { "openid", "profile", "email", "api" },
                },
                new Client
                {
                    ClientId = "client.cc",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    AllowedScopes = { "api" }
                },
                new Client
                {
                    ClientId = "client.rop",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    AllowedScopes = { "api" }
                }

            };
        }

        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>
            {
                new TestUser{SubjectId = "001", Username = "alice", Password = "alice",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Alice Smith"),
                        new Claim(JwtClaimTypes.GivenName, "Alice"),
                        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                        new Claim(JwtClaimTypes.Email, "AliceSmith@email.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                        new Claim(JwtClaimTypes.Address, @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", IdentityServerConstants.ClaimValueTypes.Json)
                    }
                },
                new TestUser{SubjectId = "002", Username = "bob", Password = "bob",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Bob Smith"),
                        new Claim(JwtClaimTypes.GivenName, "Bob"),
                        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                        new Claim(JwtClaimTypes.Email, "BobSmith@email.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                        new Claim(JwtClaimTypes.Address, @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69119, 'country': 'Germany' }", IdentityServerConstants.ClaimValueTypes.Json),
                        new Claim("location", "somewhere"),
                    }
                },
            };
        }
    }
}
