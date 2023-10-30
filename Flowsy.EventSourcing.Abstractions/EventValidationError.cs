namespace Flowsy.EventSourcing.Abstractions;

public class EventValidationError
{
    public EventValidationError(string message, string? code = null)
    {
        Message = message;
        Code = code;
    }

    public string Message { get; }
    public string? Code { get; }
}