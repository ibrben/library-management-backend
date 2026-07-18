using LibraryManagement.DataAccess.Entities;

namespace LibraryManagement.Business.Borrowings;

public sealed record BorrowBookCommand(Guid BookId, Guid BorrowerId, DateTimeOffset? DueDate);

public sealed record TransactionQuery(
    Guid? UserId,
    BorrowStatus? Status,
    int Page = 1,
    int PageSize = 20);

public sealed record BorrowTransactionResult(
    Guid Id,
    Guid UserId,
    string Username,
    Guid BookId,
    string Isbn,
    string BookTitle,
    DateTimeOffset BorrowDate,
    DateTimeOffset? DueDate,
    DateTimeOffset? ReturnDate,
    BorrowStatus Status);

public sealed record TransactionPage(
    IReadOnlyList<BorrowTransactionResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
