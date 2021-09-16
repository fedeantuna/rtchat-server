using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using System.Threading;
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

            this._sut = new ChatHub(this._applicationCacheMock.Object, this._tokenServiceMock.Object,
                this._userServiceMock.Object)
            {
                Clients = this._hubCallerClientsMock.Object,
                Context = this._hubCallerContextMock.Object,
                Groups = this._groupManagerMock.Object
            };
        }

        [Fact]
        public async Task StartConversation_CallsStartConversationOnCallerClient_WithUser()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            var user = this.SetUpUserByEmailUsingService(email, tokenResponse);
            var chatHubCallerMock = this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            this._hubCallerClientsMock.VerifyGet(hcc => hcc.Caller, Times.Once);
            chatHubCallerMock.Verify(chu => chu.StartConversation(user), Times.Once);
        }

        [Fact]
        public async Task StartConversation_AddsConnectionToUserIdGroup_WhenUserExists()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            var connectionId = this.SetUpConnectionId();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            var user = this.SetUpUserByEmailUsingService(email, tokenResponse);
            this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            this._groupManagerMock.Verify(gm => gm.AddToGroupAsync(connectionId, user.Id, CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task StartConversation_SetsUserStatusToOfflineIsStatusIsNull()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpApplicationMemoryCache();
            this.SetUpTokenResponseUsingApplicationCache();
            var user = this.SetUpUserByEmailUsingApplicationCache(email, null);
            var chatHubCallerMock = this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            Assert.Equal(Status.Offline, user.Status);
            this._hubCallerClientsMock.VerifyGet(hcc => hcc.Caller, Times.Once);
            chatHubCallerMock.Verify(chu => chu.StartConversation(user), Times.Once);
        }

        [Fact]
        public async Task StartConversation_DoesNotAddConnectionToUserIdGroup_WhenUserDoesNotExist()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpNullUserByEmailUsingService(email, tokenResponse);
            this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            this._groupManagerMock.Verify(
                gm => gm.AddToGroupAsync(It.IsAny<String>(), It.IsAny<String>(), CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task StartConversation_UsesTokenService_WhenEntryIsNotInCache()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByEmailUsingService(email, tokenResponse);
            this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
        }
        
        [Fact]
        public async Task StartConversation_SavesEntryInCache_AfterUsingTokenService()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByEmailUsingService(email, tokenResponse);
            this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
            Assert.Equal(tokenResponse, applicationMemoryCache.Get<TokenResponse>(ApplicationCacheKeys.TokenResponse));
        }

        [Fact]
        public async Task StartConversation_UsesCacheInsteadOfTokenService_WhenEntryIsInCache()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            this.SetUpUserByEmailUsingService(email, tokenResponse);
            this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Never);
        }

        [Fact]
        public async Task StartConversation_UsesUserService_WhenEntryIsNotInCache()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            this.SetUpUserByEmailUsingService(email, tokenResponse);
            this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(It.Is<MailAddress>(ma => ma.Address == email), tokenResponse),
                Times.Once);
        }
        
        [Fact]
        public async Task StartConversation_SavesEntriesInCache_AfterUsingUserService()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            var user = this.SetUpUserByEmailUsingService(email, tokenResponse);
            this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(It.Is<MailAddress>(ma => ma.Address == email), tokenResponse),
                Times.Once);
            Assert.Equal(user, applicationMemoryCache.Get<User>(user.Id));
            Assert.Equal(user, applicationMemoryCache.Get<User>(email));
        }

        [Fact]
        public async Task StartConversation_DoesNotSaveEntryInCache_WhenUserIsNull()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpConnectionId();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            this.SetUpNullUserByEmailUsingService(email, tokenResponse);
            this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            Assert.False(applicationMemoryCache.TryGetValue(email, out _));
        }
        
        [Fact]
        public async Task StartConversation_UsesCacheInsteadOfUserService_WhenEntryIsInCache()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";

            this.SetUpApplicationMemoryCache();
            this.SetUpTokenResponseUsingService();
            this.SetUpUserByEmailUsingApplicationCache(email);
            this.SetUpClientsCaller();

            // Act
            await this._sut.StartConversation(email);

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(It.IsAny<MailAddress>(), It.IsAny<TokenResponse>()),
                Times.Never);
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Never);
        }
        
        [Fact]
        public async Task StartConversation_ThrowsNullUserIdentifierException_WhenEmailIsNotValid()
        {
            // Arrange
            const String email = "obi-wan";

            // Act
            Task StartConversation() => this._sut.StartConversation(email);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(StartConversation);
        }

        [Fact]
        public async Task UpdateUserStatus_CallsUpdateUserStatusOnUserIdGroup_WithUserStatus()
        {
            // Arrange
            const String status = Status.Busy;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            var chatHubGroupMock = this.SetUpClientsGroup(userId);

            // Act
            await this._sut.UpdateUserStatus(status);

            // Assert
            this._hubCallerClientsMock.Verify(hcc => hcc.Group(userId), Times.Once);
            chatHubGroupMock.Verify(
                ch => ch.UpdateUserStatus(It.Is<UserStatus>(us => us.Status == status && us.UserId == userId)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUserStatus_UsesTokenService_WhenEntryIsNotInCache()
        {
            // Arrange
            const String status = Status.Busy;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.UpdateUserStatus(status);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
        }

        [Fact]
        public async Task UpdateUserStatus_SavesEntryInCache_AfterUsingTokenService()
        {
            // Arrange
            const String status = Status.Busy;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.UpdateUserStatus(status);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
            Assert.Equal(tokenResponse, applicationMemoryCache.Get<TokenResponse>(ApplicationCacheKeys.TokenResponse));
        }
        
        [Fact]
        public async Task UpdateUserStatus_UsesCacheInsteadOfTokenService_WhenEntryIsInCache()
        {
            // Arrange
            const String status = Status.Busy;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.UpdateUserStatus(status);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Never);
        }

        [Fact]
        public async Task UpdateUserStatus_UsesUserService_WhenEntryIsNotInCache()
        {
            // Arrange
            const String status = Status.Busy;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.UpdateUserStatus(status);

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(userId, tokenResponse),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUserStatus_SavesEntriesInCache_AfterUsingUserService()
        {
            // Arrange
            const String status = Status.Busy;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            var user = this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.UpdateUserStatus(status);

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(userId, tokenResponse),
                Times.Once);
            Assert.Equal(user, applicationMemoryCache.Get<User>(user.Id));
            Assert.Equal(user, applicationMemoryCache.Get<User>(email));
        }
        
        [Fact]
        public async Task UpdateUserStatus_DoesNotSaveEntryInCache_WhenUserIsNull()
        {
            // Arrange
            const String status = Status.Busy;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            this.SetUpNullUserByIdUsingService(userId, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            Task UpdateUserStatus() => this._sut.UpdateUserStatus(status);

            // Assert
            await Assert.ThrowsAsync<NullUserException>(UpdateUserStatus);
            Assert.False(applicationMemoryCache.TryGetValue(userId, out _));
            Assert.False(applicationMemoryCache.TryGetValue(email, out _));
        }
        
        [Fact]
        public async Task UpdateUserStatus_UsesCacheInsteadOfUserService_WhenEntryIsInCache()
        {
            // Arrange
            const String status = Status.Busy;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingApplicationCache(userId, email);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.UpdateUserStatus(status);

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(It.IsAny<String>(), It.IsAny<TokenResponse>()),
                Times.Never);
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Never);
        }

        [Fact]
        public async Task UpdateUserStatus_UpdatesStatusOnCachedEntry()
        {
            // Arrange
            const String status = Status.Busy;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingApplicationCache(userId, email, Status.Away);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.UpdateUserStatus(status);

            // Assert
            Assert.Equal(status, applicationMemoryCache.Get<User>(userId).Status);
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

            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpNullUserByIdUsingService(userId, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            Task UpdateUserStatus() => this._sut.UpdateUserStatus(status);

            // Assert
            await Assert.ThrowsAsync<NullUserException>(UpdateUserStatus);
        }

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

            const String senderEmail = "obiwankenobi@jediorder.rep";
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var senderId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            var sender = this.SetUpUserByIdUsingService(senderId, senderEmail, tokenResponse);
            var receiver = this.SetUpUserByIdUsingService(receiverId, receiverEmail, tokenResponse);
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

            const String senderEmail = "obiwankenobi@jediorder.rep";
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var senderId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            var sender = this.SetUpUserByIdUsingService(senderId, senderEmail, tokenResponse);
            var receiver = this.SetUpUserByIdUsingService(receiverId, receiverEmail, tokenResponse);
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
        public async Task SendMessage_DoesNotCallReceiveMessageOnReceiverUserClient_WhenSenderAndReceiverAreTheSameUser()
        {
            // Arrange
            const String messageContent = "Hello there!";
            var senderId = this.SetUpUserIdentifier();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = senderId,
                Content = messageContent
            };

            const String senderEmail = "obiwankenobi@jediorder.rep";
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            var sender = this.SetUpUserByIdUsingService(senderId, senderEmail, tokenResponse);
            var receiver = this.SetUpUserByIdUsingService(senderId, receiverEmail, tokenResponse);
            var chatHubUserMock = this.SetUpClientsUser(senderId);
            this.SetUpClientsCaller();

            // Act
            await this._sut.SendMessage(outgoingMessage);

            // Assert
            chatHubUserMock.Verify(ch =>
                ch.ReceiveMessage(It.Is<Message>(m =>
                    m.Sender == sender && m.Receiver == receiver && m.Content == messageContent)), Times.Never);
        }
        
        [Fact]
        public async Task SendMessage_UsesTokenService_WhenEntryIsNotInCache()
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
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var senderId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(senderId, senderEmail, tokenResponse);
            this.SetUpUserByIdUsingService(receiverId, receiverEmail, tokenResponse);
            this.SetUpClientsUser(receiverId);
            this.SetUpClientsCaller();

            // Act
            await this._sut.SendMessage(outgoingMessage);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
        }
        
        [Fact]
        public async Task SendMessage_SavesEntryInCache_AfterUsingTokenService()
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
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var senderId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(senderId, senderEmail, tokenResponse);
            this.SetUpUserByIdUsingService(receiverId, receiverEmail, tokenResponse);
            this.SetUpClientsUser(receiverId);
            this.SetUpClientsCaller();

            // Act
            await this._sut.SendMessage(outgoingMessage);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
            Assert.Equal(tokenResponse, applicationMemoryCache.Get<TokenResponse>(ApplicationCacheKeys.TokenResponse));
        }
        
        [Fact]
        public async Task SendMessage_UsesUserService_WhenEntryIsNotInCache()
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
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var senderId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(senderId, senderEmail, tokenResponse);
            this.SetUpUserByIdUsingService(receiverId, receiverEmail, tokenResponse);
            this.SetUpClientsUser(receiverId);
            this.SetUpClientsCaller();

            // Act
            await this._sut.SendMessage(outgoingMessage);

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(senderId, tokenResponse), Times.Once);
            this._userServiceMock.Verify(us => us.GetUser(receiverId, tokenResponse), Times.Once);
        }
        
        [Fact]
        public async Task SendMessage_SavesEntriesInCache_AfterUsingUserService()
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
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var senderId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            var sender = this.SetUpUserByIdUsingService(senderId, senderEmail, tokenResponse);
            var receiver = this.SetUpUserByIdUsingService(receiverId, receiverEmail, tokenResponse);
            this.SetUpClientsUser(receiverId);
            this.SetUpClientsCaller();

            // Act
            await this._sut.SendMessage(outgoingMessage);

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(senderId, tokenResponse), Times.Once);
            this._userServiceMock.Verify(us => us.GetUser(receiverId, tokenResponse), Times.Once);
            Assert.Equal(sender, applicationMemoryCache.Get<User>(senderId));
            Assert.Equal(sender, applicationMemoryCache.Get<User>(senderEmail));
            Assert.Equal(receiver, applicationMemoryCache.Get<User>(receiverId));
            Assert.Equal(receiver, applicationMemoryCache.Get<User>(receiverEmail));
        }
        
        [Fact]
        public async Task SendMessage_DoesNotSaveEntryInCache_WhenSenderUserIsNull()
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
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var senderId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpNullUserByIdUsingService(senderId, tokenResponse);
            this.SetUpUserByIdUsingService(receiverId, receiverEmail, tokenResponse);
            this.SetUpClientsUser(receiverId);
            this.SetUpClientsCaller();

            // Act
            Task SendMessage() => this._sut.SendMessage(outgoingMessage);

            // Assert
            await Assert.ThrowsAsync<NullUserException>(SendMessage);
            Assert.False(applicationMemoryCache.TryGetValue(senderId, out _));
            Assert.False(applicationMemoryCache.TryGetValue(senderEmail, out _));
        }
        
        [Fact]
        public async Task SendMessage_DoesNotSaveEntryInCache_WhenReceiverUserIsNull()
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
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var senderId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(senderId, senderEmail, tokenResponse);
            this.SetUpNullUserByIdUsingService(receiverId, tokenResponse);
            this.SetUpClientsUser(receiverId);
            this.SetUpClientsCaller();

            // Act
            Task SendMessage() => this._sut.SendMessage(outgoingMessage);

            // Assert
            await Assert.ThrowsAsync<NullUserException>(SendMessage);
            Assert.False(applicationMemoryCache.TryGetValue(receiverId, out _));
            Assert.False(applicationMemoryCache.TryGetValue(receiverEmail, out _));
        }

        [Fact]
        public async Task SendMessage_UsesCacheInsteadOfUserService_WhenEntryIsInCache()
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
            const String receiverEmail = "generalgrievous@droidarmy.sep";
            var senderId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingApplicationCache(senderId, senderEmail);
            this.SetUpUserByIdUsingApplicationCache(receiverId, receiverEmail);
            this.SetUpClientsUser(receiverId);
            this.SetUpClientsCaller();

            // Act
            await this._sut.SendMessage(outgoingMessage);

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(senderId, tokenResponse), Times.Never);
            this._userServiceMock.Verify(us => us.GetUser(receiverId, tokenResponse), Times.Never);
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

            this.SetUpNullUserIdentifier();

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

            this.SetUpNullUserIdentifier();

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

            this.SetUpNullUserIdentifier();

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
        public async Task SendMessage_ThrowsNullUserException_WhenSenderUserIsNull()
        {
            // Arrange
            const String messageContent = "Hello there!";
            var receiverId = Guid.NewGuid().ToString();
            var outgoingMessage = new OutgoingMessage
            {
                ReceiverId = receiverId,
                Content = messageContent
            };

            var senderId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpNullUserByIdUsingService(senderId, tokenResponse);

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
            var senderId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(senderId, senderEmail, tokenResponse);
            this.SetUpNullUserByIdUsingService(receiverId, tokenResponse);

            // Act
            Task SendMessage() => this._sut.SendMessage(outgoingMessage);

            // Assert
            await Assert.ThrowsAsync<NullUserException>(SendMessage);
        }
        
        [Fact]
        public async Task OnConnectedAsync_CallsUpdateUserStatusOnUserIdGroup_WithUserStatus()
        {
            // Arrange
            const String status = Status.Online;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            var chatHubGroupMock = this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnConnectedAsync();

            // Assert
            this._hubCallerClientsMock.Verify(hcc => hcc.Group(userId), Times.Once);
            chatHubGroupMock.Verify(
                ch => ch.UpdateUserStatus(It.Is<UserStatus>(us => us.Status == status && us.UserId == userId)),
                Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_UsesTokenService_WhenEntryIsNotInCache()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnConnectedAsync();

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_SavesEntryInCache_AfterUsingTokenService()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnConnectedAsync();

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
            Assert.Equal(tokenResponse, applicationMemoryCache.Get<TokenResponse>(ApplicationCacheKeys.TokenResponse));
        }
        
        [Fact]
        public async Task OnConnectedAsync_UsesCacheInsteadOfTokenService_WhenEntryIsInCache()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnConnectedAsync();

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Never);
        }

        [Fact]
        public async Task OnConnectedAsync_UsesUserService_WhenEntryIsNotInCache()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnConnectedAsync();

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(userId, tokenResponse),
                Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_SavesEntriesInCache_AfterUsingUserService()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            var user = this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnConnectedAsync();

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(userId, tokenResponse),
                Times.Once);
            Assert.Equal(user, applicationMemoryCache.Get<User>(user.Id));
            Assert.Equal(user, applicationMemoryCache.Get<User>(email));
        }
        
        [Fact]
        public async Task OnConnectedAsync_DoesNotSaveEntryInCache_WhenUserIsNull()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            this.SetUpNullUserByIdUsingService(userId, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            Task UpdateUserStatus() => this._sut.OnConnectedAsync();

            // Assert
            await Assert.ThrowsAsync<NullUserException>(UpdateUserStatus);
            Assert.False(applicationMemoryCache.TryGetValue(userId, out _));
            Assert.False(applicationMemoryCache.TryGetValue(email, out _));
        }
        
        [Fact]
        public async Task OnConnectedAsync_UsesCacheInsteadOfUserService_WhenEntryIsInCache()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingApplicationCache(userId, email);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnConnectedAsync();

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(It.IsAny<String>(), It.IsAny<TokenResponse>()),
                Times.Never);
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Never);
        }

        [Fact]
        public async Task OnConnectedAsync_UpdatesStatusOnCachedEntry()
        {
            // Arrange
            const String status = Status.Online;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingApplicationCache(userId, email, Status.Away);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnConnectedAsync();

            // Assert
            Assert.Equal(status, applicationMemoryCache.Get<User>(userId).Status);
        }

        [Fact]
        public async Task OnConnectedAsync_ThrowsNullUserIdentifierException_WhenUserIdentifierIsNull()
        {
            // Arrange
            this.SetUpNullUserIdentifier();

            // Act
            Task UpdateUserStatus() => this._sut.OnConnectedAsync();

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(UpdateUserStatus);
        }

        [Fact]
        public async Task OnConnectedAsync_ThrowsNullUserIdentifierException_WhenUserIdentifierIsEmpty()
        {
            // Arrange
            this.SetUpEmptyUserIdentifier();

            // Act
            Task UpdateUserStatus() => this._sut.OnConnectedAsync();

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(UpdateUserStatus);
        }

        [Fact]
        public async Task OnConnectedAsync_ThrowsNullUserException_WhenUserIsNull()
        {
            // Arrange
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpNullUserByIdUsingService(userId, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            Task UpdateUserStatus() => this._sut.OnConnectedAsync();

            // Assert
            await Assert.ThrowsAsync<NullUserException>(UpdateUserStatus);
        }
        
        [Fact]
        public async Task OnDisconnectedAsync_CallsUpdateUserStatusOnUserIdGroup_WithUserStatus()
        {
            // Arrange
            const String status = Status.Offline;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            var chatHubGroupMock = this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnDisconnectedAsync(null);

            // Assert
            this._hubCallerClientsMock.Verify(hcc => hcc.Group(userId), Times.Once);
            chatHubGroupMock.Verify(
                ch => ch.UpdateUserStatus(It.Is<UserStatus>(us => us.Status == status && us.UserId == userId)),
                Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_UsesTokenService_WhenEntryIsNotInCache()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnDisconnectedAsync(null);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_SavesEntryInCache_AfterUsingTokenService()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnDisconnectedAsync(null);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Once);
            Assert.Equal(tokenResponse, applicationMemoryCache.Get<TokenResponse>(ApplicationCacheKeys.TokenResponse));
        }
        
        [Fact]
        public async Task OnDisconnectedAsync_UsesCacheInsteadOfTokenService_WhenEntryIsInCache()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnDisconnectedAsync(null);

            // Assert
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Never);
        }

        [Fact]
        public async Task OnDisconnectedAsync_UsesUserService_WhenEntryIsNotInCache()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnDisconnectedAsync(null);

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(userId, tokenResponse),
                Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_SavesEntriesInCache_AfterUsingUserService()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            var user = this.SetUpUserByIdUsingService(userId, email, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnDisconnectedAsync(null);

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(userId, tokenResponse),
                Times.Once);
            Assert.Equal(user, applicationMemoryCache.Get<User>(user.Id));
            Assert.Equal(user, applicationMemoryCache.Get<User>(email));
        }
        
        [Fact]
        public async Task OnDisconnectedAsync_DoesNotSaveEntryInCache_WhenUserIsNull()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingApplicationCache();
            this.SetUpNullUserByIdUsingService(userId, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            Task UpdateUserStatus() => this._sut.OnDisconnectedAsync(null);

            // Assert
            await Assert.ThrowsAsync<NullUserException>(UpdateUserStatus);
            Assert.False(applicationMemoryCache.TryGetValue(userId, out _));
            Assert.False(applicationMemoryCache.TryGetValue(email, out _));
        }
        
        [Fact]
        public async Task OnDisconnectedAsync_UsesCacheInsteadOfUserService_WhenEntryIsInCache()
        {
            // Arrange
            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingApplicationCache(userId, email);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnDisconnectedAsync(null);

            // Assert
            this._userServiceMock.Verify(us => us.GetUser(It.IsAny<String>(), It.IsAny<TokenResponse>()),
                Times.Never);
            this._tokenServiceMock.Verify(ts => ts.GetToken(), Times.Never);
        }

        [Fact]
        public async Task OnDisconnectedAsync_UpdatesStatusOnCachedEntry()
        {
            // Arrange
            const String status = Status.Offline;

            const String email = "obiwankenobi@jediorder.rep";
            var userId = this.SetUpUserIdentifier();
            var applicationMemoryCache = this.SetUpApplicationMemoryCache();
            this.SetUpTokenResponseUsingService();
            this.SetUpUserByIdUsingApplicationCache(userId, email, Status.Away);
            this.SetUpClientsGroup(userId);

            // Act
            await this._sut.OnDisconnectedAsync(null);

            // Assert
            Assert.Equal(status, applicationMemoryCache.Get<User>(userId).Status);
        }

        [Fact]
        public async Task OnDisconnectedAsync_ThrowsNullUserIdentifierException_WhenUserIdentifierIsNull()
        {
            // Arrange
            this.SetUpNullUserIdentifier();

            // Act
            Task UpdateUserStatus() => this._sut.OnDisconnectedAsync(null);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(UpdateUserStatus);
        }

        [Fact]
        public async Task OnDisconnectedAsync_ThrowsNullUserIdentifierException_WhenUserIdentifierIsEmpty()
        {
            // Arrange
            this.SetUpEmptyUserIdentifier();

            // Act
            Task UpdateUserStatus() => this._sut.OnDisconnectedAsync(null);

            // Assert
            await Assert.ThrowsAsync<NullUserIdentifierException>(UpdateUserStatus);
        }

        [Fact]
        public async Task OnDisconnectedAsync_ThrowsNullUserException_WhenUserIsNull()
        {
            // Arrange
            var userId = this.SetUpUserIdentifier();
            this.SetUpApplicationMemoryCache();
            var tokenResponse = this.SetUpTokenResponseUsingService();
            this.SetUpNullUserByIdUsingService(userId, tokenResponse);
            this.SetUpClientsGroup(userId);

            // Act
            Task UpdateUserStatus() => this._sut.OnDisconnectedAsync(null);

            // Assert
            await Assert.ThrowsAsync<NullUserException>(UpdateUserStatus);
        }

        private String SetUpConnectionId()
        {
            var connectionId = Guid.NewGuid().ToString();

            this._hubCallerContextMock.SetupGet(hcc => hcc.ConnectionId).Returns(connectionId);

            return connectionId;
        }

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

        private MemoryCache SetUpApplicationMemoryCache()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            this._applicationCacheMock.SetupGet(ac => ac.MemoryCache).Returns(memoryCache);

            return memoryCache;
        }

        private TokenResponse SetUpTokenResponseUsingService()
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

        private TokenResponse SetUpTokenResponseUsingApplicationCache()
        {
            var tokenResponse = new TokenResponse
            {
                Scope = "test-scope",
                AccessToken = Guid.NewGuid().ToString(),
                ExpiresIn = 3600,
                TokenType = "test-type"
            };

            this._applicationCacheMock.Object.MemoryCache.Set(ApplicationCacheKeys.TokenResponse, tokenResponse);

            return tokenResponse;
        }

        private User SetUpUserByEmailUsingService(String email, TokenResponse tokenResponse,
            String status = Status.Online)
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

        private User SetUpUserByEmailUsingApplicationCache(String email, String status = Status.Online)
        {
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Picture = "some-picture",
                Status = status
            };

            this._applicationCacheMock.Object.MemoryCache.Set(user.Id, user);
            this._applicationCacheMock.Object.MemoryCache.Set(user.Email, user);

            return user;
        }

        private void SetUpNullUserByEmailUsingService(String email, TokenResponse tokenResponse)
        {
            this._userServiceMock
                .Setup(us =>
                    us.GetUser(It.Is<MailAddress>(ma => ma.Address == email), tokenResponse))
                .ReturnsAsync(() => null);
        }

        private User SetUpUserByIdUsingService(String id, String email, TokenResponse tokenResponse,
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

        private void SetUpUserByIdUsingApplicationCache(String id, String email, String status = Status.Online)
        {
            var user = new User
            {
                Id = id,
                Email = email,
                Picture = "some-picture",
                Status = status
            };

            this._applicationCacheMock.Object.MemoryCache.Set(user.Id, user);
            this._applicationCacheMock.Object.MemoryCache.Set(user.Email, user);
        }

        private void SetUpNullUserByIdUsingService(String id, TokenResponse tokenResponse)
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

        private Mock<IChatHub> SetUpClientsCaller()
        {
            var chatHubCallerMock = new Mock<IChatHub>();
            this._hubCallerClientsMock.SetupGet(hcc => hcc.Caller).Returns(chatHubCallerMock.Object);

            return chatHubCallerMock;
        }
    }
}