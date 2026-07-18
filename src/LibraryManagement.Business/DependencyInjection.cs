using LibraryManagement.Business.Authentication;
using LibraryManagement.Business.Books;
using LibraryManagement.Business.Borrowings;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusiness(this IServiceCollection services) => services
        .AddScoped<IPasswordService, PasswordService>()
        .AddScoped<IAuthenticationService, AuthenticationService>()
        .AddScoped<IBookService, BookService>()
        .AddScoped<IBorrowingService, BorrowingService>();
}
