namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// An aggregate represents a cluster of domain objects that can be treated as a single unit for data changes.
/// The aggregate root is responsible for ensuring the consistency of changes within the aggregate boundaries.
/// </summary>
public abstract class AggregateRoot<TEventBase> where TEventBase : class, IEvent
{
    /// <summary>
    /// A value to group all the events associated with this aggregate.
    /// </summary>
    public string Id { get; protected set; } = string.Empty;
    
    /// <summary>
    /// The current version of this aggregate.
    /// This value shall be incremented each time an event is applied.
    /// </summary>
    public long Version { get; protected set; }
    
    /// <summary>
    /// Indicates if this aggregate is in a newly created state.
    /// This value must be set to true when this aggregate transitions to its initial state, for instance, when setting the Id property and Version is set to 1.
    /// </summary>
    public bool IsNew { get; protected set; }
    
    
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    private readonly List<TEventBase> _events = [];
    
    /// <summary>
    /// The list of events currently applied to this aggregate.
    /// The list shall be cleared after the events have been saved to an event store. 
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public IEnumerable<TEventBase> Events => _events;

    /// <summary>
    /// Notifies that an event has been applied.
    /// </summary>
    public event EventHandler<TEventBase>? EventApplied;
    
    /// <summary>
    /// Notifies that the event list has been flushed.
    /// </summary>
    public event EventHandler? Flushed;
    
    
    /// <summary>
    /// Applies the given event to this aggregate.
    /// </summary>
    /// <param name="event">The event being applied.</param>
    protected abstract void Apply(TEventBase @event);

    /// <summary>
    /// Applies the given event to this aggregate and increments its version number.
    /// The event is added to the list of uncommited events represented by the Events property.
    /// </summary>
    /// <param name="event">The event being applied.</param>
    protected void ApplyChange(TEventBase @event)
    {
        Apply(@event);
        Version++;
        _events.Add(@event);
        EventApplied?.Invoke(this, @event);
    }
    
    /// <summary>
    /// Replays a list of events to this aggregate.
    /// </summary>
    /// <param name="events"></param>
    public void Replay(params TEventBase[] events)
    {
        Version = 0;
        foreach (var @event in events)
        {
            Apply(@event);
            Version++;
            EventApplied?.Invoke(this, @event);
        }
    }
    
    /// <summary>
    /// Clears the list of events associated with this aggregate.
    /// </summary>
    public void Flush()
    {
        _events.Clear();
        Flushed?.Invoke(this, EventArgs.Empty);
    }
}