using System.ComponentModel.DataAnnotations;
using LibraryManagement.Business.Books;
using LibraryManagement.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController(IBookService bookService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BookResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResult>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await bookService.GetAsync(id, cancellationToken));

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<PagedResult<BookResult>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BookResult>>> GetList(
        [FromQuery] BookSearchRequest request,
        CancellationToken cancellationToken) =>
        Ok(await bookService.GetListAsync(request.ToQuery(), cancellationToken));

    [AllowAnonymous]
    [HttpGet("search")]
    [ProducesResponseType<PagedResult<BookResult>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BookResult>>> Search(
        [FromQuery] BookSearchRequest request,
        CancellationToken cancellationToken) =>
        Ok(await bookService.GetListAsync(request.ToQuery(), cancellationToken));

    [Authorize(Policy = "InventoryManagement")]
    [HttpPost]
    [ProducesResponseType<BookResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookResult>> Create(
        SaveBookRequest request,
        CancellationToken cancellationToken)
    {
        var book = await bookService.CreateAsync(request.ToCommand(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = book.Id }, book);
    }

    [Authorize(Policy = "InventoryManagement")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<BookResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookResult>> Update(
        Guid id,
        SaveBookRequest request,
        CancellationToken cancellationToken) =>
        Ok(await bookService.UpdateAsync(id, request.ToCommand(), cancellationToken));

    [Authorize(Policy = "InventoryManagement")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await bookService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

public sealed class SaveBookRequest
{
    [Required, MaxLength(20)]
    public string Isbn { get; init; } = string.Empty;

    [Required, MaxLength(300)]
    public string Title { get; init; } = string.Empty;

    [Required, MaxLength(200)]
    public string Author { get; init; } = string.Empty;

    [MaxLength(200)]
    public string? Publisher { get; init; }

    public int? PublicationYear { get; init; }

    [MaxLength(100)]
    public string? Category { get; init; }

    [Required, MaxLength(100)]
    public string Shelf { get; init; } = string.Empty;

    internal SaveBookCommand ToCommand() => new(
        Isbn,
        Title,
        Author,
        Publisher,
        PublicationYear,
        Category,
        Shelf);
}

public sealed class BookSearchRequest
{
    public string? Isbn { get; init; }
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Category { get; init; }
    public BookAvailabilityStatus? Availability { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "title";
    public string SortOrder { get; init; } = "asc";

    internal BookQuery ToQuery() => new(
        Isbn,
        Title,
        Author,
        Category,
        Availability,
        Page,
        PageSize,
        SortBy,
        SortOrder);
}
