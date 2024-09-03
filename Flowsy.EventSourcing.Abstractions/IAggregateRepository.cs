namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Represents a mechanism to save and load events from an event store.
/// </summary>
/// <typeparam name="TAggregateRoot">
/// The type of aggregate root.
/// </typeparam>
/// <typeparam name="TEventBase">
/// The base type for events that can be applied to the aggregate root.
/// </typeparam>
public interface IAggregateRepository<TAggregateRoot, TEventBase>
    where TAggregateRoot : AggregateRoot<TEventBase>
    where TEventBase : class, IEvent
{
    /// <summary>
    /// Saves the events of an aggregate to the event store.
    /// </summary>
    /// <param name="aggregate">The aggregate root containing the events to save.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns></returns>
    Task SaveAsync(TAggregateRoot aggregate, CancellationToken cancellationToken);

    /// <summary>
    /// Loads an aggregate using a stream of events from the event store.
    /// </summary>
    /// <param name="aggregateId">The identifier to group events for the aggregate being loaded.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The aggregate loaded from the event store.</returns>
    Task<TAggregateRoot?> LoadAsync(string aggregateId, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a stream of events from the event store.
    /// </summary>
    /// <param name="aggregateId">The identifier to group events for the aggregate being loaded.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns></returns>
    Task<IEnumerable<IEvent>> StreamEventsAsync(string aggregateId, CancellationToken cancellationToken);
}