namespace LibraryManagement.DataAccess.Entities;

public enum BookAvailabilityStatus
{
    Available,
    Borrowed
}

public sealed class Book
{
    public Guid Id { get; set; }
    public required string Isbn { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string? Category { get; set; }
    public required string Shelf { get; set; }
    public BookAvailabilityStatus AvailabilityStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<BorrowTransaction> BorrowTransactions { get; } = [];
}
