using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using RTChat.Server.Application.Common.Messages;

namespace RTChat.Server.Application.Common.Behaviours
{
    public class UnhandledExceptionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<TRequest> _logger;
        
        public UnhandledExceptionBehaviour(ILogger<TRequest> logger)
        {
            this._logger = logger;
        }
        
        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            try
            {
                return await next();
            }
            catch (Exception ex)
            {
                var requestName = typeof(TRequest).Name;
                
                this._logger.LogError(ex, UnhandledExceptionBehaviourMessages.UnhandledExceptionErrorMessage, requestName, request);
                
                throw;
            }
        }
    }
}