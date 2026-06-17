using ElevatorSim.Application.Strategies;
using ElevatorSim.Domain.Interfaces;
using Moq;

namespace ElevatorSim.Tests.Application;

public class NearestElevatorStrategyTests
{
    private readonly NearestElevatorStrategy _strategy = new();
    private static IElevator CreateElevator(string id, int currentFloor, bool isAtCapacity)
    {
        Mock<IElevator> mock = new Mock<IElevator>();
        mock.Setup(e => e.Id).Returns(id);
        mock.Setup(e => e.CurrentFloor).Returns(currentFloor);
        mock.Setup(e => e.IsAtCapacity).Returns(isAtCapacity);
        mock.Setup(e => e.PassengerCount).Returns(isAtCapacity ? 10 : 0);
        mock.Setup(e => e.MaxCapacity).Returns(10);
        return mock.Object;
    }

    [Fact]
    public void SelectElevator_ReturnsClosestElevator()
    {
        IElevator nearElevator = CreateElevator(id: "E1", currentFloor: 3, isAtCapacity: false);
        IElevator farElevator = CreateElevator(id: "E2", currentFloor: 9, isAtCapacity: false);

        List<IElevator> elevators = new List<IElevator> { farElevator, nearElevator };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Equal("E1", result?.Id);
    }

    [Fact]
    public void SelectElevator_SkipsElevatorsAtCapacity()
    {
        IElevator nearButFull = CreateElevator(id: "E1", currentFloor: 3, isAtCapacity: true);
        IElevator farButAvailable = CreateElevator(id: "E2", currentFloor: 9, isAtCapacity: false);

        List<IElevator> elevators = new List<IElevator> { nearButFull, farButAvailable };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Equal("E2", result?.Id);
    }

    [Fact]
    public void SelectElevator_ReturnsNull_WhenAllElevatorsAtCapacity()
    {
        IElevator elevator1 = CreateElevator(id: "E1", currentFloor: 3, isAtCapacity: true);
        IElevator elevator2 = CreateElevator(id: "E2", currentFloor: 9, isAtCapacity: true);

        List<IElevator> elevators = new List<IElevator> { elevator1, elevator2 };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Null(result);
    }

    [Fact]
    public void SelectElevator_SkipsElevator_WhenGroupWillNotFit()
    {
        IElevator nearButTooSmall = CreateElevator(id: "E1", currentFloor: 3, isAtCapacity: false);
        Mock.Get(nearButTooSmall).Setup(e => e.PassengerCount).Returns(8);
        IElevator farButEnoughSpace = CreateElevator(id: "E2", currentFloor: 9, isAtCapacity: false);

        List<IElevator> elevators = new List<IElevator> { nearButTooSmall, farButEnoughSpace };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 3);

        Assert.Equal("E2", result?.Id);
    }
}
