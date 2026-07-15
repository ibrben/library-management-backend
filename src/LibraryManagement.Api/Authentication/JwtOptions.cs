namespace LibraryManagement.Api.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Secret { get; init; }
    public string Issuer { get; init; } = "LibraryManagement.Api";
    public string Audience { get; init; } = "LibraryManagement.Client";
    public int ExpirationMinutes { get; init; } = 60;

    public void Validate()
    {
        if (Secret.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must contain at least 32 characters.");
        }

        if (ExpirationMinutes is < 1 or > 1440)
        {
            throw new InvalidOperationException("Jwt:ExpirationMinutes must be between 1 and 1440.");
        }
    }
}
