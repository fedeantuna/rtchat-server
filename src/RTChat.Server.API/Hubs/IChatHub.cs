using System;
using System.Threading.Tasks;
using RTChat.Server.API.Models;

namespace RTChat.Server.API.Hubs
{
    public interface IChatHub
    {
        Task ReceiveMessage(Message message);
        Task UpdateUserStatus(UserStatus userStatus);
        Task StartConversation(User user);
        Task SyncCurrentUserStatus(String status);
    }
}