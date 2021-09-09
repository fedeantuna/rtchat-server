using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using RTChat.Server.API.Cache;
using RTChat.Server.API.Constants;
using RTChat.Server.API.Hubs;
using RTChat.Server.API.Middleware;
using RTChat.Server.API.Providers;
using RTChat.Server.API.Services;

namespace RTChat.Server.API
{
    [ExcludeFromCodeCoverage] // Testing adds no value here
    public class Startup
    {
        private const String CorsClientPolicyName = "client";
        private const String ChatHubEndpoint = "/hub/chat";

        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();

            services.AddCors(options =>
            {
                options.AddPolicy(CorsClientPolicyName, policy =>
                {
                    var allowedOrigins = this._configuration.GetSection(ConfigurationKeys.CorsAllowedOrigins)
                        .Get<String[]>();

                    policy.AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithOrigins(allowedOrigins)
                        .AllowCredentials();
                });
            });

            services.AddHttpClient(HttpClientNames.Auth0ManagementApi, c =>
            {
                var baseAddress = this._configuration[ConfigurationKeys.Auth0ManagementApiBaseAddress];
                c.BaseAddress = new Uri(baseAddress);
                c.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = this._configuration[ConfigurationKeys.Auth0Domain];
                    options.Audience = this._configuration[ConfigurationKeys.Auth0Audience];

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = ClaimTypes.NameIdentifier
                    };
                });

            services.AddSingleton<IUserIdProvider, UserIdProvider>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITokenService, TokenService>();

            services.AddSingleton<IApplicationCache, ApplicationCache>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseSignalRAuth();

            app.UseCors(CorsClientPolicyName);

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapHub<ChatHub>(ChatHubEndpoint); });
        }
    }
}