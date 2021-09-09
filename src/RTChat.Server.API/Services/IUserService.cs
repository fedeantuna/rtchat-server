using System;
using System.Net.Mail;
using System.Threading.Tasks;
using RTChat.Server.API.Models;

namespace RTChat.Server.API.Services
{
    public interface IUserService
    {
        Task<User> GetUser(String id, TokenResponse tokenResponse);
        Task<User> GetUser(MailAddress mailAddress, TokenResponse tokenResponse);
    }
}