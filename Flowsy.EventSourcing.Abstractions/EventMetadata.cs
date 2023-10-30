namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Holds information about events.
/// </summary>
public class EventMetadata
{
    public EventMetadata(string eventName, string typeName)
    {
        EventName = eventName;
        TypeName = typeName;
    }

    /// <summary>
    /// The name of the event associated to TypeName.
    /// </summary>
    public string EventName { get; }

    /// <summary>
    /// Type name used to create instances of event objects when reading them from an event store.
    /// </summary>
    public string TypeName { get; }
}