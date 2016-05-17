using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Builder;
using System.Security.Claims;
using System.Security.Principal;

namespace SimpleTokenProvider.Test
{
    public partial class Startup
    {
        private void ConfigureAuth(IApplicationBuilder app)
        {
            // The secret key every token will be signed with.
            // Keep this safe on the server!
            var secretKey = "mysupersecret_secretkey!123";

            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
                SecurityAlgorithms.HmacSha256);

            app.UseSimpleTokenProvider(new TokenProviderOptions
            {
                Path = "/api/token",
                Audience = "ExampleAudience",
                Issuer = "ExampleIssuer",
                SigningCredentials = signingCredentials,
                IdentityResolver = GetIdentity
            });
        }

        private ClaimsIdentity GetIdentity(string username, string password)
        {
            // Don't do this in production, obviously!
            if (username == "TEST" && password == "TEST123")
            {
                return new ClaimsIdentity(new GenericIdentity(username, "Token"), new Claim[] { });
            }

            // Credentials are invalid, or account doesn't exist
            return null;
        }
    }
}
