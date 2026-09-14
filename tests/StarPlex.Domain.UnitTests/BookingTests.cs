using System;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Domain.UnitTests;

public class BookingTests
{
    private Booking CreateTestBooking(Guid userId, DateTime sessionStartTime)
    {
        var booking = new Booking(userId, Guid.NewGuid(), 100, DateTime.UtcNow, BookingStatus.Confirmed);
        var session = new Session(Guid.NewGuid(), Guid.NewGuid(), sessionStartTime, 120, 100, 100, SessionStatus.Active);
        
        // Use reflection to set private Session for test setup, since we didn't add a constructor for Session
        typeof(Booking).GetProperty("Session")!.SetValue(booking, session);
        return booking;
    }

    [Fact]
    public void CanCancel_WithValidTime_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestTime = DateTime.UtcNow;
        var sessionStartTime = requestTime.AddHours(2); // 2 hours from now, valid
        var booking = CreateTestBooking(userId, sessionStartTime);

        // Act
        var result = booking.CanCancel(requestTime);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanCancel_TooLate_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestTime = DateTime.UtcNow;
        var sessionStartTime = requestTime.AddMinutes(30); // Less than 60 mins from now, invalid
        var booking = CreateTestBooking(userId, sessionStartTime);

        // Act
        var result = booking.CanCancel(requestTime);

        // Assert
        Assert.False(result);
    }



    [Fact]
    public void CancelIfEmpty_WithRemainingSeats_DoesNotChangeStatus()
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), 100, DateTime.UtcNow, BookingStatus.Confirmed);

        // Act
        booking.CancelIfEmpty(remainingSeatsCount: 1);

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void CancelIfEmpty_WithNoRemainingSeats_ChangesStatusToCancelled()
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), 100, DateTime.UtcNow, BookingStatus.Confirmed);

        // Act
        booking.CancelIfEmpty(remainingSeatsCount: 0);

        // Assert
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }
}
