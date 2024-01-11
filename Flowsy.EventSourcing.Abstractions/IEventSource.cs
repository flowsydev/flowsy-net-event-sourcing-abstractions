namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Represents an entity that produces events.
/// </summary>
public interface IEventSource
{
    /// <summary>
    /// A value to group all the events associated to this entity.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// The current version of this entity.
    /// This value shall be incremented each time an event is applied.
    /// </summary>
    long Version { get; }
    
    /// <summary>
    /// Indicates if this entity is in a newly created state.
    /// This value must be set to true when this entity transitions to its initial state, for instance, when setting the Id property and Version is set to 1.
    /// </summary>
    bool IsNew { get; }
    
    /// <summary>
    /// The list of events currently applied to this entity.
    /// The list shall be cleared after the events have been saved to an event store. 
    /// </summary>
    IEnumerable<IEvent> Events { get; }
    
    /// <summary>
    /// Notifies that an event has been applied.
    /// </summary>
    event EventHandler<IEvent>? EventApplied;
    
    /// <summary>
    /// Notifies that the event list has been flushed.
    /// </summary>
    event EventHandler? Flushed;
    
    /// <summary>
    /// Clears the list of events associated with this entity.
    /// </summary>
    void Flush();
}