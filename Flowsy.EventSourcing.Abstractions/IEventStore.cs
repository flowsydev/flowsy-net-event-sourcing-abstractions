namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Represents a service used to store and retrieve a stream of events related to a domain entity.
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventStore<TEvent> : IDisposable, IAsyncDisposable
    where TEvent : IEvent
{
    /// <summary>
    /// Saves a single event.
    /// </summary>
    /// <param name="key">The key associated with the event.</param>
    /// <param name="event">The event being saved.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous task.</param>
    /// <returns>An asyncronous task.</returns>
    Task SaveAsync(string key, TEvent @event, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a single event.
    /// </summary>
    /// <param name="key">The key associated with the event.</param>
    /// <param name="event">The event being saved.</param>
    /// <param name="correlationId">The correlation ID for the event.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous task.</param>
    /// <returns>An asyncronous task.</returns>
    Task SaveAsync(string key, TEvent @event, string correlationId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a list of events.
    /// </summary>
    /// <param name="key">The key associated with the events.</param>
    /// <param name="events">The events being saved.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous task.</param>
    /// <returns>An asyncronous task.</returns>
    Task SaveAsync(string key, IEnumerable<TEvent> events, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a list of events.
    /// </summary>
    /// <param name="key">The key associated with the events.</param>
    /// <param name="events">The events being saved.</param>
    /// <param name="correlationId">The correlation ID for the event.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous task.</param>
    /// <returns>An asyncronous task.</returns>
    Task SaveAsync(string key, IEnumerable<TEvent> events, string correlationId, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a list of events associated with the given key.
    /// </summary>
    /// <param name="key">The key associated with the events to load.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous task.</param>
    /// <returns>An asyncronous task that will produce the list of events.</returns>
    Task<IEnumerable<TEvent>> LoadEventsAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a list of event records associated with the given key.
    /// </summary>
    /// <param name="key">The key associated with the events to load.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous task.</param>
    /// <returns>An asyncronous task that will produce the list of event records.</returns>
    Task<IEnumerable<EventRecord<TEvent>>> LoadRecordsAsync(string key, CancellationToken cancellationToken);
}