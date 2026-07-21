namespace LibraryManagement.Business.Users;

public interface IUserService
{
    Task<IReadOnlyList<EndUserListItem>> GetEndUsersAsync(CancellationToken cancellationToken);
}
