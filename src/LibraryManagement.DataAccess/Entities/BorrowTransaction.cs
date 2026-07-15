namespace LibraryManagement.DataAccess.Entities;

public enum BorrowStatus
{
    Borrowed,
    Returned
}

public sealed class BorrowTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid BookId { get; set; }
    public DateTimeOffset BorrowDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset? ReturnDate { get; set; }
    public BorrowStatus Status { get; set; }
    public required User User { get; set; }
    public required Book Book { get; set; }
}
