using LibraryManagement.Business.Authentication;
using LibraryManagement.DataAccess;
using LibraryManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Authentication;

internal sealed partial class DevelopmentDataSeeder(
    LibraryDbContext dbContext,
    IPasswordService passwordService,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<DevelopmentDataSeeder> logger)
{
    private static readonly SampleBook[] Books =
    [
        new("9780132350884", "Clean Code", "Robert C. Martin", "Prentice Hall", 2008, "Software Engineering", "TECH-A01"),
        new("9780201616224", "The Pragmatic Programmer", "Andrew Hunt and David Thomas", "Addison-Wesley", 1999, "Software Engineering", "TECH-A02"),
        new("9780134494166", "Clean Architecture", "Robert C. Martin", "Prentice Hall", 2017, "Software Architecture", "TECH-A03"),
        new("9781491950357", "Designing Data-Intensive Applications", "Martin Kleppmann", "O'Reilly Media", 2017, "Databases", "TECH-B01"),
        new("9780131103627", "The C Programming Language", "Brian W. Kernighan and Dennis M. Ritchie", "Prentice Hall", 1988, "Programming", "TECH-B02"),
        new("9780061120084", "To Kill a Mockingbird", "Harper Lee", "Harper Perennial", 2006, "Fiction", "FIC-A01"),
        new("9780451524935", "1984", "George Orwell", "Signet Classics", 1950, "Fiction", "FIC-A02"),
        new("9780743273565", "The Great Gatsby", "F. Scott Fitzgerald", "Scribner", 2004, "Fiction", "FIC-A03"),
        new("9780062316097", "Sapiens", "Yuval Noah Harari", "Harper", 2015, "History", "HIS-A01"),
        new("9780140449136", "The Odyssey", "Homer", "Penguin Classics", 2003, "Classics", "CLS-A01")
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var section = configuration.GetSection("SeedData");
        if (!section.GetValue("Enabled", false))
        {
            LogSeedDataDisabled(logger);
            return;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "SeedData:Enabled may only be true when ASPNETCORE_ENVIRONMENT is Development.");
        }

        var usersCreated = 0;
        usersCreated += await AddUserIfMissingAsync(section.GetSection("Librarian"), UserRole.Librarian, cancellationToken);
        usersCreated += await AddUserIfMissingAsync(section.GetSection("EndUser"), UserRole.EndUser, cancellationToken);

        var sampleIsbns = Books.Select(sample => sample.Isbn).ToArray();
        var existingIsbnList = await dbContext.Books
            .Where(book => sampleIsbns.Contains(book.Isbn))
            .Select(book => book.Isbn)
            .ToListAsync(cancellationToken);
        var existingIsbns = existingIsbnList.ToHashSet(StringComparer.Ordinal);
        var now = DateTimeOffset.UtcNow;
        var booksToAdd = Books.Where(book => !existingIsbns.Contains(book.Isbn))
            .Select(book => new Book
            {
                Id = Guid.NewGuid(),
                Isbn = book.Isbn,
                Title = book.Title,
                Author = book.Author,
                Publisher = book.Publisher,
                PublicationYear = book.PublicationYear,
                Category = book.Category,
                Shelf = book.Shelf,
                AvailabilityStatus = BookAvailabilityStatus.Available,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToArray();
        dbContext.Books.AddRange(booksToAdd);
        await dbContext.SaveChangesAsync(cancellationToken);
        LogSeedDataCompleted(logger, usersCreated, booksToAdd.Length);
    }

    private async Task<int> AddUserIfMissingAsync(
        IConfigurationSection section,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var username = Required(section, "Username").ToLowerInvariant();
        var email = Required(section, "Email").ToLowerInvariant();
        var password = Required(section, "Password", trim: false);
        ValidatePassword(password, $"SeedData:{section.Key}:Password");
        var existingUser = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Username == username || user.Email == email,
            cancellationToken);
        if (existingUser is not null)
        {
            if (existingUser.Username != username || existingUser.Email != email || existingUser.Role != role)
            {
                throw new InvalidOperationException(
                    $"SeedData:{section.Key} conflicts with existing user '{existingUser.Username}'.");
            }

            return 0;
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordService.Hash(password),
            FirstName = Required(section, "FirstName"),
            LastName = Required(section, "LastName"),
            Role = role,
            CreatedAt = now,
            UpdatedAt = now
        });
        return 1;
    }

    private static string Required(IConfigurationSection section, string key, bool trim = true)
    {
        var value = section[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"SeedData:{section.Key}:{key} is required when sample seeding is enabled.");
        }

        return trim ? value.Trim() : value;
    }

    private static void ValidatePassword(string password, string key)
    {
        if (password.Length < 12 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
        {
            throw new InvalidOperationException(
                $"{key} must be at least 12 characters and contain uppercase, lowercase, and a digit.");
        }
    }

    [LoggerMessage(LogLevel.Debug, "Development sample-data seeding is disabled.")]
    private static partial void LogSeedDataDisabled(ILogger logger);

    [LoggerMessage(LogLevel.Information,
        "Development sample-data seeding completed: {UsersCreated} users and {BooksCreated} books created.")]
    private static partial void LogSeedDataCompleted(ILogger logger, int usersCreated, int booksCreated);

    private sealed record SampleBook(
        string Isbn,
        string Title,
        string Author,
        string Publisher,
        int PublicationYear,
        string Category,
        string Shelf);
}
