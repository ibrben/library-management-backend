using LibraryManagement.DataAccess.Entities;

namespace LibraryManagement.Business.Authentication;

public sealed record LoginCommand(string UsernameOrEmail, string Password);

public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role);

public sealed record AuthenticatedUser(
    Guid Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role);

public sealed record AuthenticationResult(string AccessToken, DateTimeOffset ExpiresAt, AuthenticatedUser User);
