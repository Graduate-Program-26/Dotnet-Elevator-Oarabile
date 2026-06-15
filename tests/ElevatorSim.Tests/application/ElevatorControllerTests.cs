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
}