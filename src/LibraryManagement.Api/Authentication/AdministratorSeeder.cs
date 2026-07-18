using LibraryManagement.Business.Authentication;
using LibraryManagement.DataAccess;
using LibraryManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Authentication;

internal sealed partial class AdministratorSeeder(
    LibraryDbContext dbContext,
    IPasswordService passwordService,
    IConfiguration configuration,
    ILogger<AdministratorSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var section = configuration.GetSection("BootstrapAdmin");
        if (!section.GetValue("Enabled", false))
        {
            LogBootstrapAdminDisabled(logger);
            return;
        }

        var username = section["Username"]?.Trim().ToLowerInvariant();
        var email = section["Email"]?.Trim().ToLowerInvariant();
        var password = section["Password"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "BootstrapAdmin username, email, and password are required when bootstrap creation is enabled.");
        }

        if (password.Length < 12 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:Password must be at least 12 characters and contain uppercase, lowercase, and a digit.");
        }

        if (await dbContext.Users.AnyAsync(user => user.Role == UserRole.Administrator, cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordService.Hash(password),
            FirstName = section["FirstName"]?.Trim() ?? "System",
            LastName = section["LastName"]?.Trim() ?? "Administrator",
            Role = UserRole.Administrator,
            CreatedAt = now,
            UpdatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        LogBootstrapAdminCreated(logger, username);
    }

    [LoggerMessage(LogLevel.Debug, "Bootstrap administrator creation is disabled.")]
    private static partial void LogBootstrapAdminDisabled(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Created bootstrap administrator {Username}.")]
    private static partial void LogBootstrapAdminCreated(ILogger logger, string username);
}
