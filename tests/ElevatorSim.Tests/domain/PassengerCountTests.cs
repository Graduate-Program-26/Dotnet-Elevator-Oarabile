using ElevatorSim.Domain.ValueObjects;
using FluentAssertions;

namespace ElevatorSim.Tests.Domain;

public class PassengerCountTests
{
    [Fact]
    public void PassengerCount_CreatesSuccessfully_WhenValueIsNonNegative()
    {
        var passengerCount = new PassengerCount(5);

        passengerCount.Value.Should().Be(5);
    }

    [Fact]
    public void PassengerCount_CreatesSuccessfully_WhenValueIsZero()
    {
        var passengerCount = new PassengerCount(0);

        passengerCount.Value.Should().Be(0);
    }

    [Fact]
    public void PassengerCount_Throws_WhenValueIsNegative()
    {
        var act = () => new PassengerCount(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PassengerCount_ToString_ReturnsValueAsString()
    {
        var passengerCount = new PassengerCount(8);

        var result = passengerCount.ToString();

        result.Should().Be("8");
    }

}