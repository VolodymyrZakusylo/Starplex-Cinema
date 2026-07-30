using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Domain.Enums;
using StarPlex.Domain.Entities;

namespace StarPlex.Infrastructure.Services;

public class TicketService : ITicketService
{
    private readonly IApplicationDbContext _context;
    private readonly IQrCodeService _qrCodeService;

    private static readonly string AccentColor = "#E5B842";
    private static readonly string DarkBg = "#1A1A2E";
    private static readonly string TextMuted = "#9999BB";

    public TicketService(IApplicationDbContext context, IQrCodeService qrCodeService)
    {
        _context = context;
        _qrCodeService = qrCodeService;
    }

    public async Task<byte[]> GenerateTicketPdfAsync(Guid ticketId, CancellationToken ct = default)
    {
        return await GenerateTicketsPdfAsync(new List<Guid> { ticketId }, ct);
    }

    public async Task<byte[]> GenerateTicketsPdfAsync(List<Guid> ticketIds, CancellationToken ct = default)
    {
        var tickets = await _context.Tickets
            .Include(t => t.BookingSeat).ThenInclude(bs => bs.Seat)
            .Include(t => t.BookingSeat).ThenInclude(bs => bs.Booking)
                .ThenInclude(b => b.Session).ThenInclude(s => s.Movie)
            .Include(t => t.BookingSeat.Booking.Session.Hall)
            .Where(t => ticketIds.Contains(t.Id))
            .ToListAsync(ct);

        if (!tickets.Any()) throw new NotFoundException("Tickets not found", ticketIds.FirstOrDefault());

        var document = Document.Create(container =>
        {
            foreach (var ticket in tickets)
            {
                var movie = ticket.BookingSeat.Booking.Session.Movie;
                var session = ticket.BookingSeat.Booking.Session;
                var seat = ticket.BookingSeat.Seat;
                var qrBytes = _qrCodeService.GenerateQrCode(ticket.TicketCode);
                var seatTypeLabel = seat.Type.ToString();

                decimal priceMultiplier = seat.Type switch
                {
                    SeatType.VIP => 1.5m,
                    SeatType.Disabled => 0.8m,
                    _ => 1.0m
                };
                decimal ticketPrice = session.BasePrice * priceMultiplier;

                container.Page(page =>
                {
                    page.Size(PageSizes.A6.Landscape());
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

                    page.Content().Column(col =>
                    {
                        col.Item().Background(DarkBg).Padding(14).Row(row =>
                        {
                            row.RelativeItem().Row(inner =>
                            {
                                inner.ConstantItem(32).Height(32)
                                    .Background(AccentColor)
                                    .AlignCenter().AlignMiddle()
                                    .Text("★").FontSize(18).FontColor(DarkBg);

                                inner.ConstantItem(8);

                                inner.RelativeItem().Column(brand =>
                                {
                                    brand.Item().Text("STARPLEX").FontSize(16).SemiBold().FontColor(Colors.White);
                                    brand.Item().Text("Cinema Network").FontSize(9).FontColor(TextMuted);
                                });
                            });

                            row.AutoItem().AlignMiddle()
                                .Border(0.5f).BorderColor(AccentColor)
                                .PaddingHorizontal(10).PaddingVertical(4)
                                .Text("E-TICKET").FontSize(10).FontColor(AccentColor);
                        });

                        col.Item().Padding(14).Row(row =>
                        {
                            row.RelativeItem().Column(main =>
                            {
                                main.Item().Text(movie.Title).FontSize(16).SemiBold().FontColor(Colors.Black);
                                main.Item().PaddingBottom(10).Text($"{movie.Genre}  ·  {movie.AgeRating}").FontSize(10).FontColor("#666666");

                                main.Item().PaddingBottom(10).Row(grid =>
                                {
                                    grid.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("DATE").FontSize(9).FontColor("#999999");
                                        c.Item().Text(session.StartTime.ToString("dd MMM yyyy")).FontSize(12).SemiBold();
                                    });
                                    grid.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("TIME").FontSize(9).FontColor("#999999");
                                        c.Item().Text(session.StartTime.ToString("HH:mm")).FontSize(14).SemiBold().FontColor(AccentColor);
                                    });
                                    grid.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("HALL").FontSize(9).FontColor("#999999");
                                        c.Item().Text(session.Hall.Name).FontSize(12).SemiBold();
                                    });
                                });

                                main.Item().Background("#F5F5F5").Padding(8).Row(seatRow =>
                                {
                                    seatRow.AutoItem().Background(DarkBg).PaddingHorizontal(10).PaddingVertical(4).Text($"Row {seat.Row}").FontSize(11).FontColor(AccentColor);
                                    seatRow.ConstantItem(8);
                                    seatRow.AutoItem().AlignMiddle().Text($"Seat {seat.Number}").FontSize(11).FontColor("#333333");
                                    seatRow.RelativeItem();
                                    seatRow.AutoItem().Border(0.5f).BorderColor(AccentColor).PaddingHorizontal(8).PaddingVertical(3).Text(seatTypeLabel).FontSize(10).FontColor(AccentColor);
                                });
                            });

                            row.ConstantItem(12);

                            row.ConstantItem(110).Column(qrCol =>
                            {
                                qrCol.Item().Width(96).Height(96).Image(qrBytes);
                                qrCol.Item().PaddingTop(6).AlignCenter().Text(ticket.TicketCode).FontSize(7).FontColor("#999999");
                            });
                        });

                        col.Item().BorderTop(0.5f).BorderColor("#E0E0E0").Background("#FAFAFA").PaddingHorizontal(14).PaddingVertical(8).Row(footer =>
                        {
                            footer.RelativeItem().AlignMiddle().Text("Present QR code at the entrance").FontSize(9).FontColor("#999999");
                            footer.AutoItem().AlignMiddle().Row(price =>
                            {
                                price.AutoItem().Text("Price  ").FontSize(10).FontColor("#999999");
                                price.AutoItem().Text($"{ticketPrice:0.00} ₴").FontSize(14).SemiBold().FontColor(Colors.Black);
                            });
                        });
                    });
                });
            }
        });

        return document.GeneratePdf();
    }
}