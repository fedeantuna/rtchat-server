using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RTChat.Server.API.Middleware
{
    public class SignalRHubAuth
    {
        private readonly RequestDelegate _next;

        public SignalRHubAuth(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var request = httpContext.Request;
                
            if (request.Path.StartsWithSegments("/hub", StringComparison.OrdinalIgnoreCase) &&
                request.Query.TryGetValue("access_token", out var accessToken))
            {
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
            }
            await this._next(httpContext);
        }
    }
}