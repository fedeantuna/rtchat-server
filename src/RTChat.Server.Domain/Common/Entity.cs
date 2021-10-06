using System;

namespace RTChat.Server.Domain.Common
{
    public abstract class Entity
    {
        protected Entity(Guid id)
        {
            this.Id = id;
        }
        
        public Guid Id { get; private set; }

        public DateTime CreatedAt { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime? LastModifiedAt { get; set; }

        public Guid LastModifiedBy { get; set; }
    }
}