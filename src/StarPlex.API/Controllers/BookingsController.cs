using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Features.Bookings.Commands.CancelCashierBooking;
using StarPlex.Application.Features.Bookings.Commands.CancelTicket;
using StarPlex.Application.Features.Bookings.Commands.ConfirmBooking;
using StarPlex.Application.Features.Bookings.Commands.CreateBooking;
using StarPlex.Application.Features.Bookings.Commands.CreateCashierSale;
using StarPlex.Application.Features.Bookings.Commands.LockSeats;
using StarPlex.Application.Features.Bookings.Commands.ScanTicket;
using StarPlex.Application.Features.Bookings.Commands.UnlockSeats;
using StarPlex.Application.Features.Bookings.Queries.GetCashierRecentSales;
using StarPlex.Application.Features.Bookings.Queries.GetUserBookings;
using StarPlex.Application.Features.Sessions.DTOs;
using StarPlex.Application.Features.Sessions.Queries.GetSessionSeatMap;
using StarPlex.Application.Features.Bookings.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace StarPlex.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITicketService _ticketService;
    private readonly IApplicationDbContext _context;

    public BookingsController(IMediator mediator, ITicketService ticketService, IApplicationDbContext context)
    {
        _mediator = mediator;
        _ticketService = ticketService;
        _context = context;
    }

    [HttpGet("session/{sessionId}/seats")]
    [AllowAnonymous]
    public async Task<ActionResult<List<SeatMapDto>>> GetSeatMap(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSessionSeatMapQuery { SessionId = sessionId }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("lock")]
    [Authorize]
    public async Task<ActionResult<LockSeatsResponseDto>> LockSeats([FromBody] LockSeatsCommand command, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "User ID could not be determined." });

        command.UserId = userId.Value;
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("unlock")]
    [Authorize]
    public async Task<ActionResult<UnlockSeatsResponseDto>> UnlockSeats([FromBody] UnlockSeatsCommand command, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "User ID could not be determined." });

        command.UserId = userId.Value;
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpGet("my-bookings")]
    [Authorize]
    public async Task<ActionResult<List<UserBookingDto>>> GetMyBookings(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _mediator.Send(new GetUserBookingsQuery { UserId = userId.Value }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking([FromBody] CreateBookingCommand command, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "User ID could not be determined." });

        command.UserId = userId.Value;
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("confirm")]
    [Authorize]
    public async Task<ActionResult<bool>> ConfirmBooking([FromBody] ConfirmBookingCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
            return BadRequest(new { Message = "Failed to confirm booking or booking not found." });

        return Ok(new { IsSuccess = result, Message = "Booking successfully confirmed." });
    }

    [HttpPost("tickets/{ticketId}/cancel")]
    [Authorize]
    public async Task<ActionResult<bool>> CancelTicket(Guid ticketId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { Message = "User ID could not be determined." });

        var result = await _mediator.Send(new CancelTicketCommand
        {
            TicketId = ticketId,
            UserId = userId.Value
        }, cancellationToken);

        if (!result)
            return BadRequest(new { Message = "Не вдалося скасувати квиток. Можливо, до сеансу залишилось менше 30 хвилин або виникла помилка банку." });

        return Ok(new { IsSuccess = result, Message = "Квиток успішно скасовано, гроші повернено на картку." });
    }

    [HttpGet("tickets/{id}/download")]
    [Authorize]
    public async Task<IActionResult> DownloadTicket(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var ticketOwnerId = await _context.Tickets
                .Where(t => t.Id == id)
                .Select(t => t.BookingSeat.Booking.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (ticketOwnerId == Guid.Empty) return NotFound(new { Message = "Ticket not found." });
            if (ticketOwnerId != currentUserId.Value) return Forbid();

            var pdfBytes = await _ticketService.GenerateTicketPdfAsync(id, cancellationToken);
            string fileName = $"Ticket-{id.ToString()[..8].ToUpper()}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpGet("{bookingId}/tickets/download-all")]
    [Authorize]
    public async Task<IActionResult> DownloadAllTickets(Guid bookingId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.BookingSeats)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null) return NotFound(new { Message = "Booking not found." });

            bool isOwner = booking.UserId == currentUserId.Value;
            bool isStaff = User.IsInRole("Cashier") || User.IsInRole("CinemaManager") || User.IsInRole("SuperAdmin");

            if (!isOwner && !isStaff)
                return Forbid();

            var bookingSeatIds = booking.BookingSeats.Select(bs => bs.Id).ToList();
            var ticketIds = await _context.Tickets
                .Where(t => bookingSeatIds.Contains(t.BookingSeatId))
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            if (!ticketIds.Any()) return NotFound(new { Message = "No tickets found for this booking." });

            var pdfBytes = await _ticketService.GenerateTicketsPdfAsync(ticketIds, cancellationToken);

            string mergedFileName = $"StarPlex-Tickets-{bookingId.ToString()[..8].ToUpper()}.pdf";
            return File(pdfBytes, "application/pdf", mergedFileName);
    }

    [HttpPost("cashier-sell")]
    [Authorize(Roles = "SuperAdmin,CinemaManager,Cashier")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Guid>> CreateCashierSale([FromBody] CreateCashierSaleCommand command, CancellationToken cancellationToken)
    {
        var bookingId = await _mediator.Send(command, cancellationToken);
        return Ok(bookingId);
    }

    [HttpGet("cashier-sales")]
    [Authorize(Roles = "SuperAdmin,CinemaManager,Cashier")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CashierSaleDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<CashierSaleDto>>> GetCashierSales(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCashierRecentSalesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("cashier/{bookingId}/cancel")]
    [Authorize(Roles = "SuperAdmin,CinemaManager,Cashier")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<bool>> CancelCashierBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelCashierBookingCommand { BookingId = bookingId }, cancellationToken);

        if (!result)
            return BadRequest(new { Message = "Failed to cancel the cashier booking or booking not found." });

        return Ok(result);
    }

    [HttpPost("scan-ticket")]
    [Authorize(Roles = "SuperAdmin,CinemaManager,Cashier")]
    public async Task<ActionResult<ScanTicketResultDto>> ScanTicket(
        [FromBody] ScanTicketCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(claim, out var id) ? id : null;
    }
}