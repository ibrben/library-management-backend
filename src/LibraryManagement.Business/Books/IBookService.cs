namespace LibraryManagement.Business.Books;

public interface IBookService
{
    Task<BookResult> CreateAsync(SaveBookCommand command, CancellationToken cancellationToken);
    Task<BookResult> UpdateAsync(Guid id, SaveBookCommand command, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<BookResult> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<BookResult>> GetListAsync(BookQuery query, CancellationToken cancellationToken);
}
