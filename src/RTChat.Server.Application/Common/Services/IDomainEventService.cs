using System.Threading;
using System.Threading.Tasks;
using RTChat.Server.Domain.Common;

namespace RTChat.Server.Application.Common.Services
{
    public interface IDomainEventService
    {
        Task Publish(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    }
}