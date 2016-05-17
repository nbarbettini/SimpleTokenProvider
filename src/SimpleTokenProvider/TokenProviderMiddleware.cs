using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpleTokenProvider
{
    /// <summary>
    /// TODO
    /// Bearer authentication middleware component which is added to an HTTP pipeline. This class is not
    /// created by application code directly, instead it is added by calling the the IAppBuilder UseJwtBearerAuthentication
    /// extension method.
    /// </summary>
    public class TokenProviderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TokenProviderOptions _options;
        private readonly ILogger _logger;

        public TokenProviderMiddleware(
            RequestDelegate next,
            IOptions<TokenProviderOptions> options,
            ILoggerFactory loggerFactory)
        {
            _next = next;
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<TokenProviderMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Path.Equals(_options.Path, StringComparison.Ordinal))
            {
                await _next(context);
                return;
            }

            _logger.LogInformation("Handling request: " + context.Request.Path);

            await context.Response.WriteAsync("Woohoo!");
        }
    }
}