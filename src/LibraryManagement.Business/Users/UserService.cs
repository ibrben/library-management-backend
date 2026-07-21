using LibraryManagement.DataAccess;
using LibraryManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Business.Users;

internal sealed class UserService(LibraryDbContext dbContext) : IUserService
{
    public async Task<IReadOnlyList<EndUserListItem>> GetEndUsersAsync(
        CancellationToken cancellationToken) =>
        await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRole.EndUser)
            .OrderBy(user => user.Username)
            .Select(user => new EndUserListItem(user.Id, user.Username))
            .ToListAsync(cancellationToken);
}
