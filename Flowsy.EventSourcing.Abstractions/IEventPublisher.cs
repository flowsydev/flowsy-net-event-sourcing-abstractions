namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Publishes events to notify another components of the application when relevant facts occur.
/// </summary>
public interface IEventPublisher
{
    
    /// <summary>
    /// Publishes events asynchronously.
    /// </summary>
    /// <param name="events">The events to publish.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken);
    
    /// <summary>
    /// Publishes events without waiting for a task for termination.
    /// </summary>
    /// <param name="events">The events to publish.</param>
    void PublishAndForget(IEnumerable<IEvent> events);
}