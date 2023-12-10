namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Represents a mechanism to store and load aggregates from an event store.
/// </summary>
public interface IAggregateRepository : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Stores the events of an aggregate.
    /// </summary>
    /// <param name="aggregate">The aggregate root with events to persist.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
    Task StoreAsync<TAggregate>(
        TAggregate aggregate,
        CancellationToken cancellationToken
    ) where TAggregate : AggregateRoot;
    
    /// <summary>
    /// Stores the events of a list of aggregates.
    /// </summary>
    /// <param name="aggregates">A list of aggregate roots with events to persist.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <typeparam name="TAggregate">The type of the aggregate roots.</typeparam>
    Task StoreAsync<TAggregate>(
        IEnumerable<TAggregate> aggregates,
        CancellationToken cancellationToken
    ) where TAggregate : AggregateRoot;
    
    /// <summary>
    /// Loads an aggregate root from an event store.
    /// </summary>
    /// <param name="id">The key that groups a related set of events for an aggregate root.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
    /// <returns>An instance of the aggregate root if any is found using the provided ID.</returns>
    Task<TAggregate?> LoadAsync<TAggregate>(
        string id,
        CancellationToken cancellationToken
    ) where TAggregate : AggregateRoot;

    /// <summary>
    /// Loads an aggregate root from an event store.
    /// </summary>
    /// <param name="id">The key that groups a related set of events for an aggregate root.</param>
    /// <param name="fromVersion">If set, queries for events on or from this version.</param>
    /// <param name="toVersion">If set, queries for events up to and including this version.</param>
    /// <param name="timestamp">If set, queries for events captured on or before this timestamp.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
    /// <returns>An instance of the aggregate root if any is found using the provided ID.</returns>
    Task<TAggregate?> LoadAsync<TAggregate>(
        string id,
        long? fromVersion = null,
        long? toVersion = null,
        DateTimeOffset? timestamp = null,
        CancellationToken cancellationToken = default
    ) where TAggregate : AggregateRoot;
}