using ElevatorSim.Application.Strategies;
using ElevatorSim.Domain.Enums;
using ElevatorSim.Domain.Interfaces;
using Moq;

namespace ElevatorSim.Tests.Application;

public class EmergencystrategyTests
{
    private readonly Emergencystrategy _strategy = new();
    private static IElevator CreateElevator(
        string id,
        int currentFloor,
        double secondsPerFloor,
        ElevatorState state,
        bool isAtCapacity = false)
    {
        var mock = new Mock<IElevator>();
        mock.Setup(e => e.Id).Returns(id);
        mock.Setup(e => e.CurrentFloor).Returns(currentFloor);
        mock.Setup(e => e.SecondsPerFloor).Returns(secondsPerFloor);
        mock.Setup(e => e.State).Returns(state);
        mock.Setup(e => e.IsAtCapacity).Returns(isAtCapacity);
        return mock.Object;
    }

    [Fact]
    public void SelectElevator_ReturnsFastestElevator_NotNearestOne()
    {
        IElevator nearButSlow = CreateElevator(id: "E1", currentFloor: 3, secondsPerFloor: 0.8, state: ElevatorState.Idle);

        IElevator farButFast = CreateElevator(id: "E2", currentFloor: 9, secondsPerFloor: 0.2, state: ElevatorState.Idle);

        List<IElevator> elevators = new List<IElevator> { nearButSlow, farButFast };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Equal("E2", result?.Id);
    }

    [Fact]
    public void SelectElevator_IgnoresCapacity_StillReturnsFullElevator()
    {
        IElevator fullElevator = CreateElevator(
              id: "E1",
              currentFloor: 3,
              secondsPerFloor: 0.4,
              state: ElevatorState.Idle,
              isAtCapacity: true);

        List<IElevator> elevators = new List<IElevator> { fullElevator };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Equal("E1", result?.Id);
    }

    [Fact]
    public void SelectElevator_SkipsOutOfServiceElevators()
    {
        IElevator outOfService = CreateElevator(id: "E1", currentFloor: 4, secondsPerFloor: 0.2, state: ElevatorState.OutOfService);
        IElevator available = CreateElevator(id: "E2", currentFloor: 9, secondsPerFloor: 0.8, state: ElevatorState.Idle);

        List<IElevator> elevators = new List<IElevator> { outOfService, available };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Equal("E2", result?.Id);
    }

    [Fact]
    public void SelectElevator_ReturnsNull_WhenAllElevatorsOutOfService()
    {
        IElevator elevator1 = CreateElevator(id: "E1", currentFloor: 3, secondsPerFloor: 0.4, state: ElevatorState.OutOfService);
        IElevator elevator2 = CreateElevator(id: "E2", currentFloor: 9, secondsPerFloor: 0.4, state: ElevatorState.OutOfService);

        List<IElevator> elevators = new List<IElevator> { elevator1, elevator2 };

        var result = _strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Null(result);
    }
}