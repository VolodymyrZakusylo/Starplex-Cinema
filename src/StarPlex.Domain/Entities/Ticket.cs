using StarPlex.Domain.Common;

namespace StarPlex.Domain.Entities;

public class Ticket : BaseEntity
{
    public Guid BookingSeatId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
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
}