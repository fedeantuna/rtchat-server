using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

namespace RTChat.Server.API.Middleware
{
    [ExcludeFromCodeCoverage] // Testing adds no value here
    public static class Extensions
    {
        public static IApplicationBuilder UseSignalRAuth(this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseMiddleware<SignalRHubAuth>();
        }
    }
}