using StarPlex.Domain.Common;
using StarPlex.Domain.Enums;

namespace StarPlex.Domain.Entities;

public class Ticket : BaseEntity
{
    public Guid BookingSeatId { get; private set; }
    public string TicketCode { get; private set; } = string.Empty;
    public bool IsUsed { get; set; }

    public BookingSeat BookingSeat { get; set; } = null!;

    public Ticket()
    {
    }

    public Ticket(Guid bookingSeatId, string ticketCode, bool isUsed = false)
    {
        BookingSeatId = bookingSeatId;
        TicketCode = ticketCode;
        IsUsed = isUsed;
    }

    public ScanTicketResult Scan(DateTime scanTime, Session session)
    {
        if (BookingSeat?.Booking?.Status != BookingStatus.Confirmed)
        {
            return ScanTicketResult.InvalidStatus;
        }

        if (IsUsed)
        {
            return ScanTicketResult.AlreadyScanned;
        }

        var allowedEntryStart = session.StartTime.AddMinutes(-15);
        var sessionEndTime = session.StartTime.AddMinutes(session.MovieDurationInMinutes);

        if (scanTime < allowedEntryStart)
        {
            return ScanTicketResult.TooEarly;
        }

        if (scanTime > sessionEndTime)
        {
            return ScanTicketResult.Expired;
        }

        IsUsed = true;
        return ScanTicketResult.Success;
    }
}