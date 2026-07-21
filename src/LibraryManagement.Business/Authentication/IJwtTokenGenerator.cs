using LibraryManagement.DataAccess.Entities;

namespace LibraryManagement.Business.Authentication;

public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAt) Generate(User user);
}
