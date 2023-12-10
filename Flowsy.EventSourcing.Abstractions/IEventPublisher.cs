namespace Flowsy.EventSourcing.Abstractions;

public interface IEventPublisher<in TEvent> where TEvent : IEvent
{
    Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken);
    
    void PublishAndForget(IEnumerable<IEvent> events);
}