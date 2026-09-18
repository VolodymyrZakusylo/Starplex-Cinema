using System;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Domain.UnitTests;

public class TicketTests
{
    private Session CreateTestSession(DateTime startTime)
    {
        return new Session(Guid.NewGuid(), Guid.NewGuid(), startTime, 120, 100, 100, SessionStatus.Active);
    }

    private Ticket CreateTestTicket(BookingStatus status)
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), 100, DateTime.UtcNow, status);
        var bookingSeat = new BookingSeat(booking.Id, Guid.NewGuid(), 100m) { Booking = booking };
        return new Ticket(bookingSeat.Id, "SPX-TEST", isUsed: false) { BookingSeat = bookingSeat };
    }

    [Fact]
    public void Scan_WithConfirmedBooking_AndValidTime_ReturnsSuccess()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(10); // 10 minutes from now
        var session = CreateTestSession(startTime);
        var ticket = CreateTestTicket(BookingStatus.Confirmed);

        // Act
        var result = ticket.Scan(DateTime.UtcNow, session);

        // Assert
        Assert.Equal(ScanTicketResult.Success, result);
        Assert.True(ticket.IsUsed);
    }

    [Fact]
    public void Scan_WithUnconfirmedBooking_ReturnsAccessDenied()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(10);
        var session = CreateTestSession(startTime);
        var ticket = CreateTestTicket(BookingStatus.Pending);

        // Act
        var result = ticket.Scan(DateTime.UtcNow, session);

        // Assert
        Assert.Equal(ScanTicketResult.InvalidStatus, result);
        Assert.False(ticket.IsUsed);
    }

    [Fact]
    public void Scan_AlreadyUsedTicket_ReturnsAlreadyScanned()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(10);
        var session = CreateTestSession(startTime);
        var ticket = CreateTestTicket(BookingStatus.Confirmed);
        ticket.Scan(DateTime.UtcNow, session); // Scan once

        // Act
        var result = ticket.Scan(DateTime.UtcNow, session); // Scan again

        // Assert
        Assert.Equal(ScanTicketResult.AlreadyScanned, result);
    }

    [Fact]
    public void Scan_TooEarly_ReturnsTooEarly()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(20); // 20 minutes from now, allowed is 15
        var session = CreateTestSession(startTime);
        var ticket = CreateTestTicket(BookingStatus.Confirmed);

        // Act
        var result = ticket.Scan(DateTime.UtcNow, session);

        // Assert
        Assert.Equal(ScanTicketResult.TooEarly, result);
        Assert.False(ticket.IsUsed);
    }

    [Fact]
    public void Scan_TooLate_ReturnsExpired()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-150); // Movie is 120 mins, started 150 mins ago (ended)
        var session = CreateTestSession(startTime);
        var ticket = CreateTestTicket(BookingStatus.Confirmed);

        // Act
        var result = ticket.Scan(DateTime.UtcNow, session);

        // Assert
        Assert.Equal(ScanTicketResult.Expired, result);
        Assert.False(ticket.IsUsed);
    }
}
