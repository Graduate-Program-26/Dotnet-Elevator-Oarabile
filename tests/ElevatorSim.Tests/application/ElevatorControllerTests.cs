using ElevatorSim.Application.Controllers;
using ElevatorSim.Application.Strategies;
using ElevatorSim.Domain.Exceptions;
using ElevatorSim.Domain.Interfaces;
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
        mock.Setup(e => e.MoveToFloorAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return mock.Object;
    }

    [Fact]
    public async Task RequestElevatorAsync_Throws_WhenFloorIsBelowMinimum()
    {
        var elevator = CreateElevator(id: "E1");
        var controller = new ElevatorController([elevator], new NearestElevatorStrategy(), minFloor: 1, maxFloor: 10);

        var act = async () => await controller.RequestElevatorAsync(0, 1, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidFloorException>(act);
    }

    [Fact]
    public async Task RequestElevatorAsync_Throws_WhenFloorIsAboveMaximum()
    {
        var elevator = CreateElevator(id: "E1");
        var controller = new ElevatorController([elevator], new NearestElevatorStrategy(), minFloor: 1, maxFloor: 10);

        var act = async () => await controller.RequestElevatorAsync(11, 1, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidFloorException>(act);
    }

    [Fact]
    public async Task RequestElevatorAsync_DispatchesSelectedElevator()
    {
        Mock<IElevator> elevatorMock = new Mock<IElevator>();
        elevatorMock.Setup(e => e.Id).Returns("E1");
        elevatorMock.Setup(e => e.IsAtCapacity).Returns(false);
        elevatorMock.Setup(e => e.MoveToFloorAsync(5, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IDispatchStrategy> strategyMock = new Mock<IDispatchStrategy>();
        strategyMock
            .Setup(s => s.SelectElevator(It.IsAny<IReadOnlyList<IElevator>>(), 5, 2))
            .Returns(elevatorMock.Object);

        ElevatorController controller =
            new ElevatorController([elevatorMock.Object], strategyMock.Object, minFloor: 1, maxFloor: 10);

        await controller.RequestElevatorAsync(5, 2, CancellationToken.None);

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
        elevatorMock.Setup(e => e.AddPassengers(It.IsAny<int>()));
        elevatorMock.Setup(e => e.MoveToFloorAsync(5, It.IsAny<CancellationToken>()))
            .Returns(moveToFloor5.Task);
        elevatorMock.Setup(e => e.MoveToFloorAsync(7, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ElevatorController controller = new ElevatorController([elevatorMock.Object], new NearestElevatorStrategy(), minFloor: 1, maxFloor: 10);

        await controller.RequestElevatorAsync(5, 1, CancellationToken.None);

        var secondRequestTask = controller.RequestElevatorAsync(7, 1, CancellationToken.None);

        Assert.False(secondRequestTask.IsCompleted);
        moveToFloor5.SetResult();

        await secondRequestTask.WaitAsync(TimeSpan.FromSeconds(2));

        elevatorMock.Verify(e => e.MoveToFloorAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestElevatorAsync_CancelsQueuedRequest_WhenTokenCancelled()
    {
        var elevatorMock = CreateElevator(id: "E1", isAtCapacity: true);

        ElevatorController controller = new ElevatorController([elevatorMock], new NearestElevatorStrategy(), minFloor: 1, maxFloor: 10);

        using CancellationTokenSource cts = new CancellationTokenSource(); 

        var requestTask = controller.RequestElevatorAsync(5, 1, cts.Token);
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => requestTask);
    }
}