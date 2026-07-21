using LibraryManagement.Business.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [Authorize(Policy = "InventoryManagement")]
    [HttpGet("end-users")]
    [ProducesResponseType<IReadOnlyList<EndUserListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<EndUserListItem>>> GetEndUsers(
        CancellationToken cancellationToken) =>
        Ok(await userService.GetEndUsersAsync(cancellationToken));
}
