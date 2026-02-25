namespace Core.Auth.Configuration;

public class CoreAuthOptions
{
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(24);
    public string CookieName { get; set; } = ".CoreSystem.Session";
    public int RateLimitMaxAttempts { get; set; } = 5;
    public int RateLimitLockoutMinutes { get; set; } = 15;
    public int BcryptWorkFactor { get; set; } = 12;
    public int MinPasswordLength { get; set; } = 4;
    public int MinFullNameLength { get; set; } = 2;

    /// <summary>
    /// Seed these roles on startup if no roles exist.
    /// Key = role name, Value = list of permission keys.
    /// </summary>
    public Dictionary<string, List<string>> DefaultRoles { get; set; } = new();
}
