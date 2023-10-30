namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Holds information about events.
/// </summary>
public class EventMetadata
{
    public EventMetadata(string version, string eventType, string fullyQualifiedName)
    {
        Version = version;
        EventType = eventType;
        FullyQualifiedName = fullyQualifiedName;
    }

    public string Version { get; }

    /// <summary>
    /// The type of event associated to this metadata.
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Fully qualified name of the .NET type used to create instances of event objects when reading them from an event store.
    /// </summary>
    public string FullyQualifiedName { get; }
}