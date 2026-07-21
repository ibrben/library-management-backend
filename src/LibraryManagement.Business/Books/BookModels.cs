using LibraryManagement.DataAccess.Entities;

namespace LibraryManagement.Business.Books;

public sealed record SaveBookCommand(
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    int? PublicationYear,
    string? Category,
    string Shelf);

public sealed record BookQuery(
    string? Isbn,
    string? Title,
    string? Author,
    string? Category,
    BookAvailabilityStatus? Availability,
    int Page = 1,
    int PageSize = 20,
    string SortBy = "title",
    string SortOrder = "asc");

public sealed record BookResult(
    Guid Id,
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    int? PublicationYear,
    string? Category,
    string Shelf,
    BookAvailabilityStatus AvailabilityStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
