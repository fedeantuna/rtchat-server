using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using RTChat.Server.API.Cache;
using RTChat.Server.API.Constants;
using RTChat.Server.API.Exceptions;
using RTChat.Server.API.Models;
using RTChat.Server.API.Services;

namespace RTChat.Server.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub<IChatHub>
    {
        private static readonly Object Lock = new();
        
        private readonly IApplicationCache _applicationCache;
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;

        public ChatHub(IApplicationCache applicationCache, ITokenService tokenService, IUserService userService)
        {
            this._applicationCache = applicationCache;
            this._tokenService = tokenService;
            this._userService = userService;
        }

        public async Task StartConversation(String email)
        {
            var currentUserId = this.GetCurrentUserId();

            var user = await this.GetUserByEmail(email);

            if (user != null)
            {
                user.Status ??= Status.Offline;

                this.SetListeningUsers(currentUserId, user.Id);
            }

            await this.Clients.Caller.StartConversation(user);
        }

        public async Task UpdateUserStatus(String status)
        {
            var currentUserId = this.GetCurrentUserId();

            var user = await this.GetUserById(currentUserId);

            if (user == null)
            {
                throw new NullUserException(currentUserId);
            }

            user.Status = status;

            var userStatus = new UserStatus
            {
                Status = status,
                UserId = currentUserId
            };

            Boolean listeningUsersEntryExists;
            List<String> listeningUsers;
            
            lock (Lock)
            {
                var listeningUsersCacheEntry = $"{ApplicationCacheKeys.ListeningUserPrefix}{currentUserId}";
                listeningUsersEntryExists = this._applicationCache.MemoryCache.TryGetValue(listeningUsersCacheEntry, out listeningUsers);

                var currentUserCacheEntry = $"{ApplicationCacheKeys.UserPrefix}{user.Email}";
                this._applicationCache.MemoryCache.Set(currentUserCacheEntry, user,
                    new MemoryCacheEntryOptions().SetSize(ApplicationCacheEntrySizes.User));
            }
            
            if (listeningUsersEntryExists)
            {
                await this.Clients.Users(listeningUsers).UpdateUserStatus(userStatus);
            }

            await this.Clients.User(currentUserId).SyncCurrentUserStatus(userStatus.Status);
        }

        public async Task SendMessage(OutgoingMessage outgoingMessage)
        {
            if (String.IsNullOrEmpty(outgoingMessage.Content) || String.IsNullOrWhiteSpace(outgoingMessage.Content))
            {
                throw new EmptyMessageException();
            }
            
            var currentUserId = this.GetCurrentUserId();

            if (String.IsNullOrEmpty(outgoingMessage.ReceiverId))
            {
                throw new NullUserIdentifierException();
            }

            var sender = await this.GetUserById(currentUserId);

            if (sender == null)
            {
                throw new NullUserException(currentUserId);
            }

            var receiver = await this.GetUserById(outgoingMessage.ReceiverId);

            if (receiver == null)
            {
                throw new NullUserException(outgoingMessage.ReceiverId);
            }

            var message = new Message
            {
                Sender = sender,
                Receiver = receiver,
                Content = outgoingMessage.Content
            };

            if (currentUserId != receiver.Id)
            {
                await this.Clients.User(receiver.Id).ReceiveMessage(message);
            }

            await this.Clients.Caller.ReceiveMessage(message);
        }

        public override async Task OnConnectedAsync()
        {
            var currentUserId = this.GetCurrentUserId();

            Int32 activeConnectionsForUser;
            
            lock (Lock)
            {
                var entryCache = $"{ApplicationCacheKeys.ActiveConnectionsForUserPrefix}{currentUserId}";
                activeConnectionsForUser = this._applicationCache.MemoryCache.GetOrCreate(entryCache, entry =>
                    {
                        entry.SetSize(ApplicationCacheEntrySizes.ActiveConnectionsForUser);

                        return 0;
                    });
                
                this._applicationCache.MemoryCache.Set($"{ApplicationCacheKeys.ActiveConnectionsForUserPrefix}{currentUserId}",
                    ++activeConnectionsForUser,
                    new MemoryCacheEntryOptions().SetSize(ApplicationCacheEntrySizes.ActiveConnectionsForUser));
            }
            
            if (activeConnectionsForUser == 1)
            {
                await this.UpdateUserStatus(Status.Online);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var currentUserId = this.GetCurrentUserId();

            Int32 activeConnectionsForUser;
            
            lock (Lock)
            {
                activeConnectionsForUser =
                    this._applicationCache.MemoryCache.Get<Int32>($"{ApplicationCacheKeys.ActiveConnectionsForUserPrefix}{currentUserId}");
                this._applicationCache.MemoryCache.Set($"{ApplicationCacheKeys.ActiveConnectionsForUserPrefix}{currentUserId}",
                    --activeConnectionsForUser,
                    new MemoryCacheEntryOptions().SetSize(ApplicationCacheEntrySizes.ActiveConnectionsForUser));
            }
            
            if (activeConnectionsForUser == 0)
            {
                await this.UpdateUserStatus(Status.Offline);
            }
        }

        private String GetCurrentUserId()
        {
            var currentUserId = this.Context.UserIdentifier;

            if (String.IsNullOrEmpty(currentUserId))
            {
                throw new NullUserIdentifierException();
            }

            return currentUserId;
        }

        private void SetListeningUsers(String currentUserId, String receiverId)
        {
            List<String> listeningUsersOnReceiver;
            List<String> listeningUsersOnCurrentUser;

            lock (Lock)
            {
                var listeningUsersOnReceiverCacheEntry = $"{ApplicationCacheKeys.ListeningUserPrefix}{receiverId}";
                listeningUsersOnReceiver = this._applicationCache.MemoryCache.GetOrCreate(listeningUsersOnReceiverCacheEntry, entry =>
                {
                    entry.SetSize(ApplicationCacheEntrySizes.ListeningUser);

                    return new List<String>();
                });
                    
                var listeningUsersOnCurrentUserCacheEntry = $"{ApplicationCacheKeys.ListeningUserPrefix}{currentUserId}";
                listeningUsersOnCurrentUser = this._applicationCache.MemoryCache.GetOrCreate(listeningUsersOnCurrentUserCacheEntry, entry =>
                {
                    entry.SetSize(ApplicationCacheEntrySizes.ListeningUser);

                    return new List<String>();
                });
            }

            listeningUsersOnReceiver.Add(currentUserId);
            listeningUsersOnCurrentUser.Add(receiverId);
        }

        private async Task<User> GetUserByEmail(String email)
        {
            if (!MailAddress.TryCreate(email, out var mailAddress))
            {
                throw new NullUserIdentifierException();
            }

            var tokenResponse = await this._tokenService.GetToken();
            var user = await this._userService.GetUser(mailAddress, tokenResponse);

            return user;
        }

        private async Task<User> GetUserById(String userId)
        {
            var tokenResponse = await this._tokenService.GetToken();
            var user = await this._userService.GetUser(userId, tokenResponse);

            return user;
        }
    }
}