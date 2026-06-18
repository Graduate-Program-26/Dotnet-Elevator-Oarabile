using ElevatorSim.Application.Controllers;
using ElevatorSim.Application.Strategies;
using ElevatorSim.Domain.Exceptions;
using ElevatorSim.Domain.Interfaces;
using ElevatorSim.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ElevatorSim.Tests.Application;

public class ElevatorControllerTests
{
    private static IElevator CreateElevator(string id, bool isAtCapacity = false)
    {
        var mock = new Mock<IElevator>();
        mock.Setup(e => e.Id).Returns(id);
        mock.Setup(e => e.CurrentFloor).Returns(1);
        mock.Setup(e => e.IsAtCapacity).Returns(isAtCapacity);
        mock.Setup(e => e.PassengerCount).Returns(isAtCapacity ? 10 : 0);
        mock.Setup(e => e.MaxCapacity).Returns(10);
        mock.Setup(e => e.MoveToFloorAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return mock.Object;
    }

    [Fact]
    public async Task RequestElevatorAsync_Throws_WhenFloorIsBelowMinimum()
    {
        var elevator = CreateElevator(id: "E1");
        ElevatorController controller = new ElevatorController([elevator], new NearestElevatorStrategy(), minFloor: 1, maxFloor: 10, NullLogger<ElevatorController>.Instance);

        var act = async () => await controller.RequestElevatorAsync(0, 1, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidFloorException>(act);
    }

    [Fact]
    public async Task RequestElevatorAsync_Throws_WhenFloorIsAboveMaximum()
    {
        var elevator = CreateElevator(id: "E1");
        ElevatorController controller = new ElevatorController([elevator], new NearestElevatorStrategy(), minFloor: 1, maxFloor: 10, NullLogger<ElevatorController>.Instance);

        var act = async () => await controller.RequestElevatorAsync(11, 1, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidFloorException>(act);
    }

    [Fact]
    public async Task RequestElevatorAsync_Throws_WhenPassengerGroupCannotFitAnyElevator()
    {
        var elevator = CreateElevator(id: "E1");
        ElevatorController controller = new ElevatorController([elevator], new NearestElevatorStrategy(), minFloor: 1, maxFloor: 10, NullLogger<ElevatorController>.Instance);

        var act = async () => await controller.RequestElevatorAsync(5, 11, CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task RequestElevatorAsync_DispatchesSelectedElevator()
    {
        TaskCompletionSource boarded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Mock<IElevator> elevatorMock = new Mock<IElevator>();
        elevatorMock.Setup(e => e.Id).Returns("E1");
        elevatorMock.Setup(e => e.IsAtCapacity).Returns(false);
        elevatorMock.Setup(e => e.PassengerCount).Returns(0);
        elevatorMock.Setup(e => e.MaxCapacity).Returns(10);
        elevatorMock.Setup(e => e.AddPassengers(2)).Callback(() => boarded.TrySetResult());
        elevatorMock.Setup(e => e.MoveToFloorAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        elevatorMock.Setup(e => e.MoveToFloorAsync(5, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IDispatchStrategy> strategyMock = new Mock<IDispatchStrategy>();
        strategyMock
            .Setup(s => s.SelectElevator(It.IsAny<IReadOnlyList<IElevator>>(), 1, 2))
            .Returns(elevatorMock.Object);

        ElevatorController controller =
            new ElevatorController([elevatorMock.Object], strategyMock.Object, minFloor: 1, maxFloor: 10, NullLogger<ElevatorController>.Instance);

        await controller.RequestElevatorAsync(5, 2, CancellationToken.None);
        await boarded.Task.WaitAsync(TimeSpan.FromSeconds(3));

        elevatorMock.Verify(e => e.AddPassengers(2), Times.Once);
        elevatorMock.Verify(e => e.MoveToFloorAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestElevatorAsync_QueuesRequest_AndDispatchesOnceElevatorFrees()
    {
        TaskCompletionSource moveToFloor5 = new TaskCompletionSource();

        Mock<IElevator> elevatorMock = new Mock<IElevator>();
        elevatorMock.Setup(e => e.Id).Returns("E1");
        elevatorMock.Setup(e => e.CurrentFloor).Returns(1);
        elevatorMock.Setup(e => e.IsAtCapacity).Returns(false);
        elevatorMock.Setup(e => e.PassengerCount).Returns(0);
        elevatorMock.Setup(e => e.MaxCapacity).Returns(10);
        elevatorMock.Setup(e => e.AddPassengers(It.IsAny<int>()));
        elevatorMock.Setup(e => e.MoveToFloorAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        elevatorMock.Setup(e => e.MoveToFloorAsync(5, It.IsAny<CancellationToken>()))
            .Returns(moveToFloor5.Task);
        elevatorMock.Setup(e => e.MoveToFloorAsync(7, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ElevatorController controller = new ElevatorController([elevatorMock.Object], new NearestElevatorStrategy(), minFloor: 1, maxFloor: 10, NullLogger<ElevatorController>.Instance);

        await controller.RequestElevatorAsync(5, 1, CancellationToken.None);

        var secondRequestTask = controller.RequestElevatorAsync(7, 1, CancellationToken.None);

        Assert.False(secondRequestTask.IsCompleted);
        moveToFloor5.SetResult();

        await secondRequestTask.WaitAsync(TimeSpan.FromSeconds(4));

        elevatorMock.Verify(e => e.MoveToFloorAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestElevatorAsync_CancelsQueuedRequest_WhenTokenCancelled()
    {
        var elevatorMock = CreateElevator(id: "E1", isAtCapacity: true);

        ElevatorController controller = new ElevatorController([elevatorMock], new NearestElevatorStrategy(), minFloor: 1, maxFloor: 10, NullLogger<ElevatorController>.Instance);

        using CancellationTokenSource cts = new CancellationTokenSource();

        var requestTask = controller.RequestElevatorAsync(5, 1, cts.Token);
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => requestTask);
    }

    [Fact]
    public async Task RequestElevatorAsync_UsesUpdatedStrategy_AfterSetDispatchStrategy()
    {
        var elevator = CreateElevator(id: "E1");

        Mock<IDispatchStrategy> initialStrategy = new Mock<IDispatchStrategy>();
        initialStrategy
            .Setup(s => s.SelectElevator(It.IsAny<IReadOnlyList<IElevator>>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns((IElevator?)null);

        Mock<IDispatchStrategy> newStrategy = new Mock<IDispatchStrategy>();
        newStrategy
            .Setup(s => s.SelectElevator(It.IsAny<IReadOnlyList<IElevator>>(), 1, 1))
            .Returns(elevator);

        ElevatorController controller = new ElevatorController([elevator], initialStrategy.Object, minFloor: 1, maxFloor: 10, NullLogger<ElevatorController>.Instance);
        controller.SetDispatchStrategy(newStrategy.Object);
        await controller.RequestElevatorAsync(5, 1, CancellationToken.None);

        newStrategy.Verify(s => s.SelectElevator(It.IsAny<IReadOnlyList<IElevator>>(), 1, 1), Times.Once);
        initialStrategy.Verify(s => s.SelectElevator(It.IsAny<IReadOnlyList<IElevator>>(), 1, 1), Times.Never);
    }

    [Fact]
    public async Task RequestElevatorAsync_RemovesAllPassengers_WhenElevatorArrives()
    {
        TaskCompletionSource unloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var elevatorMock = new Mock<IElevator>();
        elevatorMock.Setup(e => e.Id).Returns("E1");
        elevatorMock.Setup(e => e.IsAtCapacity).Returns(false);
        elevatorMock.Setup(e => e.PassengerCount).Returns(3);
        elevatorMock.Setup(e => e.MaxCapacity).Returns(10);
        elevatorMock
            .Setup(e => e.RemovePassengers(3))
            .Callback(() => unloaded.TrySetResult());
        elevatorMock
            .Setup(e => e.MoveToFloorAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        elevatorMock
            .Setup(e => e.MoveToFloorAsync(5, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new ElevatorController(
            [elevatorMock.Object],
            new NearestElevatorStrategy(),
            minFloor: 1,
            maxFloor: 10,
            NullLogger<ElevatorController>.Instance);

        await controller.RequestElevatorAsync(5, 3, CancellationToken.None);
        await unloaded.Task.WaitAsync(TimeSpan.FromSeconds(5));

        elevatorMock.Verify(e => e.RemovePassengers(3), Times.Once);
    }

    [Fact]
    public async Task RequestElevatorAsync_DropsOffPassengers_AndAllowsSameElevatorToMoveAgain()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 1);
        var controller = new ElevatorController(
            [elevator],
            new NearestElevatorStrategy(),
            minFloor: 1,
            maxFloor: 10,
            NullLogger<ElevatorController>.Instance);

        await controller.RequestElevatorAsync(3, 2, CancellationToken.None);
        await Task.Delay(2500);

        await controller.RequestElevatorAsync(5, 1, CancellationToken.None);
        await Task.Delay(2500);

        Assert.Equal(5, elevator.CurrentFloor);
        Assert.Equal(0, elevator.PassengerCount);
    }

    [Fact]
    public void MarkElevatorOutOfService_UpdatesElevatorState()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 1);
        var controller = new ElevatorController(
            [elevator],
            new NearestElevatorStrategy(),
            minFloor: 1,
            maxFloor: 10,
            NullLogger<ElevatorController>.Instance);

        controller.MarkElevatorOutOfService("E1");

        Assert.Equal(ElevatorSim.Domain.Enums.ElevatorState.OutOfService, elevator.State);
    }

    [Fact]
    public void ReturnElevatorToService_UpdatesElevatorState()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 1);
        var controller = new ElevatorController(
            [elevator],
            new NearestElevatorStrategy(),
            minFloor: 1,
            maxFloor: 10,
            NullLogger<ElevatorController>.Instance);

        controller.MarkElevatorOutOfService("E1");
        controller.ReturnElevatorToService("E1");

        Assert.Equal(ElevatorSim.Domain.Enums.ElevatorState.Idle, elevator.State);
    }
}
