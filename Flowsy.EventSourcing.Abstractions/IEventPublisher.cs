namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Publishes events to notify another components of the application when relevant facts occur.
/// </summary>
public interface IEventPublisher<in TAggregateRoot, TEventBase>
    where TAggregateRoot : AggregateRoot<TEventBase>
    where TEventBase : class, IEvent
{
    /// <summary>
    /// Publishes events asynchronously.
    /// </summary>
    /// <param name="aggregate">The event source with events to publish.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task PublishAsync(TAggregateRoot aggregate, CancellationToken cancellationToken);
    
    /// <summary>
    /// Publishes events asynchronously.
    /// </summary>
    /// <param name="events">The events to publish.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task PublishAsync(IEnumerable<TEventBase> events, CancellationToken cancellationToken);
    
    /// <summary>
    /// Publishes events without waiting for a task for termination.
    /// </summary>
    /// <param name="aggregate">The event source with events to publish.</param>
    void PublishAndForget(TAggregateRoot aggregate);
    
    /// <summary>
    /// Publishes events without waiting for a task for termination.
    /// </summary>
    /// <param name="events">The events to publish.</param>
    void PublishAndForget(IEnumerable<TEventBase> events);
}