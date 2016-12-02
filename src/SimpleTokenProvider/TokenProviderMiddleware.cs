// Copyright (c) Nate Barbettini. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly ILogger _logger;
        private readonly JsonSerializerSettings _serializerSettings;

        public TokenProviderMiddleware(
            RequestDelegate next,
            IOptions<TokenProviderOptions> options,
            TokenValidationParameters tokenValidationParameters,
            ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<TokenProviderMiddleware>();
            _tokenValidationParameters = tokenValidationParameters;

            _options = options.Value;
            if(tokenValidationParameters == null)
            {
                throw new ArgumentNullException(nameof(tokenValidationParameters));
            }
            ThrowIfInvalidOptions(_options);

            _serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
        }

        public Task Invoke(HttpContext context)
        {
            // first figure out whether this is a request for a new token or for a refresh
            bool isCreate = context.Request.Path.Equals(_options.Path, StringComparison.Ordinal);
            bool isRefresh = !isCreate && context.Request.Path.Equals(_options.RefreshPath, StringComparison.Ordinal);
            // If the request path doesn't match, skip
            if (!isCreate && !isRefresh)
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

            _logger.LogInformation($"Handling request for {(isCreate ? "create token": "refresh token")}: " + context.Request.Path);

            if (isCreate)
            {
                return GenerateToken(context);
            }
            else
            {
                return IssueRefreshedToken(context);
            }
        }

        private Task IssueRefreshedToken(HttpContext context)
        {
            try
            {
                // first extract token text from Authorization header
                string authenticationText = context.Request.Headers["Authorization"].ToString();
                int firstSpace = authenticationText.IndexOf(" ");
                string tokenText = authenticationText.Substring(firstSpace + 1);

                var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
                SecurityToken originalToken;
                // validate token using validation parameters
                var claimsi = jwtSecurityTokenHandler.ValidateToken(tokenText, _tokenValidationParameters, out originalToken);
                var now = DateTime.UtcNow;
                // create a new token based on original one 
                // apply new expiration
                var jwt = new JwtSecurityToken(
                 issuer: _options.Issuer,
                 audience: _options.Audience,
                 claims: ((JwtSecurityToken)originalToken).Claims,
                 notBefore: now,
                 expires: now.Add(_options.Expiration),
                 signingCredentials: _options.SigningCredentials);
                return WriteTokenResponse(context, jwt);
            }
            catch
            {
                context.Response.StatusCode = 400;
                return context.Response.WriteAsync("Bad request or invalid token.");
            }
        }

        private async Task GenerateToken(HttpContext context)
        {
            var username = context.Request.Form["username"];
            var password = context.Request.Form["password"];

            var identity = await _options.IdentityResolver(username, password);
            if (identity == null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid username or password.");
                return;
            }

            var now = DateTime.UtcNow;

            // Specifically add the jti (nonce), iat (issued timestamp), and sub (subject/user) claims.
            // You can add other claims here, if you want:
            var claims = new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, await _options.NonceGenerator()),
                new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(now).ToString(), ClaimValueTypes.Integer64)
            };

            // Create the JWT and write it to a string
            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: now.Add(_options.Expiration),
                signingCredentials: _options.SigningCredentials);
            await WriteTokenResponse(context, jwt);
        }

        private async Task WriteTokenResponse(HttpContext context, JwtSecurityToken jwt)
        {
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = new
            {
                access_token = encodedJwt,
                expires_in = (int)_options.Expiration.TotalSeconds
            };

            // Serialize and return the response
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response, _serializerSettings));
        }

        private static void ThrowIfInvalidOptions(TokenProviderOptions options)
        {
            if (string.IsNullOrEmpty(options.Path))
            {
                throw new ArgumentNullException(nameof(TokenProviderOptions.Path));
            }

            if (string.IsNullOrEmpty(options.Issuer))
            {
                throw new ArgumentNullException(nameof(TokenProviderOptions.Issuer));
            }

            if (string.IsNullOrEmpty(options.Audience))
            {
                throw new ArgumentNullException(nameof(TokenProviderOptions.Audience));
            }

            if (options.Expiration == TimeSpan.Zero)
            {
                throw new ArgumentException("Must be a non-zero TimeSpan.", nameof(TokenProviderOptions.Expiration));
            }

            if (options.IdentityResolver == null)
            {
                throw new ArgumentNullException(nameof(TokenProviderOptions.IdentityResolver));
            }

            if (options.SigningCredentials == null)
            {
                throw new ArgumentNullException(nameof(TokenProviderOptions.SigningCredentials));
            }

            if (options.NonceGenerator == null)
            {
                throw new ArgumentNullException(nameof(TokenProviderOptions.NonceGenerator));
            }
        }

        /// <summary>
        /// Get this datetime as a Unix epoch timestamp (seconds since Jan 1, 1970, midnight UTC).
        /// </summary>
        /// <param name="date">The date to convert.</param>
        /// <returns>Seconds since Unix epoch.</returns>
        public static long ToUnixEpochDate(DateTime date) => new DateTimeOffset(date).ToUniversalTime().ToUnixTimeSeconds();
    }
}
