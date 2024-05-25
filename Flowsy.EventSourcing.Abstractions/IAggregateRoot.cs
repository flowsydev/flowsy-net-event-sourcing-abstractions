namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// An aggregate represents a cluster of domain objects that can be treated as a single unit for data changes.
/// The aggregate root is responsible for ensuring the consistency of changes within the aggregate boundaries.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// A value to group all the events associated with this aggregate.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// The current version of this aggregate.
    /// This value shall be incremented each time an event is applied.
    /// </summary>
    long Version { get; }
    
    /// <summary>
    /// Indicates if this aggregate is in a newly created state.
    /// This value must be set to true when this aggregate transitions to its initial state, for instance, when setting the Id property and Version is set to 1.
    /// </summary>
    bool IsNew { get; }
    
    /// <summary>
    /// The list of events currently applied to this aggregate.
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
    /// Clears the list of events associated with this aggregate.
    /// </summary>
    void Flush();
}