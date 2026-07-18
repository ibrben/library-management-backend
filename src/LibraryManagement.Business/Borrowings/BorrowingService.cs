using LibraryManagement.Business.Exceptions;
using LibraryManagement.DataAccess;
using LibraryManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Business.Borrowings;

internal sealed partial class BorrowingService(
    LibraryDbContext dbContext,
    ILogger<BorrowingService> logger) : IBorrowingService
{
    public async Task<BorrowTransactionResult> BorrowAsync(
        BorrowBookCommand command,
        CancellationToken cancellationToken)
    {
        if (command.BookId == Guid.Empty || command.BorrowerId == Guid.Empty)
        {
            throw new ValidationException("Borrowing validation failed.", new Dictionary<string, string[]>
            {
                [command.BookId == Guid.Empty ? nameof(command.BookId) : nameof(command.BorrowerId)] =
                    ["A valid identifier is required."]
            });
        }

        var now = DateTimeOffset.UtcNow;
        if (command.DueDate.HasValue && command.DueDate.Value <= now)
        {
            throw new ValidationException("Borrowing validation failed.", new Dictionary<string, string[]>
            {
                [nameof(command.DueDate)] = ["Due date must be in the future."]
            });
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == command.BorrowerId, cancellationToken)
            ?? throw new NotFoundException($"User '{command.BorrowerId}' was not found.");
        var book = await dbContext.Books.SingleOrDefaultAsync(
            candidate => candidate.Id == command.BookId, cancellationToken)
            ?? throw new NotFoundException($"Book '{command.BookId}' was not found.");

        await using var databaseTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var updated = await dbContext.Books
            .Where(candidate => candidate.Id == command.BookId &&
                candidate.AvailabilityStatus == BookAvailabilityStatus.Available)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(candidate => candidate.AvailabilityStatus, BookAvailabilityStatus.Borrowed)
                .SetProperty(candidate => candidate.UpdatedAt, now), cancellationToken);
        if (updated == 0)
        {
            throw new ConflictException("The book is not available for borrowing.");
        }

        book.AvailabilityStatus = BookAvailabilityStatus.Borrowed;
        book.UpdatedAt = now;
        var transaction = new BorrowTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BookId = book.Id,
            BorrowDate = now,
            DueDate = command.DueDate,
            ReturnDate = null,
            Status = BorrowStatus.Borrowed,
            User = user,
            Book = book
        };
        dbContext.BorrowTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);
        LogBookBorrowed(logger, transaction.Id, book.Id, user.Id);
        return Map(transaction);
    }

    public async Task<BorrowTransactionResult> ReturnAsync(
        Guid transactionId,
        Guid actorId,
        bool canManageAll,
        CancellationToken cancellationToken)
    {
        if (transactionId == Guid.Empty)
        {
            throw new ValidationException("Return validation failed.", new Dictionary<string, string[]>
            {
                [nameof(transactionId)] = ["A valid transaction identifier is required."]
            });
        }

        var transaction = await dbContext.BorrowTransactions
            .Include(candidate => candidate.User)
            .Include(candidate => candidate.Book)
            .SingleOrDefaultAsync(candidate => candidate.Id == transactionId, cancellationToken)
            ?? throw new NotFoundException($"Borrow transaction '{transactionId}' was not found.");
        if (!canManageAll && transaction.UserId != actorId)
        {
            throw new ForbiddenException("You may only return books borrowed by your own account.");
        }

        var now = DateTimeOffset.UtcNow;
        await using var databaseTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var updated = await dbContext.BorrowTransactions
            .Where(candidate => candidate.Id == transactionId && candidate.Status == BorrowStatus.Borrowed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(candidate => candidate.Status, BorrowStatus.Returned)
                .SetProperty(candidate => candidate.ReturnDate, now), cancellationToken);
        if (updated == 0)
        {
            throw new ConflictException("This transaction has already been returned.");
        }

        transaction.Status = BorrowStatus.Returned;
        transaction.ReturnDate = now;
        transaction.Book.AvailabilityStatus = BookAvailabilityStatus.Available;
        transaction.Book.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);
        LogBookReturned(logger, transaction.Id, transaction.BookId, transaction.UserId, actorId);
        return Map(transaction);
    }

    public async Task<TransactionPage> GetHistoryAsync(
        TransactionQuery query,
        CancellationToken cancellationToken)
    {
        ValidateQuery(query);
        var transactions = dbContext.BorrowTransactions.AsNoTracking()
            .Include(transaction => transaction.User)
            .Include(transaction => transaction.Book)
            .AsQueryable();
        if (query.UserId.HasValue)
        {
            transactions = transactions.Where(transaction => transaction.UserId == query.UserId.Value);
        }

        if (query.Status.HasValue)
        {
            transactions = transactions.Where(transaction => transaction.Status == query.Status.Value);
        }

        var totalCount = await transactions.CountAsync(cancellationToken);
        var items = await transactions.OrderByDescending(transaction => transaction.BorrowDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(transaction => Map(transaction))
            .ToListAsync(cancellationToken);
        return new TransactionPage(items, query.Page, query.PageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize));
    }

    public async Task<BorrowTransactionResult> GetAsync(
        Guid transactionId,
        Guid actorId,
        bool canManageAll,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.BorrowTransactions.AsNoTracking()
            .Include(candidate => candidate.User)
            .Include(candidate => candidate.Book)
            .SingleOrDefaultAsync(candidate => candidate.Id == transactionId, cancellationToken)
            ?? throw new NotFoundException($"Borrow transaction '{transactionId}' was not found.");
        if (!canManageAll && transaction.UserId != actorId)
        {
            throw new ForbiddenException("You may only view transactions for your own account.");
        }

        return Map(transaction);
    }

    private static void ValidateQuery(TransactionQuery query)
    {
        var errors = new Dictionary<string, string[]>();
        if (query.Page < 1) errors[nameof(query.Page)] = ["Page must be at least 1."];
        if (query.PageSize is < 1 or > 100)
            errors[nameof(query.PageSize)] = ["Page size must be between 1 and 100."];
        if (errors.Count > 0) throw new ValidationException("Transaction query validation failed.", errors);
    }

    private static BorrowTransactionResult Map(BorrowTransaction transaction) => new(
        transaction.Id,
        transaction.UserId,
        transaction.User.Username,
        transaction.BookId,
        transaction.Book.Isbn,
        transaction.Book.Title,
        transaction.BorrowDate,
        transaction.DueDate,
        transaction.ReturnDate,
        transaction.Status);

    [LoggerMessage(LogLevel.Information,
        "Borrow transaction {TransactionId}: book {BookId} assigned to user {UserId}.")]
    private static partial void LogBookBorrowed(ILogger logger, Guid transactionId, Guid bookId, Guid userId);

    [LoggerMessage(LogLevel.Information,
        "Return transaction {TransactionId}: book {BookId} returned for user {UserId} by actor {ActorId}.")]
    private static partial void LogBookReturned(
        ILogger logger, Guid transactionId, Guid bookId, Guid userId, Guid actorId);
}
