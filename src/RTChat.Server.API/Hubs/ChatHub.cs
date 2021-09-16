using System;
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
            var user = await this.GetUserByEmail(email);

            if (user != null)
            {
                user.Status ??= Status.Offline;

                await this.Groups.AddToGroupAsync(this.Context.ConnectionId, user.Id);
            }

            await this.Clients.Caller.StartConversation(user);
        }

        public async Task UpdateUserStatus(String status)
        {
            var userId = this.Context.UserIdentifier;

            if (String.IsNullOrEmpty(userId))
            {
                throw new NullUserIdentifierException();
            }

            var user = await this.GetUserById(userId);

            if (user == null)
            {
                throw new NullUserException(userId);
            }
            
            user.Status = status;
            this.SaveUserInApplicationCache(user);

            var userStatus = new UserStatus
            {
                Status = status,
                UserId = userId
            };

            await this.Clients.Group(userId).UpdateUserStatus(userStatus);
        }

        public async Task SendMessage(OutgoingMessage outgoingMessage)
        {
            if (String.IsNullOrEmpty(outgoingMessage.Content) || String.IsNullOrWhiteSpace(outgoingMessage.Content))
            {
                throw new EmptyMessageException();
            }
            
            var senderId = this.Context.UserIdentifier;
            
            if (String.IsNullOrEmpty(senderId) || String.IsNullOrEmpty(outgoingMessage.ReceiverId))
            {
                throw new NullUserIdentifierException();
            }

            var sender = await this.GetUserById(senderId);

            if (sender == null)
            {
                throw new NullUserException(senderId);
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

            if (senderId != receiver.Id)
            {
                await this.Clients.User(receiver.Id).ReceiveMessage(message);
            }

            await this.Clients.Caller.ReceiveMessage(message);
        }

        public override async Task OnConnectedAsync()
        {
            await this.UpdateUserStatus(Status.Online);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await this.UpdateUserStatus(Status.Offline);
        }

        private async Task<User> GetUserByEmail(String email)
        {
            if (!MailAddress.TryCreate(email, out var mailAddress))
            {
                throw new NullUserIdentifierException();
            }

            if (this.TryGetUserFromCache(email, out var user))
            {
                return user;
            }

            var tokenResponse = await this.GetToken();
            user = await this._userService.GetUser(mailAddress, tokenResponse);

            this.SaveUserInApplicationCache(user);

            return user;
        }

        private async Task<User> GetUserById(String userId)
        {
            if (this.TryGetUserFromCache(userId, out var user))
            {
                return user;
            }

            var tokenResponse = await this.GetToken();
            user = await this._userService.GetUser(userId, tokenResponse);

            this.SaveUserInApplicationCache(user);

            return user;
        }

        private Boolean TryGetUserFromCache(String key, out User user)
        {
            return this._applicationCache.MemoryCache.TryGetValue(
                key,
                out user);
        }

        private void SaveUserInApplicationCache(User user)
        {
            if (user == null)
            {
                return;
            }

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSize(ApplicationCacheEntrySizes.User)
                .SetPriority(CacheItemPriority.NeverRemove);

            this._applicationCache.MemoryCache.Set(user.Id, user, cacheEntryOptions);
            this._applicationCache.MemoryCache.Set(user.Email, user, cacheEntryOptions);
        }

        private async Task<TokenResponse> GetToken()
        {
            if (this.TryGetTokenResponseFromCache(out var tokenResponse))
            {
                return tokenResponse;
            }

            tokenResponse = await this._tokenService.GetToken();

            this.SaveTokenResponseInApplicationCache(tokenResponse);

            return tokenResponse;
        }

        private Boolean TryGetTokenResponseFromCache(out TokenResponse tokenResponse)
        {
            return this._applicationCache.MemoryCache.TryGetValue(
                ApplicationCacheKeys.TokenResponse,
                out tokenResponse);
        }

        private void SaveTokenResponseInApplicationCache(TokenResponse tokenResponse)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSize(ApplicationCacheEntrySizes.TokenResponse)
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(tokenResponse.ExpiresIn * 0.9));

            this._applicationCache.MemoryCache.Set(
                ApplicationCacheKeys.TokenResponse,
                tokenResponse,
                cacheEntryOptions);
        }
    }
}