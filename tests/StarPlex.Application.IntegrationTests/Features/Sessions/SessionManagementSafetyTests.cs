using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Helpers;
using StarPlex.Application.Features.Sessions.Commands.CreateSession;
using StarPlex.Application.Features.Sessions.Commands.MoveSession;
using StarPlex.Application.Features.Sessions.Commands.UpdateSession;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Sessions;

public class SessionManagementSafetyTests : IntegrationTestBase
{
    public SessionManagementSafetyTests()
    {
        CurrentUserServiceMock.Setup(u => u.IsSuperAdmin).Returns(true);
    }

    private static DateTime GetFutureKyivDateTime(int month, int day, int hour, int minute)
    {
        var kyivTzi = TimeZoneHelpers.KyivTimeZone;
        var nowKyiv = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, kyivTzi);
        var futureYear = nowKyiv.Year + 1;
        var localKyiv = new DateTime(futureYear, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localKyiv, kyivTzi);
    }

    [Fact]
    public async Task UpdateSession_WhenChangingMovieIdWithConfirmedBookings_ShouldThrowBusinessRuleException()
    {
        var cinema = new Cinema("StarPlex Guard 1", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);
        var hall = new Hall(cinema.Id, "Hall 1", 10, 10);
        DbContext.Halls.Add(hall);

        var movie1 = new Movie(201, "Movie 1", "M1", "Desc", 90, "p.jpg", "b.jpg", "Action", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        var movie2 = new Movie(202, "Movie 2", "M2", "Desc", 90, "p.jpg", "b.jpg", "Comedy", "u", "PG", 7.5, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.AddRange(movie1, movie2);

        var startTimeUtc = GetFutureKyivDateTime(7, 15, 12, 0);
        var session = new Session(movie1.Id, hall.Id, startTimeUtc, 90, 150, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);

        var userId = Guid.NewGuid();
        var booking = new Booking(userId, session.Id, 150m, DateTime.UtcNow, BookingStatus.Confirmed);
        DbContext.Bookings.Add(booking);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        var command = new UpdateSessionCommand
        {
            Id = session.Id,
            MovieId = movie2.Id,
            HallId = hall.Id,
            StartTime = startTimeUtc,
            BasePrice = 150
        };

        var act = () => Mediator.Send(command);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Cannot change session time, hall, or movie because there are active bookings*");
    }

    [Fact]
    public async Task UpdateSession_WhenPriceOnlyChangeWithConfirmedBookings_ShouldBeAllowed()
    {
        var cinema = new Cinema("StarPlex Guard 2", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);
        var hall = new Hall(cinema.Id, "Hall 1", 10, 10);
        DbContext.Halls.Add(hall);

        var movie = new Movie(203, "Movie 1", "M1", "Desc", 90, "p.jpg", "b.jpg", "Action", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var startTimeUtc = GetFutureKyivDateTime(7, 15, 12, 0);
        var session = new Session(movie.Id, hall.Id, startTimeUtc, 90, 150, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);

        var userId = Guid.NewGuid();
        var booking = new Booking(userId, session.Id, 150m, DateTime.UtcNow, BookingStatus.Confirmed);
        DbContext.Bookings.Add(booking);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        var command = new UpdateSessionCommand
        {
            Id = session.Id,
            MovieId = movie.Id,
            HallId = hall.Id,
            StartTime = startTimeUtc,
            BasePrice = 180
        };

        await Mediator.Send(command);

        var updatedSession = await DbContext.Sessions.FindAsync(session.Id);
        updatedSession!.BasePrice.Should().Be(180);
    }

    [Fact]
    public async Task UpdateSession_WhenChangingStartTimeOrHallWithConfirmedBookings_ShouldThrowBusinessRuleException()
    {
        var cinema = new Cinema("StarPlex Guard 3", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);
        var hall1 = new Hall(cinema.Id, "Hall 1", 10, 10);
        var hall2 = new Hall(cinema.Id, "Hall 2", 10, 10);
        DbContext.Halls.AddRange(hall1, hall2);

        var movie = new Movie(204, "Movie 1", "M1", "Desc", 90, "p.jpg", "b.jpg", "Action", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var startTimeUtc = GetFutureKyivDateTime(7, 15, 12, 0);
        var session = new Session(movie.Id, hall1.Id, startTimeUtc, 90, 150, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);

        var userId = Guid.NewGuid();
        var booking = new Booking(userId, session.Id, 150m, DateTime.UtcNow, BookingStatus.Confirmed);
        DbContext.Bookings.Add(booking);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        var command = new UpdateSessionCommand
        {
            Id = session.Id,
            MovieId = movie.Id,
            HallId = hall2.Id,
            StartTime = startTimeUtc,
            BasePrice = 150
        };

        var act = () => Mediator.Send(command);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Cannot change session time, hall, or movie because there are active bookings*");
    }

    [Fact]
    public async Task CreateSession_WhenValidKyivTimeInside10To23_ShouldSucceed()
    {
        var cinema = new Cinema("StarPlex Valid", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);
        var hall = new Hall(cinema.Id, "Hall 1", 10, 10);
        DbContext.Halls.Add(hall);

        var movie = new Movie(205, "Movie 1", "M1", "Desc", 90, "p.jpg", "b.jpg", "Action", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var startTimeUtc = GetFutureKyivDateTime(7, 15, 10, 0);
        var command = new CreateSessionCommand
        {
            MovieId = movie.Id,
            HallId = hall.Id,
            StartTime = startTimeUtc,
            BasePrice = 150
        };

        var sessionId = await Mediator.Send(command);
        sessionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateSession_WhenStartingBefore1000KyivTime_ShouldThrowBusinessRuleException()
    {
        var cinema = new Cinema("StarPlex Early", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);
        var hall = new Hall(cinema.Id, "Hall 1", 10, 10);
        DbContext.Halls.Add(hall);

        var movie = new Movie(206, "Movie 1", "M1", "Desc", 90, "p.jpg", "b.jpg", "Action", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var startTimeUtc = GetFutureKyivDateTime(7, 15, 9, 55);
        var command = new CreateSessionCommand
        {
            MovieId = movie.Id,
            HallId = hall.Id,
            StartTime = startTimeUtc,
            BasePrice = 150
        };

        var act = () => Mediator.Send(command);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*outside cinema working hours*");
    }

    [Fact]
    public async Task CreateSession_WhenCleanupExtendsPast2300KyivTime_ShouldThrowBusinessRuleException()
    {
        var cinema = new Cinema("StarPlex Late", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);
        var hall = new Hall(cinema.Id, "Hall 1", 10, 10);
        DbContext.Halls.Add(hall);

        var movie = new Movie(207, "Movie 1", "M1", "Desc", 120, "p.jpg", "b.jpg", "Action", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var startTimeUtc = GetFutureKyivDateTime(7, 15, 21, 10);
        var command = new CreateSessionCommand
        {
            MovieId = movie.Id,
            HallId = hall.Id,
            StartTime = startTimeUtc,
            BasePrice = 150
        };

        var act = () => Mediator.Send(command);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*outside cinema working hours*");
    }

    [Fact]
    public async Task MoveSession_WhenConfirmedBookingsExist_ShouldThrowBusinessRuleException()
    {
        var cinema = new Cinema("StarPlex Move Guard", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);
        var hall1 = new Hall(cinema.Id, "Hall 1", 10, 10);
        var hall2 = new Hall(cinema.Id, "Hall 2", 10, 10);
        DbContext.Halls.AddRange(hall1, hall2);

        var movie = new Movie(208, "Movie 1", "M1", "Desc", 90, "p.jpg", "b.jpg", "Action", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);

        var startTimeUtc = GetFutureKyivDateTime(7, 15, 12, 0);
        var session = new Session(movie.Id, hall1.Id, startTimeUtc, 90, 150, 150, SessionStatus.Active);
        DbContext.Sessions.Add(session);

        var userId = Guid.NewGuid();
        var booking = new Booking(userId, session.Id, 150m, DateTime.UtcNow, BookingStatus.Confirmed);
        DbContext.Bookings.Add(booking);

        await DbContext.SaveChangesAsync(CancellationToken.None);

        var newTimeUtc = GetFutureKyivDateTime(7, 15, 14, 0);
        var command = new MoveSessionCommand
        {
            SessionId = session.Id,
            HallId = hall2.Id,
            NewStartTime = newTimeUtc
        };

        var act = () => Mediator.Send(command);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Cannot move session because there are active bookings*");
    }
}
