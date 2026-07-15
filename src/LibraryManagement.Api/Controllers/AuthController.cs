using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LibraryManagement.Business.Authentication;
using LibraryManagement.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthenticationResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationResult>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await authenticationService.LoginAsync(
            new LoginCommand(request.UsernameOrEmail, request.Password), cancellationToken));
    }

    [Authorize(Roles = nameof(UserRole.Administrator))]
    [HttpPost("register")]
    [ProducesResponseType<AuthenticatedUser>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthenticatedUser>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authenticationService.RegisterAsync(
            new RegisterCommand(
                request.Username,
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.Role),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> Me() => Ok(new
    {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        username = User.Identity?.Name,
        email = User.FindFirstValue(ClaimTypes.Email),
        role = User.FindFirstValue(ClaimTypes.Role)
    });
}

public sealed record LoginRequest(
    [Required] string UsernameOrEmail,
    [Required] string Password);

public sealed record RegisterRequest(
    [Required, MinLength(3), MaxLength(100)] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(12)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    UserRole Role = UserRole.EndUser);
