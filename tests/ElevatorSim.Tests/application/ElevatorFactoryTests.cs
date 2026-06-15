using ElevatorSim.Application.Factories;
using ElevatorSim.Domain.Entities;
using ElevatorSim.Domain.Enums;

namespace ElevatorSim.Tests.Application;

public class ElevatorFactoryTests
{
    private readonly ElevatorFactory _factory = new ();

    [Fact]
    public void Create_ReturnsPassengerElevator_WhenTypeIsPassenger()
    {
        var elevator = _factory.Create(ElevatorType.Passanger, "E1", startingFloor: 1);

        Assert.IsType<PassengerElevator>(elevator);
        Assert.Equal("E1", elevator.Id);
        Assert.Equal(1, elevator.CurrentFloor);
    }
}
