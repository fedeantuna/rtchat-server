using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RTChat.Server.API.Cache;
using RTChat.Server.API.Constants;
using RTChat.Server.API.Exceptions;
using RTChat.Server.API.Hubs;
using RTChat.Server.API.Models;
using RTChat.Server.API.Services;
using Xunit;

namespace RTChat.Server.API.Tests.Hubs
{
    [ExcludeFromCodeCoverage]
    public class ChatHubTests
    {
        private readonly Mock<IApplicationCache> _applicationCacheMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IUserService> _userServiceMock;

        private readonly Mock<IHubCallerClients<IChatHub>> _hubCallerClientsMock;
        private readonly Mock<HubCallerContext> _hubCallerContextMock;
        private readonly Mock<IGroupManager> _groupManagerMock;

        private readonly ChatHub _sut;

        public ChatHubTests()
        {
            this._applicationCacheMock = new Mock<IApplicationCache>();
            this._tokenServiceMock = new Mock<ITokenService>();
            this._userServiceMock = new Mock<IUserService>();

            this._hubCallerClientsMock = new Mock<IHubCallerClients<IChatHub>>();
            this._hubCallerContextMock = new Mock<HubCallerContext>();
            this._groupManagerMock = new Mock<IGroupManager>();

            this._sut = new ChatHub(this._applicationCacheMock.Object, this._tokenServiceMock.Object, this._userServiceMock.Object)
            {
                Clients = this._hubCallerClientsMock.Object,
                Context = this._hubCallerContextMock.Object,
                Groups = this._groupManagerMock.Object
            };
        }

        #region StartConversation

        [Fact]
        public async Task StartConversation_CallsStartConversationOnCallerClient_WithUser()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpInMemoryCache();
            this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            var user = this.SetUpUserByEmail(email, tokenResponse);
            var chatHubCallerMock = this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            this._hubCallerClientsMock.VerifyGet(hcc => hcc.Caller, Times.Once);
            chatHubCallerMock.Verify(chu => chu.StartConversation(user), Times.Once);
        }

