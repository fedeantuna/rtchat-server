using System;

namespace RTChat.Server.Infrastructure.Messages
{
    public static class DomainEventServiceMessages
    {
        public const String DomainEventServiceInformationMessageEventParameter = "Event";
        public static readonly String DomainEventServiceInformationMessage = $"Publishing domain event. Event - {{{DomainEventServiceInformationMessageEventParameter}}}";
    }
}