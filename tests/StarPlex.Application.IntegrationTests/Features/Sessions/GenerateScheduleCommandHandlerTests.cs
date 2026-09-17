using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Helpers;
using StarPlex.Application.Features.Sessions.Commands.GenerateSchedule;
using StarPlex.Application.IntegrationTests.Infrastructure;
using StarPlex.Domain.Entities;
using StarPlex.Domain.Enums;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Sessions;

public class GenerateScheduleCommandHandlerTests : IntegrationTestBase
{
    public GenerateScheduleCommandHandlerTests(DatabaseFixture fixture) : base(fixture)
    {
        CurrentUserServiceMock.Setup(u => u.IsSuperAdmin).Returns(true);
    }

    [Fact]
    public async Task GenerateSchedule_WhenTargetDateIsInPastInKyiv_ShouldThrowBusinessRuleException()
    {
        var cinema = new Cinema("StarPlex Past", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Hall 1", 10, 10);
        DbContext.Halls.Add(hall);

        var movie = new Movie(101, "Movie 1", "Movie 1", "Desc", 120, "p.jpg", "b.jpg", "Action", "u", "PG", 8.5, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var nowKyiv = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneHelpers.KyivTimeZone);
        var pastKyivDate = nowKyiv.Date.AddDays(-1);

        var command = new GenerateScheduleCommand
        {
            CinemaId = cinema.Id,
            TargetDate = pastKyivDate,
            BasePrice = 150,
            MovieIds = new List<Guid> { movie.Id }
        };

        var act = () => Mediator.Send(command);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Cannot generate a schedule for a past date.");
    }

    [Fact]
    public async Task GenerateSchedule_WhenMultipleHallsAndMovies_ShouldStaggerMovieRotationAcrossHalls()
    {
        var cinema = new Cinema("StarPlex Multi", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall1 = new Hall(cinema.Id, "Hall A", 10, 10);
        var hall2 = new Hall(cinema.Id, "Hall B", 10, 10);
        DbContext.Halls.AddRange(hall1, hall2);

        var movie1 = new Movie(101, "Movie High Rating", "Movie A", "Desc", 100, "p.jpg", "b.jpg", "Action", "u", "PG", 9.0, MovieStatus.NowShowing, DateTime.UtcNow);
        var movie2 = new Movie(102, "Movie Low Rating", "Movie B", "Desc", 100, "p.jpg", "b.jpg", "Comedy", "u", "PG", 7.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.AddRange(movie1, movie2);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var nowKyiv = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneHelpers.KyivTimeZone);
        var targetDate = nowKyiv.Date.AddDays(2);
        var command = new GenerateScheduleCommand
        {
            CinemaId = cinema.Id,
            TargetDate = targetDate,
            BasePrice = 150,
            MovieIds = new List<Guid> { movie1.Id, movie2.Id }
        };

        var count = await Mediator.Send(command);

        count.Should().BeGreaterThan(0);

        var kyivTzi = TimeZoneHelpers.KyivTimeZone;

        var hall1Sessions = await DbContext.Sessions
            .Where(s => s.HallId == hall1.Id)
            .ToListAsync();

        var hall2Sessions = await DbContext.Sessions
            .Where(s => s.HallId == hall2.Id)
            .ToListAsync();

        var hall1FirstSession = hall1Sessions
            .Where(s => TimeZoneInfo.ConvertTimeFromUtc(s.StartTime, kyivTzi).Date == targetDate)
            .OrderBy(s => s.StartTime)
            .FirstOrDefault();

        var hall2FirstSession = hall2Sessions
            .Where(s => TimeZoneInfo.ConvertTimeFromUtc(s.StartTime, kyivTzi).Date == targetDate)
            .OrderBy(s => s.StartTime)
            .FirstOrDefault();

        hall1FirstSession.Should().NotBeNull();
        hall2FirstSession.Should().NotBeNull();

        // Hall 1 (index 0) starts with Movie 1 (highest rating)
        hall1FirstSession!.MovieId.Should().Be(movie1.Id);

        // Hall 2 (index 1) starts with Movie 2 (staggered offset index 1)
        hall2FirstSession!.MovieId.Should().Be(movie2.Id);
    }

    [Fact]
    public async Task GenerateSchedule_ShouldGenerateFirstSessionAt1000KyivTimeAndPreserveBusinessDate()
    {
        var cinema = new Cinema("StarPlex Date", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Hall 1", 10, 10);
        DbContext.Halls.Add(hall);

        var movie = new Movie(103, "Movie Test", "Movie T", "Desc", 90, "p.jpg", "b.jpg", "Drama", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var nowKyiv = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneHelpers.KyivTimeZone);
        var targetDate = nowKyiv.Date.AddDays(1);
        var command = new GenerateScheduleCommand
        {
            CinemaId = cinema.Id,
            TargetDate = targetDate,
            BasePrice = 150,
            MovieIds = new List<Guid> { movie.Id }
        };

        await Mediator.Send(command);

        var generatedSessions = await DbContext.Sessions
            .Where(s => s.HallId == hall.Id)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        generatedSessions.Should().NotBeEmpty();

        var kyivTzi = TimeZoneHelpers.KyivTimeZone;

        generatedSessions.All(s => TimeZoneInfo.ConvertTimeFromUtc(s.StartTime, kyivTzi).Date == targetDate).Should().BeTrue();

        var firstSessionKyivTime = TimeZoneInfo.ConvertTimeFromUtc(generatedSessions.First().StartTime, kyivTzi);
        firstSessionKyivTime.Hour.Should().Be(10);
        firstSessionKyivTime.Minute.Should().Be(0);
    }

    [Fact]
    public async Task GenerateSchedule_ShouldHandleDSTConversionsCorrectlyForSummerAndWinterDates()
    {
        var cinema = new Cinema("StarPlex DST", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Hall 1", 10, 10);
        DbContext.Halls.Add(hall);

        var movie = new Movie(104, "Movie DST", "Movie DST", "Desc", 90, "p.jpg", "b.jpg", "Drama", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var nowKyiv = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneHelpers.KyivTimeZone);
        var futureYear = nowKyiv.Year + 1;

        var summerTargetDate = new DateTime(futureYear, 7, 15);
        var summerCommand = new GenerateScheduleCommand
        {
            CinemaId = cinema.Id,
            TargetDate = summerTargetDate,
            BasePrice = 150,
            MovieIds = new List<Guid> { movie.Id }
        };

        await Mediator.Send(summerCommand);

        var summerFirstSession = await DbContext.Sessions
            .Where(s => s.HallId == hall.Id)
            .OrderBy(s => s.StartTime)
            .FirstOrDefaultAsync();

        summerFirstSession.Should().NotBeNull();
        // Summer DST (EEST, UTC+3): 10:00 Kyiv time -> 07:00 UTC
        summerFirstSession!.StartTime.Should().Be(new DateTime(futureYear, 7, 15, 7, 0, 0, DateTimeKind.Utc));

        DbContext.Sessions.RemoveRange(DbContext.Sessions);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var winterTargetDate = new DateTime(futureYear, 1, 15);
        var winterCommand = new GenerateScheduleCommand
        {
            CinemaId = cinema.Id,
            TargetDate = winterTargetDate,
            BasePrice = 150,
            MovieIds = new List<Guid> { movie.Id }
        };

        await Mediator.Send(winterCommand);

        var winterFirstSession = await DbContext.Sessions
            .Where(s => s.HallId == hall.Id)
            .OrderBy(s => s.StartTime)
            .FirstOrDefaultAsync();

        winterFirstSession.Should().NotBeNull();
        // Winter EET (UTC+2): 10:00 Kyiv time -> 08:00 UTC
        winterFirstSession!.StartTime.Should().Be(new DateTime(futureYear, 1, 15, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GenerateSchedule_OccupiedSlotEndingExactlyAt2300_ShouldBeAllowed()
    {
        var cinema = new Cinema("StarPlex ExactCutoff", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Hall 1", 10, 10);
        DbContext.Halls.Add(hall);

        // Movie duration = 110m + 20m cleanup = 130m per slot.
        // Slots: 10:00 (ends 12:10), 12:10 (ends 14:20), 14:20 (ends 16:30), 16:30 (ends 18:40), 18:40 (ends 20:50), 20:50 (ends 22:40 + 20m cleanup = 23:00).
        var movie = new Movie(105, "Exact Movie", "Exact Movie", "Desc", 110, "p.jpg", "b.jpg", "Drama", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var nowKyiv = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneHelpers.KyivTimeZone);
        var targetDate = nowKyiv.Date.AddDays(3);
        var command = new GenerateScheduleCommand
        {
            CinemaId = cinema.Id,
            TargetDate = targetDate,
            BasePrice = 150,
            MovieIds = new List<Guid> { movie.Id }
        };

        var count = await Mediator.Send(command);
        count.Should().Be(6);

        var kyivTzi = TimeZoneHelpers.KyivTimeZone;
        var generatedSessions = await DbContext.Sessions
            .Where(s => s.HallId == hall.Id)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        var lastSession = generatedSessions.Last();
        var lastStartKyiv = TimeZoneInfo.ConvertTimeFromUtc(lastSession.StartTime, kyivTzi);
        var occupiedEndKyiv = lastStartKyiv.AddMinutes(lastSession.MovieDurationInMinutes + 20);

        occupiedEndKyiv.TimeOfDay.Should().Be(new TimeSpan(23, 0, 0));
    }

    [Fact]
    public async Task GenerateSchedule_MovieEndingBefore2300ButCleanupExtendsAfter2300_ShouldNotGenerateSession()
    {
        var cinema = new Cinema("StarPlex CleanupCutoff", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall = new Hall(cinema.Id, "Hall 1", 10, 10);
        DbContext.Halls.Add(hall);

        // Movie duration = 111m + 20m cleanup = 131m per slot, rounded up to 5-min start intervals (10:00, 12:15, 14:30, 16:45, 19:00).
        // 5th valid session: start 19:00, movie end 20:51, cleanup ends at 21:11.
        // The late candidates starting at 20:55 (movie end 22:46, occupied end 23:06) or 21:15 (movie end 23:06, occupied end 23:26) MUST NOT be generated!
        var movie = new Movie(106, "Cleanup Exceeds Movie", "Cleanup Exceeds", "Desc", 111, "p.jpg", "b.jpg", "Action", "u", "PG", 8.0, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.Add(movie);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var nowKyiv = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneHelpers.KyivTimeZone);
        var targetDate = nowKyiv.Date.AddDays(4);
        var command = new GenerateScheduleCommand
        {
            CinemaId = cinema.Id,
            TargetDate = targetDate,
            BasePrice = 150,
            MovieIds = new List<Guid> { movie.Id }
        };

        var count = await Mediator.Send(command);

        var kyivTzi = TimeZoneHelpers.KyivTimeZone;
        var generatedSessions = await DbContext.Sessions
            .Where(s => s.HallId == hall.Id)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        // 1. Assert exactly 5 sessions were generated for this hall
        count.Should().Be(5);
        generatedSessions.Should().HaveCount(5);

        // 2. Assert the 5th (last) generated session starts at 19:00, movie ends at 20:51, cleanup ends at 21:11
        var lastSession = generatedSessions.Last();
        var lastStartKyiv = TimeZoneInfo.ConvertTimeFromUtc(lastSession.StartTime, kyivTzi);
        var lastMovieEndKyiv = lastStartKyiv.AddMinutes(lastSession.MovieDurationInMinutes);
        var lastOccupiedEndKyiv = lastStartKyiv.AddMinutes(lastSession.MovieDurationInMinutes + 20);

        lastStartKyiv.TimeOfDay.Should().Be(new TimeSpan(19, 0, 0));
        lastMovieEndKyiv.TimeOfDay.Should().Be(new TimeSpan(20, 51, 0));
        lastOccupiedEndKyiv.TimeOfDay.Should().Be(new TimeSpan(21, 11, 0));

        // 3. Explicitly verify that late candidate starting at/after 20:55 (whose occupied cleanup extends past 23:00) was NOT created
        generatedSessions.Any(s =>
        {
            var startKyiv = TimeZoneInfo.ConvertTimeFromUtc(s.StartTime, kyivTzi);
            return startKyiv.TimeOfDay >= new TimeSpan(20, 55, 0);
        }).Should().BeFalse();
    }

    [Fact]
    public async Task GenerateSchedule_AllGeneratedSessions_MustHaveOccupiedSlotEndingAtOrBefore2300KyivTime()
    {
        var cinema = new Cinema("StarPlex Invariant", "Main St", "Kyiv");
        DbContext.Cinemas.Add(cinema);

        var hall1 = new Hall(cinema.Id, "Hall 1", 10, 10);
        var hall2 = new Hall(cinema.Id, "Hall 2", 10, 10);
        DbContext.Halls.AddRange(hall1, hall2);

        var movie1 = new Movie(107, "Long Movie", "Long Movie", "Desc", 135, "p.jpg", "b.jpg", "Sci-Fi", "u", "PG", 8.5, MovieStatus.NowShowing, DateTime.UtcNow);
        var movie2 = new Movie(108, "Short Movie", "Short Movie", "Desc", 75, "p.jpg", "b.jpg", "Comedy", "u", "PG", 7.5, MovieStatus.NowShowing, DateTime.UtcNow);
        DbContext.Movies.AddRange(movie1, movie2);
        await DbContext.SaveChangesAsync(CancellationToken.None);

        var nowKyiv = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneHelpers.KyivTimeZone);
        var targetDate = nowKyiv.Date.AddDays(5);
        var command = new GenerateScheduleCommand
        {
            CinemaId = cinema.Id,
            TargetDate = targetDate,
            BasePrice = 150,
            MovieIds = new List<Guid> { movie1.Id, movie2.Id }
        };

        var count = await Mediator.Send(command);
        count.Should().BeGreaterThan(0);

        var kyivTzi = TimeZoneHelpers.KyivTimeZone;
        var generatedSessions = await DbContext.Sessions
            .Where(s => s.Hall.CinemaId == cinema.Id)
            .ToListAsync();

        foreach (var s in generatedSessions)
        {
            var startKyiv = TimeZoneInfo.ConvertTimeFromUtc(s.StartTime, kyivTzi);
            var occupiedEndKyiv = startKyiv.AddMinutes(s.MovieDurationInMinutes + 20);

            // 1. Session occupied slot does not cross midnight into next date
            occupiedEndKyiv.Date.Should().Be(targetDate);

            // 2. Session occupied slot ends at or before 23:00 local time
            occupiedEndKyiv.TimeOfDay.Should().BeLessThanOrEqualTo(new TimeSpan(23, 0, 0));
        }
    }
}
