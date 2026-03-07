namespace Core.Common.Exceptions;

/// <summary>
/// Thrown when a resource conflict is detected (e.g., duplicate, scheduling overlap). Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : Exception
{
    public string ErrorCode { get; }

    public ConflictException(string message, string errorCode = "CONFLICT")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
