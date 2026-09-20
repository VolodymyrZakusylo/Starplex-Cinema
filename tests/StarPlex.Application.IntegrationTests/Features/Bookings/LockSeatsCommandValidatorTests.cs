using FluentValidation.TestHelper;
using StarPlex.Application.Features.Bookings.Commands.LockSeats;
using Xunit;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

public class LockSeatsCommandValidatorTests
{
    private readonly LockSeatsCommandValidator _validator;

    public LockSeatsCommandValidatorTests()
    {
        _validator = new LockSeatsCommandValidator();
    }

    [Fact]
    public void Should_Have_Error_When_SeatIds_Is_Empty()
    {
        var command = new LockSeatsCommand { SessionId = Guid.NewGuid(), SeatIds = new List<Guid>() };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SeatIds).WithErrorMessage("You must select at least one seat.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_SeatIds_Count_Is_Valid()
    {
        var seats = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToList();
        var command = new LockSeatsCommand { SessionId = Guid.NewGuid(), SeatIds = seats };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.SeatIds);
    }

    [Fact]
    public void Should_Have_Error_When_SeatIds_Count_Exceeds_20()
    {
        var seats = Enumerable.Range(0, 21).Select(_ => Guid.NewGuid()).ToList();
        var command = new LockSeatsCommand { SessionId = Guid.NewGuid(), SeatIds = seats };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SeatIds).WithErrorMessage("You cannot lock more than 20 seats at a time.");
    }

    [Fact]
    public void Should_Have_Error_When_SeatIds_Is_Null()
    {
        var command = new LockSeatsCommand { SessionId = Guid.NewGuid(), SeatIds = null! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SeatIds);
    }
}
