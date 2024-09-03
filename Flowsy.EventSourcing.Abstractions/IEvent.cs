namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Represents an event that occurred in the system.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// The instant when the event occurred.
    /// </summary>
    DateTimeOffset OcurrenceInstant { get; }
}