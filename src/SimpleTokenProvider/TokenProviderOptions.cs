using System;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace SimpleTokenProvider
{
    public class TokenProviderOptions
    {
        public string Path { get; set; } = "/token";

        public string Issuer { get; set; }

        public string Audience { get; set; }

        public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(5);

        public SigningCredentials SigningCredentials { get; set; }

        public Func<string, string, ClaimsIdentity> IdentityResolver { get; set; }
            = new Func<string, string, ClaimsIdentity>((u, p) => null); // Default IdentityResolver always returns null (fails)

        public Func<string> NonceGenerator { get; set; }
            = new Func<string>(() => Guid.NewGuid().ToString());
    }
}