using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SimpleTokenProvider.Test
{
    public partial class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true);
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // The secret key every token will be signed with.
        // In production, you should store this securely in environment variables
        // or a key management tool. Don't hardcode this into your application!
        private static readonly string secretKey = "mysupersecret_secretKey!123";

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);
            loggerFactory.AddDebug();

            app.UseStaticFiles();

            // Add JWT generation endpoint:
            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
            var options = new TokenProviderOptions
            {
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
                Expiration = TimeSpan.FromDays(30)
            };

            app.UseMiddleware<TokenProviderMiddleware>(Options.Create(options));

            app.UseMvc();
        }
    }
}
