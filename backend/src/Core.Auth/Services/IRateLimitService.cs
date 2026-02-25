namespace Core.Auth.Services;

public interface IRateLimitService
{
    bool IsRateLimited(string key);
    void RecordAttempt(string key);
    void ResetAttempts(string key);
    string BuildLoginKey(string clientIp, string username);
}
