using System;
using System.Threading.Tasks;

namespace RTChat.Server.Application.Common.Services
{
    public interface IIdentityService
    {
        Task<String> GetUsername(String userId);

        Task<Boolean> IsInRole(String userId, String role);

        Task<Boolean> Authorize(String userId, String policy);
    }
}