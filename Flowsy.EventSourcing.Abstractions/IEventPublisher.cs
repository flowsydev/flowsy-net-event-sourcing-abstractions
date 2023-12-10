namespace Flowsy.EventSourcing.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken);
    
    void PublishAndForget(IEnumerable<IEvent> events);
}