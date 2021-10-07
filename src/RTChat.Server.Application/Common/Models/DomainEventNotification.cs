using MediatR;
using RTChat.Server.Domain.Common;

namespace RTChat.Server.Application.Common.Models
{
    public class DomainEventNotification<TDomainEvent> : INotification
        where TDomainEvent : DomainEvent
    {
        public DomainEventNotification(TDomainEvent domainEvent)
        {
            DomainEvent = domainEvent;
        }

        public TDomainEvent DomainEvent { get; }
    }
}