using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.Logging;
using RTChat.Server.Application.Common.Messages;
using RTChat.Server.Application.Common.Services;

namespace RTChat.Server.Application.Common.Behaviours
{
    public class LoggingBehaviour<TRequest> : IRequestPreProcessor<TRequest>
        where TRequest : IBaseRequest
    {
        private readonly ILogger<TRequest> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IIdentityService _identityService;

        public LoggingBehaviour(ILogger<TRequest> logger, ICurrentUserService currentUserService, IIdentityService identityService)
        {
            this._logger = logger;
            this._currentUserService = currentUserService;
            this._identityService = identityService;
        }

        public async Task Process(TRequest request, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.GetUserId() ?? String.Empty;
            var userName = String.Empty;

            if (!String.IsNullOrEmpty(userId))
            {
                userName = await _identityService.GetUsername(userId);
            }

            _logger.LogInformation(LoggingBehaviourMessages.LoggingBehaviourInformationMessage,
                requestName, userId, userName, request);
        }
    }
}