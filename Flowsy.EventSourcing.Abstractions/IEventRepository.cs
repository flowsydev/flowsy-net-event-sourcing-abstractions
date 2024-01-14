namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Represents a mechanism to store and load events from an event store.
/// </summary>
public interface IEventRepository : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Begins a deferred persistence operation.
    /// Events will be appended to the stream but they will be permanently persisted (commited) until SaveChangesAsync is invoked.
    /// </summary>
    void BeginPersistence();
    
    /// <summary>
    /// Stores the events of an event source.
    /// If BeginPersistence was invoked before StoreAsync, events will be appended to the stream but they will be permanently persisted (commited) until SaveChangesAsync is invoked.
    /// </summary>
    /// <param name="eventSource">The event source with events to be persisted.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <typeparam name="TEventSource">The type of the event source.</typeparam>
    Task StoreAsync<TEventSource>(TEventSource eventSource, CancellationToken cancellationToken)
        where TEventSource : class, IEventSource;

    /// <summary>
    /// Completes a deferred persistence operation by permanently saving the events stored using StoreAsync.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns></returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Loads an event source from an event store.
    /// </summary>
    /// <param name="id">The key that groups a related set of events for an event source.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <typeparam name="TEventSource">The type of the event source.</typeparam>
    /// <returns>An instance of the event source if any is found using the provided ID.</returns>
    Task<TEventSource?> LoadAsync<TEventSource>(string id, CancellationToken cancellationToken)
        where TEventSource : class, IEventSource;

    /// <summary>
    /// Loads an event source from an event store.
    /// </summary>
    /// <param name="id">The key that groups a related set of events for an event source.</param>
    /// <param name="configure">Action to configure the loaded event source before usage.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <typeparam name="TEventSource">The type of the event source.</typeparam>
    /// <returns>An instance of the event source if any is found using the provided ID.</returns>
    Task<TEventSource?> LoadAsync<TEventSource>(string id, Action<TEventSource> configure, CancellationToken cancellationToken)
        where TEventSource : class, IEventSource;

    /// <summary>
    /// Loads an event source from an event store.
    /// </summary>
    /// <param name="id">The key that groups a related set of events for an event source.</param>
    /// <param name="fromVersion">If set, queries for events on or from this version.</param>
    /// <param name="toVersion">If set, queries for events up to and including this version.</param>
    /// <param name="timestamp">If set, queries for events captured on or before this timestamp.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <typeparam name="TEventSource">The type of the event source.</typeparam>
    /// <returns>An instance of the event source if any is found using the provided ID.</returns>
    Task<TEventSource?> LoadAsync<TEventSource>(
        string id,
        long? fromVersion = null,
        long? toVersion = null,
        DateTimeOffset? timestamp = null,
        CancellationToken cancellationToken = default
    ) where TEventSource : class, IEventSource;

    /// <summary>
    /// Loads an event source from an event store.
    /// </summary>
    /// <param name="id">The key that groups a related set of events for an event source.</param>
    /// <param name="fromVersion">If set, queries for events on or from this version.</param>
    /// <param name="toVersion">If set, queries for events up to and including this version.</param>
    /// <param name="timestamp">If set, queries for events captured on or before this timestamp.</param>
    /// <param name="configure">Action to configure the loaded event source before usage.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <typeparam name="TEventSource">The type of the event source.</typeparam>
    /// <returns>An instance of the event source if any is found using the provided ID.</returns>
    Task<TEventSource?> LoadAsync<TEventSource>(
        string id,
        long? fromVersion = null,
        long? toVersion = null,
        DateTimeOffset? timestamp = null,
        Action<TEventSource>? configure = null,
        CancellationToken cancellationToken = default
    ) where TEventSource : class, IEventSource;
}