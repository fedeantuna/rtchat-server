using System.Threading.Tasks;
using RTChat.Server.API.Models;

namespace RTChat.Server.API.Services
{
    public interface ITokenService
    {
        Task<TokenResponse> GetToken();
    }
}