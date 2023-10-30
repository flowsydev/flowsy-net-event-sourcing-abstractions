namespace Flowsy.EventSourcing.Abstractions;

/// <summary>
/// Contains the results of an event validation.
/// </summary>
/// <typeparam name="TEvent">The type of the target event.</typeparam>
public class EventValidationResult<TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Creates an instance of a successful result.
    /// </summary>
    /// <param name="event">The event being validated.</param>
    public EventValidationResult(TEvent @event)
        : this(@event, null, null)
    {
        IsSuccessful = true;
    }

    /// <summary>
    /// Creates an instance of a validation result.
    /// </summary>
    /// <param name="event">The event being validated.</param>
    /// <param name="message">A short message summarizing the result of the validation.</param>
    /// <param name="errors">The list of validation errors. If the list is null or empty, the result is considered successful.</param>
    public EventValidationResult(TEvent @event, string? message, IEnumerable<EventValidationError>? errors = null)
    {
        Event = @event;
        IsSuccessful = !(errors?.Any() ?? false);
        Message = message;
        Errors = errors ?? Array.Empty<EventValidationError>();
    }
    
    /// <summary>
    /// The event being validated.
    /// </summary>
    public TEvent Event { get; }
    public bool IsSuccessful { get; }
    public string? Message { get; }
    public IEnumerable<EventValidationError> Errors { get; }
}