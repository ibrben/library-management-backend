namespace LibraryManagement.Business.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken);
    Task<AuthenticatedUser> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken);
}
