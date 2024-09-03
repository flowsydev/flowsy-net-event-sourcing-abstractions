namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Defines virtual methods to publish events to notify another components of the application when relevant facts occur.
/// </summary>
/// <typeparam name="TAggregateRoot">
/// The type of the aggregate root that is the source of the events.
/// </typeparam>
/// <typeparam name="TEventBase">
/// The base type of the events.
/// </typeparam>
public abstract class EventPublisher<TAggregateRoot, TEventBase> : IEventPublisher<TAggregateRoot, TEventBase>
    where TAggregateRoot : AggregateRoot<TEventBase>
    where TEventBase : class, IEvent
{
    /// <summary>
    /// Asynchronously publishes events of the given aggregate root by calling the method overload that accepts a list of events.
    /// </summary>
    /// <param name="aggregate">
    /// The event source with events to publish.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token for the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    public virtual Task PublishAsync(TAggregateRoot aggregate, CancellationToken cancellationToken)
        => PublishAsync(aggregate.Events, cancellationToken);

    /// <summary>
    /// Asynchronously publishes the given events.
    /// </summary>
    /// <param name="events">
    /// The events to publish.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token for the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    public abstract Task PublishAsync(IEnumerable<TEventBase> events, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes events without waiting for a task for termination.
    /// </summary>
    /// <param name="aggregate"></param>
    public virtual void PublishAndForget(TAggregateRoot aggregate)
        => PublishAndForget(aggregate.Events);

    /// <summary>
    /// Publishes events without waiting for a task for termination by calling the method overload that accepts a list of events.
    /// </summary>
    /// <param name="events">
    /// The events to publish.
    /// </param>
    public abstract void PublishAndForget(IEnumerable<TEventBase> events);
}