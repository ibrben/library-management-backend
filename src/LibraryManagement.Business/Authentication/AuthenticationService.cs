using System.Net.Mail;
using LibraryManagement.Business.Exceptions;
using LibraryManagement.DataAccess;
using LibraryManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Business.Authentication;

internal sealed class AuthenticationService(
    LibraryDbContext dbContext,
    IPasswordService passwordService,
    IJwtTokenGenerator jwtTokenGenerator) : IAuthenticationService
{
    public async Task<AuthenticationResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var identifier = command.UsernameOrEmail.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Username == identifier || candidate.Email == identifier,
            cancellationToken);

        if (user is null || !passwordService.Verify(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid username, email, or password.");
        }

        var (token, expiresAt) = jwtTokenGenerator.Generate(user);
        return new AuthenticationResult(token, expiresAt, Map(user));
    }

    public async Task<AuthenticatedUser> RegisterAsync(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);

        var username = command.Username.Trim().ToLowerInvariant();
        var email = command.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(
                user => user.Username == username || user.Email == email,
                cancellationToken))
        {
            throw new ConflictException("A user with that username or email already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordService.Hash(command.Password),
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            Role = command.Role,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    private static AuthenticatedUser Map(User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Role);

    private static void Validate(RegisterCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (command.Username.Trim().Length is < 3 or > 100)
        {
            errors[nameof(command.Username)] = ["Username must be between 3 and 100 characters."];
        }

        try
        {
            _ = new MailAddress(command.Email.Trim());
        }
        catch (FormatException)
        {
            errors[nameof(command.Email)] = ["Email must be valid."];
        }

        if (command.Password.Length < 12 ||
            !command.Password.Any(char.IsUpper) ||
            !command.Password.Any(char.IsLower) ||
            !command.Password.Any(char.IsDigit))
        {
            errors[nameof(command.Password)] =
                ["Password must be at least 12 characters and contain uppercase, lowercase, and a digit."];
        }

        if (string.IsNullOrWhiteSpace(command.FirstName))
        {
            errors[nameof(command.FirstName)] = ["First name is required."];
        }

        if (string.IsNullOrWhiteSpace(command.LastName))
        {
            errors[nameof(command.LastName)] = ["Last name is required."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Registration validation failed.", errors);
        }
    }
}
