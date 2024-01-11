namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Represents an entity
/// </summary>
public abstract class EventSource : IEventSource
{
    /// <summary>
    /// A value to group all the events associated to this entity.
    /// </summary>
    public string Id { get; protected set; } = default!;
    
    /// <summary>
    /// The current version of this entity.
    /// This value shall be incremented each time an event is applied.
    /// </summary>
    public long Version { get; protected set; }
    
    /// <summary>
    /// Indicates if this entity is in a newly created state.
    /// This value must be set to true when this entity transitions to its initial state, for instance, when setting the Id property and Version is set to 1.
    /// </summary>
    public bool IsNew { get; protected set; }
    
    
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    private readonly List<IEvent> _events = [];
    
    /// <summary>
    /// The list of events currently applied to this entity.
    /// The list shall be cleared after the events have been saved to an event store. 
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public IEnumerable<IEvent> Events => _events;

    /// <summary>
    /// Notifies that an event has been applied.
    /// </summary>
    public event EventHandler<IEvent>? EventApplied;
    
    /// <summary>
    /// Notifies that the event list has been flushed.
    /// </summary>
    public event EventHandler? Flushed;
    
    
    /// <summary>
    /// Applies the given event to this aggregate root.
    /// </summary>
    /// <param name="event">The event being applied.</param>
    protected abstract void Apply(IEvent @event);

    /// <summary>
    /// Applies the given event to this aggregate root and increments its version number.
    /// The event is added to the list of uncommited events represented by the Events property.
    /// </summary>
    /// <param name="event">The event being applied.</param>
    protected virtual void ApplyChange(IEvent @event)
    {
        Apply(@event);
        Version++;
        _events.Add(@event);
        EventApplied?.Invoke(this, @event);
    }
    
    /// <summary>
    /// Clears the list of events associated with this entity.
    /// </summary>
    public virtual void Flush()
    {
        _events.Clear();
        Flushed?.Invoke(this, EventArgs.Empty);
    }
}