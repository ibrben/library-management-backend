using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LibraryManagement.Business.Borrowings;
using LibraryManagement.Business.Exceptions;
using LibraryManagement.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/borrowings")]
public sealed class BorrowingsController(IBorrowingService borrowingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<BorrowTransactionResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BorrowTransactionResult>> Borrow(
        BorrowBookRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorId();
        var canManageAll = User.IsInRole(nameof(UserRole.Administrator)) ||
            User.IsInRole(nameof(UserRole.Librarian));
        if (!canManageAll && request.UserId.HasValue && request.UserId.Value != actorId)
        {
            throw new ForbiddenException("You may only borrow books for your own account.");
        }

        var borrowerId = canManageAll ? request.UserId ?? actorId : actorId;
        var transaction = await borrowingService.BorrowAsync(
            new BorrowBookCommand(request.BookId, borrowerId, request.DueDate), cancellationToken);
        return CreatedAtAction(nameof(GetMine), new { }, transaction);
    }

    [HttpPost("{transactionId:guid}/return")]
    [ProducesResponseType<BorrowTransactionResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BorrowTransactionResult>> Return(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var canManageAll = User.IsInRole(nameof(UserRole.Administrator)) ||
            User.IsInRole(nameof(UserRole.Librarian));
        return Ok(await borrowingService.ReturnAsync(
            transactionId, GetActorId(), canManageAll, cancellationToken));
    }

    [HttpGet("{transactionId:guid}")]
    [ProducesResponseType<BorrowTransactionResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BorrowTransactionResult>> Get(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var canManageAll = User.IsInRole(nameof(UserRole.Administrator)) ||
            User.IsInRole(nameof(UserRole.Librarian));
        return Ok(await borrowingService.GetAsync(
            transactionId, GetActorId(), canManageAll, cancellationToken));
    }

    [HttpGet("mine")]
    [ProducesResponseType<TransactionPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionPage>> GetMine(
        [FromQuery] TransactionHistoryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await borrowingService.GetHistoryAsync(
            request.ToQuery(GetActorId()), cancellationToken));

    [Authorize(Policy = "InventoryManagement")]
    [HttpGet]
    [ProducesResponseType<TransactionPage>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionPage>> GetGlobal(
        [FromQuery] GlobalTransactionHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var query = request.ToQuery();
        var result = User.IsInRole(nameof(UserRole.Administrator))
            ? await borrowingService.GetHistoryAsync(query, cancellationToken)
            : await borrowingService.GetEndUserHistoryAsync(query, cancellationToken);
        return Ok(result);
    }

    private Guid GetActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedException("The access token has an invalid user identifier.");
    }
}

public sealed record BorrowBookRequest(
    [Required] Guid BookId,
    Guid? UserId = null,
    DateTimeOffset? DueDate = null);

public class TransactionHistoryRequest
{
    public BorrowStatus? Status { get; init; }
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;

    internal TransactionQuery ToQuery(Guid userId) => new(userId, Status, Page, PageSize);
}

public sealed class GlobalTransactionHistoryRequest : TransactionHistoryRequest
{
    public Guid? UserId { get; init; }

    internal TransactionQuery ToQuery() => new(UserId, Status, Page, PageSize);
}
