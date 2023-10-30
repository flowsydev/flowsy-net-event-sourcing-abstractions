namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Represents an exception thrown when event validation fails.
/// </summary>
/// <typeparam name="TEvent">
/// The type of event.
/// </typeparam>
public sealed class EventValidationException<TEvent> : Exception where TEvent : IEvent
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="validationResult">The results of the event validation.</param>
    public EventValidationException(EventValidationResult<TEvent> validationResult) 
        : base(validationResult.Message ?? string.Format(Resources.Strings.InvalidEvent, typeof(TEvent).Name))
    {
        ValidationResult = validationResult;
    }
    
    /// <summary>
    /// The results of the event validation.
    /// </summary>
    public EventValidationResult<TEvent> ValidationResult { get; }
}