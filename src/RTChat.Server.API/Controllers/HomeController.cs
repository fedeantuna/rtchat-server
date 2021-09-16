using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RTChat.Server.API.Constants;

namespace RTChat.Server.API.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }
        
        public String Index()
        {
            var configurations = new List<String>
            {
                this._configuration[ConfigurationKeys.Auth0Audience],
                this._configuration[ConfigurationKeys.Auth0Domain],
                this._configuration[ConfigurationKeys.CorsAllowedOrigins],
                this._configuration[ConfigurationKeys.Auth0ManagementApiAudience],
                this._configuration[ConfigurationKeys.Auth0ManagementApiBaseAddress],
                this._configuration[ConfigurationKeys.Auth0ManagementApiClientId],
                this._configuration[ConfigurationKeys.Auth0ManagementApiTokenEndpoint],
                this._configuration[ConfigurationKeys.InMemoryCacheSizeLimit],
                this._configuration[ConfigurationKeys.Auth0ManagementApiUsersByEmailEndpoint],
                this._configuration[ConfigurationKeys.Auth0ManagementApiUsersByIdEndpoint],
            };

            return String.Join(", ", configurations);
        }
    }
}