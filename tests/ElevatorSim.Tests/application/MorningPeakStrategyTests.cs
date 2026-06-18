using ElevatorSim.Application.Strategies;
using ElevatorSim.Domain.Enums;
using ElevatorSim.Domain.Interfaces;
using Moq;

namespace ElevatorSim.Tests.Application;

public class MorningPeakStrategyTests
{
    private readonly MorningPeakStrategy _strategy = new();

    private static IElevator CreateElevator(string id, int currentFloor, ElevatorDirection direction, bool isAtCapacity = false)
    {
        Mock<IElevator> mock = new Mock<IElevator>();
        mock.Setup(e => e.Id).Returns(id);
        mock.Setup(e => e.CurrentFloor).Returns(currentFloor);
        mock.Setup(e => e.Direction).Returns(direction);
        mock.Setup(e => e.IsAtCapacity).Returns(isAtCapacity);
        mock.Setup(e => e.PassengerCount).Returns(isAtCapacity ? 10 : 0);
        mock.Setup(e => e.MaxCapacity).Returns(10);
        return mock.Object;
    }

    [Fact]
    public void SelectElevator_PrefersElevatorMovingUpBelowRequestedFloor_OverIdleElevator()
    {
        IElevator movingUp = CreateElevator(id: "E1", currentFloor: 2, direction: ElevatorDirection.Up);
        IElevator idleBelow = CreateElevator(id: "E2", currentFloor: 3, direction: ElevatorDirection.Idle);

        List<IElevator> elevators = new List<IElevator> { idleBelow, movingUp };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Equal("E1", result?.Id);
    }

    [Fact]
    public void SelectElevator_PrefersIdleBelowRequestedFloor_OverIdleAboveRequestedFloor()
    {
        IElevator idleBelow = CreateElevator(id: "E1", currentFloor: 3, direction: ElevatorDirection.Idle);
        IElevator idleAbove = CreateElevator(id: "E2", currentFloor: 8, direction: ElevatorDirection.Idle);

        List<IElevator> elevators = new List<IElevator> { idleAbove, idleBelow };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Equal("E1", result?.Id);
    }

    [Fact]
    public void SelectElevator_PrefersIdleAboveRequestedFloor_OverElevatorMovingDown()
    {
        IElevator idleAbove = CreateElevator(id: "E1", currentFloor: 8, direction: ElevatorDirection.Idle);
        IElevator movingDown = CreateElevator(id: "E2", currentFloor: 9, direction: ElevatorDirection.Down);

        List<IElevator> elevators = new List<IElevator> { movingDown, idleAbove };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Equal("E1", result?.Id);
    }

    [Fact]
    public void SelectElevator_UsesDistanceAsTiebreaker_WhenScoresAreEqual()
    {
        IElevator idleFar = CreateElevator(id: "E1", currentFloor: 1, direction: ElevatorDirection.Idle);
        IElevator idleNear = CreateElevator(id: "E2", currentFloor: 4, direction: ElevatorDirection.Idle);

        List<IElevator> elevators = new List<IElevator> { idleFar, idleNear };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Equal("E2", result?.Id);
    }

    [Fact]
    public void SelectElevator_SkipsElevatorsAtCapacity()
    {
        IElevator movingUpButFull = CreateElevator(id: "E1", currentFloor: 2, direction: ElevatorDirection.Up, isAtCapacity: true);
        IElevator idleAboveButAvailable = CreateElevator(id: "E2", currentFloor: 8, direction: ElevatorDirection.Idle);

        List<IElevator> elevators = new List<IElevator> { movingUpButFull, idleAboveButAvailable };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Equal("E2", result?.Id);
    }

    [Fact]
    public void SelectElevator_ReturnsNull_WhenAllElevatorsAtCapacity()
    {
        IElevator elevator1 = CreateElevator(id: "E1", currentFloor: 2, direction: ElevatorDirection.Up, isAtCapacity: true);
        IElevator elevator2 = CreateElevator(id: "E2", currentFloor: 8, direction: ElevatorDirection.Idle, isAtCapacity: true);

        List<IElevator> elevators = new List<IElevator> { elevator1, elevator2 };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Null(result);
    }

    [Fact]
    public void SelectElevator_SkipsElevator_WhenGroupWillNotFit()
    {
        IElevator movingUpButTooSmall = CreateElevator(id: "E1", currentFloor: 2, direction: ElevatorDirection.Up);
        Mock.Get(movingUpButTooSmall).Setup(e => e.PassengerCount).Returns(9);
        IElevator idleWithSpace = CreateElevator(id: "E2", currentFloor: 8, direction: ElevatorDirection.Idle);

        List<IElevator> elevators = new List<IElevator> { movingUpButTooSmall, idleWithSpace };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 2);

        Assert.Equal("E2", result?.Id);
    }
}
