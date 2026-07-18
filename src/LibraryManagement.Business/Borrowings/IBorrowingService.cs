namespace LibraryManagement.Business.Borrowings;

public interface IBorrowingService
{
    Task<BorrowTransactionResult> BorrowAsync(
        BorrowBookCommand command,
        CancellationToken cancellationToken);

    Task<BorrowTransactionResult> ReturnAsync(
        Guid transactionId,
        Guid actorId,
        bool canManageAll,
        CancellationToken cancellationToken);

    Task<BorrowTransactionResult> GetAsync(
        Guid transactionId,
        Guid actorId,
        bool canManageAll,
        CancellationToken cancellationToken);

    Task<TransactionPage> GetHistoryAsync(
        TransactionQuery query,
        CancellationToken cancellationToken);
}
