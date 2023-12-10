namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Represents the entry point to a cluster of domain objects (aggregate) treated as a single unit.
/// An aggregate root ensures the consistency of changes being made within the aggregate boundary and enforces invariants.
/// </summary>
public abstract class AggregateRoot 
{
    /// <summary>
    /// A value to group all the events associated to this aggregate root.
    /// </summary>
    public string Id { get; protected set; } = default!;
    
    public long Version { get; protected set; }
    
    public bool IsNew { get; protected set; }
    
    
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    private readonly List<IEvent> _events = [];
    
    /// <summary>
    /// The list of events currently applied to this aggregate root.
    /// The list will be cleared after the events have been saved to an event store. 
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public IEnumerable<IEvent> Events => _events;
    
    
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
    }
    
    public virtual void Flush()
    {
        _events.Clear();
    }
}