        [Fact]
        public async Task StartConversation_CallsStartConversationOnCallerClient_WithNull_WhenUserIsNull()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpNullUserByEmail(email, tokenResponse);
            var chatHubCallerMock = this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            this._hubCallerClientsMock.VerifyGet(hcc => hcc.Caller, Times.Once);
            chatHubCallerMock.Verify(chu => chu.StartConversation(null), Times.Once);
        }

        [Fact]
        public async Task StartConversation_AddsCurrentUserIdIdToListeningUsersForUser()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            var memoryCache = this.SetUpInMemoryCache();
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            var user = this.SetUpUserByEmail(email, tokenResponse);
            this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            Assert.Collection(memoryCache.Get<List<String>>($"{ApplicationCacheKeys.ListeningUserPrefix}{user.Id}"),
                item => Assert.Equal(currentUserId, item));
        }

        [Fact]
        public async Task StartConversation_SetsUserStatusToOffline_WhenUserStatusIsNull()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpInMemoryCache();
            this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            var user = this.SetUpUserByEmail(email, tokenResponse);
            var chatHubCallerMock = this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            Assert.Equal(Status.Offline, user.Status);
            this._hubCallerClientsMock.VerifyGet(hcc => hcc.Caller, Times.Once);
            chatHubCallerMock.Verify(chu => chu.StartConversation(user), Times.Once);
        }

        [Fact]
        public async Task StartConversation_GetsUserByEmail()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpInMemoryCache();
            this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserByEmail(email, tokenResponse);
            this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
            this._userServiceMock.Verify(us => us.GetUser(It.Is<MailAddress>(ma => ma.Address == email), tokenResponse),
                Times.Once);
        }

        [Fact]
        public async Task StartConversation_ThrowsNullUserIdentifierException_WhenEmailIsNotValid()
        {
            // Arrange
            const String email = "obi-wan";

            this.SetUpUserIdentifier();

            // Act
            Task StartConversation() => this._sut.StartConversation(email);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(StartConversation);
        }

        [Fact]
        public async Task StartConversation_ThrowsNullUserIdentifierException_WhenUserIdentifierInContextIsNull()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpNullUserIdentifier();

            // Act
            Task StartConversation() => this._sut.StartConversation(email);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(StartConversation);
        }

        [Fact]
        public async Task StartConversation_ThrowsNullUserIdentifierException_WhenUserIdentifierInContextIsEmpty()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpEmptyUserIdentifier();

            // Act
            Task StartConversation() => this._sut.StartConversation(email);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(StartConversation);
        }

        #endregion

        #region UpdateUserStatus
        
        [Fact]
        public async Task UpdateUserStatus_CallsSyncCurrentUserStatusOnClientsCurrentUser_WithStatus()
        {
            // Arrange
            const String status = Status.Busy;
            var userIds = new List<String>();

            var memoryCache = this.SetUpInMemoryCache();
            const String email = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, email, tokenResponse);
            memoryCache.Set($"{ApplicationCacheKeys.ListeningUserPrefix}{currentUserId}", userIds);
            this.SetUpClientsUsers(userIds);
            var chatHubClientsUserMock = this.SetUpClientsUser(currentUserId);

            // Act
            await this._sut.UpdateUserStatus(status);

            // Assert
            this._hubCallerClientsMock.Verify(hcc => hcc.User(currentUserId), Times.Once);
            chatHubClientsUserMock.Verify(ch => ch.SyncCurrentUserStatus(status), Times.Once);
        }

        [Fact]
        public async Task UpdateUserStatus_CallsUpdateUserStatusOnListeningUsers_WithUserStatus()
        {
            // Arrange
            const String status = Status.Busy;
            var userIds = new List<String>();

            var memoryCache = this.SetUpInMemoryCache();
            const String email = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, email, tokenResponse);
            memoryCache.Set($"{ApplicationCacheKeys.ListeningUserPrefix}{currentUserId}", userIds);
            var chatHubClientsUsersMock = this.SetUpClientsUsers(userIds);
            this.SetUpClientsUser(currentUserId);

            // Act
            await this._sut.UpdateUserStatus(status);

            // Assert
            this._hubCallerClientsMock.Verify(hcc => hcc.Users(userIds), Times.Once);
            chatHubClientsUsersMock.Verify(ch =>
                ch.UpdateUserStatus(It.Is<UserStatus>(us => us.Status == status && us.UserId == currentUserId)), Times.Once);
        }

        [Fact]
        public async Task UpdateUserStatus_GetsUserById()
        {
            // Arrange
            const String status = Status.Busy;

            this.SetUpInMemoryCache();
            const String email = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, email, tokenResponse);
            this.SetUpClientsGroup(currentUserId);
            this.SetUpClientsUser(currentUserId);

            // Act
            await this._sut.UpdateUserStatus(status);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
            this._userServiceMock.Verify(us => us.GetUser(currentUserId, tokenResponse),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUserStatus_UpdatesUserStatusOnMemoryCache()
        {
            // Arrange
            const String status = Status.Busy;
            var userIds = new List<String>();

            var memoryCache = this.SetUpInMemoryCache();
            const String email = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            var user = this.SetUpUserById(currentUserId, email, tokenResponse);
            memoryCache.Set($"{ApplicationCacheKeys.ListeningUserPrefix}{currentUserId}", userIds);
            this.SetUpClientsUsers(userIds);
            this.SetUpClientsUser(currentUserId);

            await this._sut.UpdateUserStatus(status);
            
            var userByIdCacheEntry = $"{ApplicationCacheKeys.UserPrefix}{currentUserId}";
            var userByEmailCacheEntry = $"{ApplicationCacheKeys.UserPrefix}{email}";

            memoryCache.Set(userByIdCacheEntry, user);
            memoryCache.Set(userByEmailCacheEntry, user);
            
            // Act
            var userById = memoryCache.Get<User>(userByIdCacheEntry);
            var userByEmail = memoryCache.Get<User>(userByEmailCacheEntry);

            // Assert
            Assert.NotNull(userById);
            Assert.NotNull(userByEmail);
            Assert.Equal(status, userById.Status);
            Assert.Equal(status, userByEmail.Status);
        }

        [Fact]
        public async Task UpdateUserStatus_ThrowsNullUserIdentifierException_WhenUserIdentifierIsNull()
        {
            // Arrange
            const String status = Status.Busy;

            this.SetUpNullUserIdentifier();

            // Act
            Task UpdateUserStatus() => this._sut.UpdateUserStatus(status);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(UpdateUserStatus);
        }

        [Fact]
        public async Task UpdateUserStatus_ThrowsNullUserIdentifierException_WhenUserIdentifierIsEmpty()
        {
            // Arrange
            const String status = Status.Busy;

            this.SetUpEmptyUserIdentifier();

            // Act
            Task UpdateUserStatus() => this._sut.UpdateUserStatus(status);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(UpdateUserStatus);
        }

        [Fact]
        public async Task UpdateUserStatus_ThrowsNullUserException_WhenUserIsNull()
        {
            // Arrange
            const String status = Status.Busy;

            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpNullUserById(currentUserId, tokenResponse);

            // Act
            Task UpdateUserStatus() => this._sut.UpdateUserStatus(status);

            // Assert
            await Assert.ThrowsAsync<NullUserException>(UpdateUserStatus);
        }

        #endregion

        #region SendMessage

        [Fact]
        public async Task SendMessage_CallsReceiveMessageOnCallerClient_WithMessage()
        {
            // Arrange
            const String messageContent = "Hello there!";
            var receiverId = Guid.NewGuid().ToString();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = receiverId,
                Content = messageContent
            };

            const String currentUserEmail = "obiwankenobi@jediorder.rep";
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            var sender = this.SetUpUserById(currentUserId, currentUserEmail, tokenResponse);
            var receiver = this.SetUpUserById(receiverId, receiverEmail, tokenResponse);
            this.SetUpClientsUser(receiverId);
            var chatHubCallerMock = this.SetUpClientsCaller();

            // Act
            await this._sut.SendMessage(outgoingMessage);

            // Assert
            this._hubCallerClientsMock.VerifyGet(hcc => hcc.Caller);
            chatHubCallerMock.Verify(ch =>
                ch.ReceiveMessage(It.Is<Message>(m =>
                    m.Sender == sender && m.Receiver == receiver && m.Content == messageContent)), Times.Once);
        }

        [Fact]
        public async Task SendMessage_CallsReceiveMessageOnReceiverUserClient_WithMessage()
        {
            // Arrange
            const String messageContent = "Hello there!";
            var receiverId = Guid.NewGuid().ToString();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = receiverId,
                Content = messageContent
            };

            const String currentUserEmail = "obiwankenobi@jediorder.rep";
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            var sender = this.SetUpUserById(currentUserId, currentUserEmail, tokenResponse);
            var receiver = this.SetUpUserById(receiverId, receiverEmail, tokenResponse);
            var chatHubUserMock = this.SetUpClientsUser(receiverId);
            this.SetUpClientsCaller();

            // Act
            await this._sut.SendMessage(outgoingMessage);

            // Assert
            chatHubUserMock.Verify(ch =>
                ch.ReceiveMessage(It.Is<Message>(m =>
                    m.Sender == sender && m.Receiver == receiver && m.Content == messageContent)), Times.Once);
        }

        [Fact]
        public async Task
            SendMessage_DoesNotCallReceiveMessageOnReceiverUserClient_WhenSenderAndReceiverAreTheSameUser()
        {
            // Arrange
            const String messageContent = "Hello there!";
            var currentUserId = this.SetUpUserIdentifier();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = currentUserId,
                Content = messageContent
            };

            const String currentUserEmail = "obiwankenobi@jediorder.rep";
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var tokenResponse = this.SetUpTokenResponse();
            var sender = this.SetUpUserById(currentUserId, currentUserEmail, tokenResponse);
            var receiver = this.SetUpUserById(currentUserId, receiverEmail, tokenResponse);
            var chatHubUserMock = this.SetUpClientsUser(currentUserId);
            this.SetUpClientsCaller();

            // Act
            await this._sut.SendMessage(outgoingMessage);

            // Assert
            chatHubUserMock.Verify(ch =>
                ch.ReceiveMessage(It.Is<Message>(m =>
                    m.Sender == sender && m.Receiver == receiver && m.Content == messageContent)), Times.Never);
        }

        [Fact]
        public async Task SendMessage_GetsUserById_ForCurrentUserAndReceiver()
        {
            // Arrange
            const String messageContent = "Hello there!";
            var receiverId = Guid.NewGuid().ToString();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = receiverId,
                Content = messageContent
            };

            const String currentUserEmail = "obiwankenobi@jediorder.rep";
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, currentUserEmail, tokenResponse);
            this.SetUpUserById(receiverId, receiverEmail, tokenResponse);
            this.SetUpClientsUser(receiverId);
            this.SetUpClientsCaller();

            // Act
            await this._sut.SendMessage(outgoingMessage);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Exactly(2));
            this._userServiceMock.Verify(us => us.GetUser(currentUserId, tokenResponse),
                Times.Once);
            this._userServiceMock.Verify(us => us.GetUser(receiverId, tokenResponse),
                Times.Once);
        }

        [Fact]
        public async Task SendMessage_ThrowsEmptyMessageException_WhenMessageContentIsNull()
        {
            // Arrange
            const String messageContent = null;
            var receiverId = Guid.NewGuid().ToString();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = receiverId,
                Content = messageContent
            };
            
            this.SetUpUserIdentifier();

            // Act
            Task SendMessage() => this._sut.SendMessage(outgoingMessage);

            // Assert
            await Assert.ThrowsAsync<EmptyMessageException>(SendMessage);
        }

        [Fact]
        public async Task SendMessage_ThrowsEmptyMessageException_WhenMessageContentIsEmpty()
        {
            // Arrange
            const String messageContent = "";
            var receiverId = Guid.NewGuid().ToString();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = receiverId,
                Content = messageContent
            };

            this.SetUpUserIdentifier();

            // Act
            Task SendMessage() => this._sut.SendMessage(outgoingMessage);

            // Assert
            await Assert.ThrowsAsync<EmptyMessageException>(SendMessage);
        }

        [Fact]
        public async Task SendMessage_ThrowsEmptyMessageException_WhenMessageContentIsWhiteSpaces()
        {
            // Arrange
            const String messageContent = "     ";
            var receiverId = Guid.NewGuid().ToString();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = receiverId,
                Content = messageContent
            };
            
            this.SetUpUserIdentifier();

            // Act
            Task SendMessage() => this._sut.SendMessage(outgoingMessage);

            // Assert
            await Assert.ThrowsAsync<EmptyMessageException>(SendMessage);
        }

        [Fact]
        public async Task SendMessage_ThrowsNullUserIdentifierException_WhenUserIdentifierIsNull()
        {
            // Arrange
            const String messageContent = "Hello there!";
            var receiverId = Guid.NewGuid().ToString();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = receiverId,
                Content = messageContent
            };

            this.SetUpNullUserIdentifier();

            // Act
            Task SendMessage() => this._sut.SendMessage(outgoingMessage);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(SendMessage);
        }

        [Fact]
        public async Task SendMessage_ThrowsNullUserIdentifierException_WhenUserIdentifierIsEmpty()
        {
            // Arrange
            const String messageContent = "Hello there!";
            var receiverId = Guid.NewGuid().ToString();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = receiverId,
                Content = messageContent
            };

            this.SetUpEmptyUserIdentifier();

            // Act
            Task SendMessage() => this._sut.SendMessage(outgoingMessage);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(SendMessage);
        }

        [Fact]
        public async Task SendMessage_ThrowsNullUserIdentifierException_WhenReceiverIdUserIsNull()
        {
            // Arrange
            const String messageContent = "Hello there!";
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = null,
                Content = messageContent
            };

            this.SetUpUserIdentifier();

            // Act
            Task SendMessage() => this._sut.SendMessage(outgoingMessage);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(SendMessage);
        }

        [Fact]
        public async Task SendMessage_ThrowsNullUserIdentifierException_WhenReceiverIdUserIsEmpty()
        {
            // Arrange
            const String messageContent = "Hello there!";
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = String.Empty,
                Content = messageContent
            };

            this.SetUpUserIdentifier();

            // Act
            Task SendMessage() => this._sut.SendMessage(outgoingMessage);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(SendMessage);
        }

        [Fact]
        public async Task SendMessage_ThrowsNullUserException_WhenCurrentUserIsNull()
        {
            // Arrange
            const String messageContent = "Hello there!";
            var receiverId = Guid.NewGuid().ToString();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = receiverId,
                Content = messageContent
            };

            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpNullUserById(currentUserId, tokenResponse);

            // Act
            Task SendMessage() => this._sut.SendMessage(outgoingMessage);

            // Assert
            await Assert.ThrowsAsync<NullUserException>(SendMessage);
        }

        [Fact]
        public async Task SendMessage_ThrowsNullUserException_WhenReceiverUserIsNull()
        {
            // Arrange
            const String messageContent = "Hello there!";
            var receiverId = Guid.NewGuid().ToString();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = receiverId,
                Content = messageContent
            };

            const String senderEmail = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, senderEmail, tokenResponse);
            this.SetUpNullUserById(receiverId, tokenResponse);

            // Act
            Task SendMessage() => this._sut.SendMessage(outgoingMessage);

            // Assert
            await Assert.ThrowsAsync<NullUserException>(SendMessage);
        }

        #endregion

        #region OnConnectedAsync
        
        [Fact]
        public async Task OnConnectedAsync_CallsSyncCurrentUserStatusOnClientsCurrentUser_WithOnlineUserStatus_WhenThereIsOnlyOneActiveConnection()
        {
            // Arrange
            var userIds = new List<String>();

            var memoryCache = this.SetUpInMemoryCache();
            const String email = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, email, tokenResponse);
            memoryCache.Set($"{ApplicationCacheKeys.ListeningUserPrefix}{currentUserId}", userIds);
            this.SetUpClientsUsers(userIds);
            var chatHubClientsUserMock = this.SetUpClientsUser(currentUserId);

            // Act
            await this._sut.OnConnectedAsync();

            // Assert
            this._hubCallerClientsMock.Verify(hcc => hcc.User(currentUserId), Times.Once);
            chatHubClientsUserMock.Verify(ch => ch.SyncCurrentUserStatus(Status.Online), Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_CallsUpdateUserStatusOnListeningUsers_WithOnlineUserStatus_WhenThereIsOnlyOneActiveConnection()
        {
            // Arrange
            var userIds = new List<String>();

            var memoryCache = this.SetUpInMemoryCache();
            const String email = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, email, tokenResponse);
            memoryCache.Set($"{ApplicationCacheKeys.ListeningUserPrefix}{currentUserId}", userIds);
            var chatHubClientsUsersMock = this.SetUpClientsUsers(userIds);
            this.SetUpClientsUser(currentUserId);

            // Act
            await this._sut.OnConnectedAsync();

            // Assert
            this._hubCallerClientsMock.Verify(hcc => hcc.Users(userIds), Times.Once);
            chatHubClientsUsersMock.Verify(ch =>
                ch.UpdateUserStatus(It.Is<UserStatus>(us => us.Status == Status.Online && us.UserId == currentUserId)), Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_GetsUserById_WhenThereIsOnlyOneActiveConnection()
        {
            // Arrange
            this.SetUpInMemoryCache();
            const String email = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, email, tokenResponse);
            this.SetUpClientsGroup(currentUserId);
            this.SetUpClientsUser(currentUserId);

            // Act
            await this._sut.OnConnectedAsync();

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
            this._userServiceMock.Verify(us => us.GetUser(currentUserId, tokenResponse),
                Times.Once);
        }
        
        [Fact]
        public async Task OnConnectedAsync_DoesNotCallUpdateUserStatus_WhenThereIsNotOnlyOneActiveConnection()
        {
            // Arrange
            var memoryCache = this.SetUpInMemoryCache();
            const String email = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, email, tokenResponse);
            this.SetUpClientsGroup(currentUserId);
            var chatHubClientsUsersMock = this.SetUpClientsUsers(new List<String>());
            
            var entryCache = $"{ApplicationCacheKeys.ActiveConnectionsForUserPrefix}{currentUserId}";
            memoryCache.Set(entryCache, 1);

            // Act
            await this._sut.OnConnectedAsync();

            // Assert
            this._hubCallerClientsMock.Verify(hcc => hcc.Users(It.IsAny<List<String>>()), Times.Never);
            chatHubClientsUsersMock.Verify(ch => ch.UpdateUserStatus(It.IsAny<UserStatus>()), Times.Never);
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Never);
            this._userServiceMock.Verify(us => us.GetUser(It.IsAny<String>(), It.IsAny<TokenResponse>()), Times.Never);
        }

        [Fact]
        public async Task OnConnectedAsync_ThrowsNullUserIdentifierException_WhenUserIdentifierIsNull()
        {
            // Arrange
            this.SetUpNullUserIdentifier();

            // Act
            Task OnConnectedAsync() => this._sut.OnConnectedAsync();

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(OnConnectedAsync);
        }

        [Fact]
        public async Task OnConnectedAsync_ThrowsNullUserIdentifierException_WhenUserIdentifierIsEmpty()
        {
            // Arrange
            this.SetUpEmptyUserIdentifier();

            // Act
            Task OnConnectedAsync() => this._sut.OnConnectedAsync();

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(OnConnectedAsync);
        }

        [Fact]
        public async Task OnConnectedAsync_ThrowsNullUserException_WhenUserIsNull()
        {
            // Arrange
            this.SetUpInMemoryCache();
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpNullUserById(currentUserId, tokenResponse);

            // Act
            Task OnConnectedAsync() => this._sut.OnConnectedAsync();

            // Assert
            await Assert.ThrowsAsync<NullUserException>(OnConnectedAsync);
        }

        #endregion

        #region OnDisconnectedAsync

        [Fact]
        public async Task OnDisconnectedAsync_CallsSyncCurrentUserStatusOnClientsCurrentUser_WithUserStatusOffline_WhenThereAreNoActiveConnectionsLeft()
        {
            // Arrange
            var userIds = new List<String>();

            var memoryCache = this.SetUpInMemoryCache();
            const String email = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, email, tokenResponse);
            memoryCache.Set($"{ApplicationCacheKeys.ListeningUserPrefix}{currentUserId}", userIds);
            this.SetUpClientsUsers(userIds);
            var chatHubClientsUserMock = this.SetUpClientsUser(currentUserId);
            
            var entryCache = $"{ApplicationCacheKeys.ActiveConnectionsForUserPrefix}{currentUserId}";
            memoryCache.Set(entryCache, 1);

            // Act
            await this._sut.OnDisconnectedAsync(null);

            // Assert
            this._hubCallerClientsMock.Verify(hcc => hcc.User(currentUserId), Times.Once);
            chatHubClientsUserMock.Verify(ch => ch.SyncCurrentUserStatus(Status.Offline), Times.Once);
        }
        
        [Fact]
        public async Task OnDisconnectedAsync_CallsUpdateUserStatusOnListeningUsers_WithUserStatusOffline_WhenThereAreNoActiveConnectionsLeft()
        {
            // Arrange
            var userIds = new List<String>();

            var memoryCache = this.SetUpInMemoryCache();
            const String email = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, email, tokenResponse);
            memoryCache.Set($"{ApplicationCacheKeys.ListeningUserPrefix}{currentUserId}", userIds);
            var chatHubClientsUsersMock = this.SetUpClientsUsers(userIds);
            this.SetUpClientsUser(currentUserId);
            
            var entryCache = $"{ApplicationCacheKeys.ActiveConnectionsForUserPrefix}{currentUserId}";
            memoryCache.Set(entryCache, 1);

            // Act
            await this._sut.OnDisconnectedAsync(null);

            // Assert
            this._hubCallerClientsMock.Verify(hcc => hcc.Users(userIds), Times.Once);
            chatHubClientsUsersMock.Verify(ch =>
                ch.UpdateUserStatus(It.Is<UserStatus>(us => us.Status == Status.Offline && us.UserId == currentUserId)), Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_GetsUserById_WhenThereAreNoActiveConnectionsLeft()
        {
            // Arrange
            var memoryCache = this.SetUpInMemoryCache();
            const String email = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, email, tokenResponse);
            this.SetUpClientsGroup(currentUserId);
            this.SetUpClientsUser(currentUserId);
            
            var entryCache = $"{ApplicationCacheKeys.ActiveConnectionsForUserPrefix}{currentUserId}";
            memoryCache.Set(entryCache, 1);

            // Act
            await this._sut.OnDisconnectedAsync(null);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
            this._userServiceMock.Verify(us => us.GetUser(currentUserId, tokenResponse),
                Times.Once);
        }
        
        [Fact]
        public async Task OnDisconnectedAsync_DoesNotCallUpdateUserStatus_WhenThereIsAtLeastOneActiveConnectionLeft()
        {
            // Arrange
            var memoryCache = this.SetUpInMemoryCache();
            const String email = "obiwankenobi@jediorder.rep";
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpUserById(currentUserId, email, tokenResponse);
            this.SetUpClientsGroup(currentUserId);
            var chatHubClientsUsersMock = this.SetUpClientsUsers(new List<String>());
            
            var entryCache = $"{ApplicationCacheKeys.ActiveConnectionsForUserPrefix}{currentUserId}";
            memoryCache.Set(entryCache, 2);

            // Act
            await this._sut.OnDisconnectedAsync(null);

            // Assert
            this._hubCallerClientsMock.Verify(hcc => hcc.Users(It.IsAny<List<String>>()), Times.Never);
            chatHubClientsUsersMock.Verify(ch => ch.UpdateUserStatus(It.IsAny<UserStatus>()), Times.Never);
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Never);
            this._userServiceMock.Verify(us => us.GetUser(It.IsAny<String>(), It.IsAny<TokenResponse>()), Times.Never);
        }

        [Fact]
        public async Task OnDisconnectedAsync_ThrowsNullUserIdentifierException_WhenUserIdentifierIsNull()
        {
            // Arrange
            this.SetUpNullUserIdentifier();

            // Act
            Task OnDisconnectedAsync() => this._sut.OnDisconnectedAsync(null);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(OnDisconnectedAsync);
        }

        [Fact]
        public async Task OnDisconnectedAsync_ThrowsNullUserIdentifierException_WhenUserIdentifierIsEmpty()
        {
            // Arrange
            this.SetUpEmptyUserIdentifier();

            // Act
            Task OnDisconnectedAsync() => this._sut.OnDisconnectedAsync(null);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(OnDisconnectedAsync);
        }

        [Fact]
        public async Task OnDisconnectedAsync_ThrowsNullUserException_WhenUserIsNull()
        {
            // Arrange
            var memoryCache = this.SetUpInMemoryCache();
            var currentUserId = this.SetUpUserIdentifier();
            var tokenResponse = this.SetUpTokenResponse();
            this.SetUpNullUserById(currentUserId, tokenResponse);
            
            var entryCache = $"{ApplicationCacheKeys.ActiveConnectionsForUserPrefix}{currentUserId}";
            memoryCache.Set(entryCache, 1);

            // Act
            Task OnDisconnectedAsync() => this._sut.OnDisconnectedAsync(null);

            // Assert
            await Assert.ThrowsAsync<NullUserException>(OnDisconnectedAsync);
        }

        #endregion

        #region Private Helpers

        private String SetUpUserIdentifier()
        {
            var userIdentifier = Guid.NewGuid().ToString();

            this._hubCallerContextMock.SetupGet(hcc => hcc.UserIdentifier).Returns(userIdentifier);

            return userIdentifier;
        }

        private void SetUpNullUserIdentifier()
        {
            this._hubCallerContextMock.SetupGet(hcc => hcc.UserIdentifier).Returns(() => null);
        }

        private void SetUpEmptyUserIdentifier()
        {
            this._hubCallerContextMock.SetupGet(hcc => hcc.UserIdentifier).Returns(String.Empty);
        }

        private TokenResponse SetUpTokenResponse()
        {
            var tokenResponse = new TokenResponse
            {
                Scope = "test-scope",
                AccessToken = Guid.NewGuid().ToString(),
                ExpiresIn = 3600,
                TokenType = "test-type"
            };
            this._tokenServiceMock.Setup(ts => ts.GetToken()).ReturnsAsync(tokenResponse);

            return tokenResponse;
        }

        private User SetUpUserByEmail(String email, TokenResponse tokenResponse, String status = null)
        {
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Picture = "some-picture",
                Status = status
            };

            this._userServiceMock
                .Setup(us =>
                    us.GetUser(It.Is<MailAddress>(ma => ma.Address == email), tokenResponse))
                .ReturnsAsync(user);

            return user;
        }

        private void SetUpNullUserByEmail(String email, TokenResponse tokenResponse)
        {
            this._userServiceMock
                .Setup(us =>
                    us.GetUser(It.Is<MailAddress>(ma => ma.Address == email), tokenResponse))
                .ReturnsAsync(() => null);
        }

        private User SetUpUserById(String id, String email, TokenResponse tokenResponse,
            String status = Status.Online)
        {
            var user = new User
            {
                Id = id,
                Email = email,
                Picture = "some-picture",
                Status = status
            };

            this._userServiceMock
                .Setup(us =>
                    us.GetUser(id, tokenResponse))
                .ReturnsAsync(user);

            return user;
        }

        private void SetUpNullUserById(String id, TokenResponse tokenResponse)
        {
            this._userServiceMock
                .Setup(us =>
                    us.GetUser(id, tokenResponse))
                .ReturnsAsync(() => null);
        }

        private Mock<IChatHub> SetUpClientsGroup(String groupName)
        {
            var chatHubGroupMock = new Mock<IChatHub>();
            this._hubCallerClientsMock.Setup(hcc => hcc.Group(groupName)).Returns(chatHubGroupMock.Object);

            return chatHubGroupMock;
        }

        private Mock<IChatHub> SetUpClientsUser(String userId)
        {
            var chatHubUserMock = new Mock<IChatHub>();
            this._hubCallerClientsMock.Setup(hcc => hcc.User(userId)).Returns(chatHubUserMock.Object);

            return chatHubUserMock;
        }

        private Mock<IChatHub> SetUpClientsUsers(IReadOnlyList<String> userIds)
        {
            var chatHubUserMock = new Mock<IChatHub>();
            this._hubCallerClientsMock.Setup(hcc => hcc.Users(userIds)).Returns(chatHubUserMock.Object);

            return chatHubUserMock;
        }

        private Mock<IChatHub> SetUpClientsCaller()
        {
            var chatHubCallerMock = new Mock<IChatHub>();
            this._hubCallerClientsMock.SetupGet(hcc => hcc.Caller).Returns(chatHubCallerMock.Object);

            return chatHubCallerMock;
        }

        private IMemoryCache SetUpInMemoryCache()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            this._applicationCacheMock.SetupGet(ac => ac.MemoryCache).Returns(memoryCache);

            return memoryCache;
        }

        #endregion
    }
}