namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Holds information about events.
/// </summary>
public class EventMetadata
{
    public EventMetadata(string typeName)
    {
        TypeName = typeName;
    }

    /// <summary>
    /// Type name used to create instances of event objects when reading them from an event store.
    /// </summary>
    public string TypeName { get; }
}