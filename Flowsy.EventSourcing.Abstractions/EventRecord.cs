namespace Flowsy.EventSourcing.Abstractions;

public sealed class EventRecord<TPayload>
    where TPayload : IEvent
{
    public EventRecord(
        long id,
        string key,
        string type,
        TPayload payload,
        EventMetadata metadata,
        DateTime timestamp,
        string? version,
        string? correlationId
        )
    {
        Id = id;
        Key = key;
        Type = type;
        Payload = payload;
        Metadata = metadata;
        Timestamp = timestamp;
        Version = version;
        CorrelationId = correlationId;
    }

    /// <summary>
    /// An incremental unique identifier for the event.
    /// </summary>
    public long Id { get; }
    
    /// <summary>
    /// Key used to group related events. It usually stores the aggregate ID.
    /// </summary>
    public string Key { get; }
    
    /// <summary>
    /// The event type.
    /// </summary>
    public string Type { get; }
    
    /// <summary>
    /// Event payload.
    /// </summary>
    public TPayload Payload { get; }
    
    /// <summary>
    /// Event metadata.
    /// </summary>
    public EventMetadata Metadata { get; }
    
    /// <summary>
    /// The date and time the event has occurred.
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// A version used to determine how to interpret the event payload and metadata.
    /// </summary>
    public string? Version { get; }
    
    /// <summary>
    /// Helps correlate workflows or processes that span multiple events.
    /// </summary>
    public string? CorrelationId { get; }
}