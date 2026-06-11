using ElevatorSim.Domain.Exceptions;
using ElevatorSim.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ElevatorSim.Tests.Domain;

public class FloorNumberTests
{
    private const int s_minFloor = 1;
    private const int s_maxFloor = 10;

    [Fact]
    public void FloorNumber_CreatesSuccessfully_WhenValueWithinRange()
    {
        var floorNumber = new FloorNumber(5, s_minFloor, s_maxFloor);
        floorNumber.Value.Should().Be(5);
    }

    [Fact]
    public void FloorNumber_Throws_WhenValueBelowMinimum()
    {
        var act = () => new FloorNumber(0, s_minFloor, s_maxFloor);

        act.Should().Throw<InvalidFloorException>()
            .WithMessage("*0*");
    }

    [Fact]
    public void FloorNumber_Throws_WhenValueAboveMaximum()
    {
        var act = () => new FloorNumber(11, s_minFloor, s_maxFloor);

        act.Should().Throw<InvalidFloorException>()
            .WithMessage("*11*");
    }

    [Fact]
    public void FloorNumber_ToString_ReturnsValueAsString()
    {
        var floorNumber = new FloorNumber(7, s_minFloor, s_maxFloor);

        var result = floorNumber.ToString();

        result.Should().Be("7");
    }
}