namespace Core.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource is not found. Maps to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(string message, string errorCode = "NOT_FOUND")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
