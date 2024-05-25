namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Represents a mechanism to save and load events from an event store.
/// </summary>
/// <typeparam name="T">The type of aggregate root.</typeparam>
public interface IAggregateRepository<T> where T : IAggregateRoot
{
    /// <summary>
    /// Saves the events of an aggregate to the event store.
    /// </summary>
    /// <param name="aggregateRoot">The aggregate root containing the events to save.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns></returns>
    Task SaveAsync(T aggregateRoot, CancellationToken cancellationToken);

    /// <summary>
    /// Loads an aggregate using a stream of events from the event store.
    /// </summary>
    /// <param name="id">The identifier to group events for the aggregate being loaded.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The aggregate loaded from the event store.</returns>
    Task<T> LoadAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a stream of events from the event store.
    /// </summary>
    /// <param name="id">The identifier to group events for the aggregate being loaded.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns></returns>
    Task<IEnumerable<IEvent>> StreamEventsAsync(string id, CancellationToken cancellationToken);
}