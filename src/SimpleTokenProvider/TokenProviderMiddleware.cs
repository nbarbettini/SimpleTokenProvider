// Copyright (c) Nate Barbettini. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace SimpleTokenProvider
{
    /// <summary>
    /// Token generator middleware component which is added to an HTTP pipeline.
    /// This class is not created by application code directly,
    /// instead it is added by calling the <see cref="TokenProviderAppBuilderExtensions.UseSimpleTokenProvider(Microsoft.AspNetCore.Builder.IApplicationBuilder, TokenProviderOptions)"/>
    /// extension method.
    /// </summary>
    public class TokenProviderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TokenProviderOptions _options;

        public TokenProviderMiddleware(
            RequestDelegate next,
            IOptions<TokenProviderOptions> options)
        {
            _next = next;
            _options = options.Value;
        }

        public Task Invoke(HttpContext context)
        {
            // If the request path doesn't match, skip
            if (!context.Request.Path.Equals(_options.Path, StringComparison.Ordinal))
            {
                return _next(context);
            }

            // Request must be POST with Content-Type: application/x-www-form-urlencoded
            if (!context.Request.Method.Equals("POST")
               || !context.Request.HasFormContentType)
            {
                context.Response.StatusCode = 400;
                return context.Response.WriteAsync("Bad request.");
            }

            return GenerateToken(context);
        }

        private async Task GenerateToken(HttpContext context)
        {
            var username = context.Request.Form["username"];
            var password = context.Request.Form["password"];

            var identity = await GetIdentity(username, password);
            if (identity == null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid username or password.");
                return;
            }

            var now = DateTimeOffset.UtcNow;

            // Specifically add the jti (random nonce), iat (issued timestamp), and sub (subject/user) claims.
            // You can add other claims here, if you want:
            var claims = new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Create the JWT and write it to a string
            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now.LocalDateTime,
                expires: now.LocalDateTime.Add(_options.Expiration),
                signingCredentials: _options.SigningCredentials);
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = new
            {
                access_token = encodedJwt,
                expires_in = (int)_options.Expiration.TotalSeconds
            };

            // Serialize and return the response
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response, new JsonSerializerSettings { Formatting = Formatting.Indented }));
        }

        private Task<ClaimsIdentity> GetIdentity(string username, string password)
        {
            // DON'T do this in production, obviously!
            if (username == "TEST" && password == "TEST123")
            {
                return Task.FromResult(new ClaimsIdentity(new System.Security.Principal.GenericIdentity(username, "Token"), new Claim[] { }));
            }

            // Credentials are invalid, or account doesn't exist
            return Task.FromResult<ClaimsIdentity>(null);
        }
    }
}