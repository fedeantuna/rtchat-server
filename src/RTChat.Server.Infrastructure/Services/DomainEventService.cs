using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using RTChat.Server.Application.Common.Models;
using RTChat.Server.Application.Common.Services;
using RTChat.Server.Domain.Common;
using RTChat.Server.Infrastructure.Messages;

namespace RTChat.Server.Infrastructure.Services
{
    public class DomainEventService : IDomainEventService
    {
        private readonly ILogger<DomainEventService> _logger;
        private readonly IPublisher _mediatR;

        public DomainEventService(ILogger<DomainEventService> logger, IPublisher mediatR)
        {
            this._logger = logger;
            this._mediatR = mediatR;
        }
        
        public async Task Publish(DomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            this._logger.LogInformation(DomainEventServiceMessages.DomainEventServiceInformationMessage, domainEvent.GetType().Name);
            await this._mediatR.Publish(GetNotificationCorrespondingToDomainEvent(domainEvent), cancellationToken);
        }
        
        private static INotification GetNotificationCorrespondingToDomainEvent(DomainEvent domainEvent)
        {
            return (INotification)Activator.CreateInstance(
                typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()), domainEvent);
        }
    }
}