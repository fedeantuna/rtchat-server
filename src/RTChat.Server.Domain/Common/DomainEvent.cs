using System;

namespace RTChat.Server.Domain.Common
{
    public abstract class DomainEvent
    {
        protected DomainEvent(DateTimeOffset now)
        {
            this.OccurredAt = now;
        }

        public DateTimeOffset OccurredAt { get; private set; }

        public Boolean IsPublished { get; set; }
    }
}