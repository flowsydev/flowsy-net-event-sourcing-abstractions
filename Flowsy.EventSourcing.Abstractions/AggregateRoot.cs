namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Represents the entry point to a cluster of domain objects (aggregate) treated as a single unit.
/// An aggregate root ensures the consistency of changes being made within the aggregate boundary and enforces invariants.
/// </summary>
/// <typeparam name="TEvent">
/// The type of the base class for the events supported by the aggregate.
/// </typeparam>
public abstract class AggregateRoot<TEvent> 
    where TEvent : IEvent
{
    private readonly List<TEvent> _events = new();
    
    /// <summary>
    /// Creates an new instance of the aggregate root.
    /// </summary>
    /// <param name="eventPublisher">
    /// An optional service to publish the events applied to this aggregate root.
    /// The event publisher could send notifications to other components in the same application
    /// or even to external services to orchestrate distributed transactions.
    /// </param>
    protected AggregateRoot(IEventPublisher<TEvent>? eventPublisher = null)
    {
        Key = default!;
        EventPublisher = eventPublisher;
    }
    
    /// <summary>
    /// A value to group all the events associated to this aggregate root.
    /// </summary>
    public string Key { get; protected set; }
    
    /// <summary>
    /// The list of events currently applied to this aggregate root.
    /// The list will be cleared after the events have been saved to an event store. 
    /// </summary>
    public IEnumerable<TEvent> Events => _events;
    
    /// <summary>
    /// An optional service to notify another components when the events are saved to an event store.
    /// </summary>
    protected IEventPublisher<TEvent>? EventPublisher { get; }

    /// <summary>
    /// Validates an event before applying it to this aggregate root.
    /// </summary>
    /// <param name="event">The event to validate.</param>
    /// <returns>The validation result.</returns>
    protected virtual EventValidationResult<TEvent> Validate(TEvent @event)
        => new (@event);

    /// <summary>
    /// Asynchronously validates an event before applying it to this aggregate root.
    /// </summary>
    /// <param name="event">The event to validate.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous task.</param>
    /// <returns>An asynchronous task that will produce the validation result.</returns>
    protected virtual Task<EventValidationResult<TEvent>> ValidateAsync(
        TEvent @event,
        CancellationToken cancellationToken
        )
        => Task.Run(() => Validate(@event), cancellationToken);
    
    /// <summary>
    /// Applies the given event to this aggregate root.
    /// </summary>
    /// <param name="event">The event being applied.</param>
    protected abstract void Apply(TEvent @event);

    /// <summary>
    /// Asynchronously applies the given event to this aggregate root.
    /// </summary>
    /// <param name="event">The event being applied.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous task.</param>
    /// <returns>An asynchronous task.</returns>
    protected virtual Task ApplyAsync(TEvent @event, CancellationToken cancellationToken)
        => Task.Run(() => Apply(@event), cancellationToken);
    
    /// <summary>
    /// Validates the given event and then applies it to this aggregate root.
    /// If the validation fails, an EventValidationException&lt;TEvent&gt; is thrown.
    /// </summary>
    /// <param name="event">The event to validate and apply.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous task.</param>
    /// <exception cref="EventValidationException{TEvent}">The exception holding the validation result.</exception>
    protected virtual async Task ApplyChangeAsync(TEvent @event, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateAsync(@event, cancellationToken);
        if (!validationResult.IsSuccessful)
            throw new EventValidationException<TEvent>(validationResult);
        
        await ApplyAsync(@event, cancellationToken);
        _events.Add(@event);
    }

    /// <summary>
    /// Saves the events to an event store.
    /// </summary>
    /// <param name="eventStore">The event store that will save the events.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous task.</param>
    public virtual async Task SaveAsync(IEventStore<TEvent> eventStore, CancellationToken cancellationToken)
    {
        await eventStore.SaveAsync(Key, _events, cancellationToken);
        TryPublishEvents(_events.ToArray());
        _events.Clear();
    }

    /// <summary>
    /// Publishes events of this aggregate root if an event publisher was provided.
    /// </summary>
    /// <param name="events">The events to publish.</param>
    protected virtual void TryPublishEvents(IEnumerable<TEvent> events)
        => EventPublisher?.PublishAsync(Key, events, CancellationToken.None);
    
    /// <summary>
    /// Loads the events associated with the given key and applies them to this aggregate root.
    /// </summary>
    /// <param name="key">The key associated with the events to load.</param>
    /// <param name="eventStore">The store used to load the events.</param>
    /// <param name="cancellationToken">The cancellation token for the asynchronous task.</param>
    public virtual async Task LoadAsync(string key, IEventStore<TEvent> eventStore, CancellationToken cancellationToken)
    {
        var events = await eventStore.LoadEventsAsync(key, cancellationToken);
        foreach (var e in events)
            await ApplyAsync(e, cancellationToken);
    }
}