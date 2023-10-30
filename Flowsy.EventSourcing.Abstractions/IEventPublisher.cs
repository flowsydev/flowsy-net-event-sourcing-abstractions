namespace Flowsy.EventSourcing.Abstractions;

public interface IEventPublisher<in TEvent> where TEvent : IEvent
{
    Task PublishAsync(string key, IEnumerable<TEvent> events, CancellationToken cancellationToken);
}