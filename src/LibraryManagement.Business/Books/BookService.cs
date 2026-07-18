using LibraryManagement.Business.Exceptions;
using LibraryManagement.DataAccess;
using LibraryManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Business.Books;

internal sealed class BookService(LibraryDbContext dbContext) : IBookService
{
    public async Task<BookResult> CreateAsync(SaveBookCommand command, CancellationToken cancellationToken)
    {
        var normalized = NormalizeAndValidate(command);
        if (await dbContext.Books.AnyAsync(book => book.Isbn == normalized.Isbn, cancellationToken))
        {
            throw new ConflictException($"A book with ISBN '{normalized.Isbn}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = normalized.Isbn,
            Title = normalized.Title,
            Author = normalized.Author,
            Publisher = normalized.Publisher,
            PublicationYear = normalized.PublicationYear,
            Category = normalized.Category,
            Shelf = normalized.Shelf,
            AvailabilityStatus = BookAvailabilityStatus.Available,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Books.Add(book);
        await SaveChangesAsync(cancellationToken);
        return Map(book);
    }

    public async Task<BookResult> UpdateAsync(
        Guid id,
        SaveBookCommand command,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeAndValidate(command);
        var book = await dbContext.Books.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Book '{id}' was not found.");
        if (await dbContext.Books.AnyAsync(
                candidate => candidate.Isbn == normalized.Isbn && candidate.Id != id,
                cancellationToken))
        {
            throw new ConflictException($"A book with ISBN '{normalized.Isbn}' already exists.");
        }

        book.Isbn = normalized.Isbn;
        book.Title = normalized.Title;
        book.Author = normalized.Author;
        book.Publisher = normalized.Publisher;
        book.PublicationYear = normalized.PublicationYear;
        book.Category = normalized.Category;
        book.Shelf = normalized.Shelf;
        book.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveChangesAsync(cancellationToken);
        return Map(book);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await dbContext.Books.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Book '{id}' was not found.");
        var hasActiveBorrow = book.AvailabilityStatus == BookAvailabilityStatus.Borrowed ||
            await dbContext.BorrowTransactions.AnyAsync(
                transaction => transaction.BookId == id && transaction.Status == BorrowStatus.Borrowed,
                cancellationToken);
        if (hasActiveBorrow)
        {
            throw new ConflictException("A borrowed book cannot be deleted.");
        }

        dbContext.Books.Remove(book);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<BookResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await dbContext.Books.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Book '{id}' was not found.");
        return Map(book);
    }

    public async Task<PagedResult<BookResult>> GetListAsync(
        BookQuery query,
        CancellationToken cancellationToken)
    {
        ValidateQuery(query);
        var books = dbContext.Books.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Isbn))
        {
            var isbn = query.Isbn.Trim();
            books = books.Where(book => book.Isbn == isbn);
        }

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            books = books.Where(book => EF.Functions.ILike(book.Title, $"%{query.Title.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            books = books.Where(book => EF.Functions.ILike(book.Author, $"%{query.Author.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            books = books.Where(book => book.Category != null &&
                EF.Functions.ILike(book.Category, $"%{query.Category.Trim()}%"));
        }

        if (query.Availability.HasValue)
        {
            books = books.Where(book => book.AvailabilityStatus == query.Availability.Value);
        }

        books = ApplySorting(books, query.SortBy.Trim().ToLowerInvariant(), query.SortOrder);
        var totalCount = await books.CountAsync(cancellationToken);
        var items = await books.Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(book => Map(book))
            .ToListAsync(cancellationToken);
        return new PagedResult<BookResult>(
            items,
            query.Page,
            query.PageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize));
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException("A book with the same unique value already exists.", exception);
        }
    }

    private static IQueryable<Book> ApplySorting(IQueryable<Book> books, string sortBy, string sortOrder)
    {
        var descending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy, descending) switch
        {
            ("isbn", false) => books.OrderBy(book => book.Isbn),
            ("isbn", true) => books.OrderByDescending(book => book.Isbn),
            ("author", false) => books.OrderBy(book => book.Author).ThenBy(book => book.Title),
            ("author", true) => books.OrderByDescending(book => book.Author).ThenBy(book => book.Title),
            ("category", false) => books.OrderBy(book => book.Category).ThenBy(book => book.Title),
            ("category", true) => books.OrderByDescending(book => book.Category).ThenBy(book => book.Title),
            ("createdat", false) => books.OrderBy(book => book.CreatedAt),
            ("createdat", true) => books.OrderByDescending(book => book.CreatedAt),
            (_, false) => books.OrderBy(book => book.Title),
            (_, true) => books.OrderByDescending(book => book.Title)
        };
    }

    private static SaveBookCommand NormalizeAndValidate(SaveBookCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateRequired(command.Isbn, nameof(command.Isbn), 20, errors);
        ValidateRequired(command.Title, nameof(command.Title), 300, errors);
        ValidateRequired(command.Author, nameof(command.Author), 200, errors);
        ValidateRequired(command.Shelf, nameof(command.Shelf), 100, errors);
        ValidateOptional(command.Publisher, nameof(command.Publisher), 200, errors);
        ValidateOptional(command.Category, nameof(command.Category), 100, errors);
        if (command.PublicationYear is < 1000 || command.PublicationYear > DateTime.UtcNow.Year)
        {
            errors[nameof(command.PublicationYear)] =
                [$"Publication year must be between 1000 and {DateTime.UtcNow.Year}."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Book validation failed.", errors);
        }

        return command with
        {
            Isbn = command.Isbn.Trim(),
            Title = command.Title.Trim(),
            Author = command.Author.Trim(),
            Publisher = NormalizeOptional(command.Publisher),
            Category = NormalizeOptional(command.Category),
            Shelf = command.Shelf.Trim()
        };
    }

    private static void ValidateQuery(BookQuery query)
    {
        var errors = new Dictionary<string, string[]>();
        if (query.Page < 1)
        {
            errors[nameof(query.Page)] = ["Page must be at least 1."];
        }

        if (query.PageSize is < 1 or > 100)
        {
            errors[nameof(query.PageSize)] = ["Page size must be between 1 and 100."];
        }

        string[] allowedSortFields = ["title", "isbn", "author", "category", "createdat"];
        if (!allowedSortFields.Contains(query.SortBy.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            errors[nameof(query.SortBy)] = ["Sort by must be title, isbn, author, category, or createdAt."];
        }

        if (!query.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase) &&
            !query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            errors[nameof(query.SortOrder)] = ["Sort order must be asc or desc."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Book query validation failed.", errors);
        }
    }

    private static void ValidateRequired(
        string value,
        string field,
        int maxLength,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"{field} is required."];
        }
        else if (value.Trim().Length > maxLength)
        {
            errors[field] = [$"{field} cannot exceed {maxLength} characters."];
        }
    }

    private static void ValidateOptional(
        string? value,
        string field,
        int maxLength,
        Dictionary<string, string[]> errors)
    {
        if (value?.Trim().Length > maxLength)
        {
            errors[field] = [$"{field} cannot exceed {maxLength} characters."];
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BookResult Map(Book book) => new(
        book.Id,
        book.Isbn,
        book.Title,
        book.Author,
        book.Publisher,
        book.PublicationYear,
        book.Category,
        book.Shelf,
        book.AvailabilityStatus,
        book.CreatedAt,
        book.UpdatedAt);
}
