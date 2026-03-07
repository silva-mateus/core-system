namespace Core.Common.Exceptions;

/// <summary>
/// Thrown when the user does not have permission for the requested action. Maps to HTTP 403 Forbidden.
/// </summary>
public class ForbiddenException : Exception
{
    public string ErrorCode { get; }

    public ForbiddenException(string message, string errorCode = "FORBIDDEN")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
