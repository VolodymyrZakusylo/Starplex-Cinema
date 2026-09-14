using System;
using StarPlex.Domain.Entities;

namespace StarPlex.Domain.Factories;

public static class TicketFactory
{
    public static Ticket CreateForSeat(Guid bookingSeatId)
    {
        string ticketSegment = Guid.NewGuid().ToString()[..8].ToUpper();
        string uniqueToken = $"SPX-{ticketSegment}";

        return new Ticket(bookingSeatId, uniqueToken, isUsed: false)
        {
            Id = Guid.NewGuid()
        };
    }
}
