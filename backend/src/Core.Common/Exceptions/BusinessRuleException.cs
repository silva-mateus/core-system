namespace Core.Common.Exceptions;

/// <summary>
/// Thrown when a business rule is violated. Maps to HTTP 400 Bad Request.
/// </summary>
public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }

    public BusinessRuleException(string message, string errorCode = "BUSINESS_RULE_VIOLATION")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
