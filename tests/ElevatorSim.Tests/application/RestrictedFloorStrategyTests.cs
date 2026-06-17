using ElevatorSim.Application.Strategies;
using ElevatorSim.Domain.Interfaces;
using Moq;
using Xunit;

namespace ElevatorSim.Tests.Application;

public class RestrictedFloorStrategyTests
{
    [Fact]
    public void SelectElevator_ReturnsRestrictedElevator_WhenFloorIsAllowed()
    {
        IElevator restrictedElevator = CreateElevator(id: "E3", isAtCapacity: false);
        IElevator otherElevator = CreateElevator(id: "E1", isAtCapacity: false);

        List<IElevator> elevators = new List<IElevator> { otherElevator, restrictedElevator };

        Mock<IDispatchStrategy> fallback = new Mock<IDispatchStrategy>();

        RestrictedFloorStrategy strategy = new RestrictedFloorStrategy(
            restrictedElevatorId: "E3",
            allowedFloors: new HashSet<int> { 1, 2, 3 },
            fallbackStrategy: fallback.Object);

        var result = strategy.SelectElevator(elevators, requestedFloor: 2, passengerCount: 1);

        Assert.Equal("E3", result?.Id);
        fallback.Verify(f => f.SelectElevator(It.IsAny<IReadOnlyList<IElevator>>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void SelectElevator_UsesFallback_WhenFloorIsNotAllowed()
    {
        IElevator restrictedElevator = CreateElevator(id: "E3", isAtCapacity: false);
        IElevator otherElevator = CreateElevator(id: "E1", isAtCapacity: false);

        List<IElevator> elevators = new List<IElevator> { otherElevator, restrictedElevator };

        Mock<IDispatchStrategy> fallback = new Mock<IDispatchStrategy>();
        fallback
            .Setup(f => f.SelectElevator(It.IsAny<IReadOnlyList<IElevator>>(), 5, 1))
            .Returns(otherElevator);

        RestrictedFloorStrategy strategy = new RestrictedFloorStrategy(
            restrictedElevatorId: "E3",
            allowedFloors: new HashSet<int> { 1, 2, 3 },
            fallbackStrategy: fallback.Object);

        var result = strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.Equal("E1", result?.Id);
    }

    [Fact]
    public void SelectElevator_UsesFallback_WhenRestrictedElevatorIsAtCapacity()
    {
        IElevator restrictedElevator = CreateElevator(id: "E3", isAtCapacity: true);
        IElevator otherElevator = CreateElevator(id: "E1", isAtCapacity: false);

        List<IElevator> elevators = new List<IElevator> { otherElevator, restrictedElevator };

        Mock<IDispatchStrategy> fallback = new Mock<IDispatchStrategy>();
        fallback
            .Setup(f => f.SelectElevator(It.IsAny<IReadOnlyList<IElevator>>(), 2, 1))
            .Returns(otherElevator);

        RestrictedFloorStrategy strategy = new RestrictedFloorStrategy(
            restrictedElevatorId: "E3",
            allowedFloors: new HashSet<int> { 1, 2, 3 },
            fallbackStrategy: fallback.Object);

        var result = strategy.SelectElevator(elevators, requestedFloor: 2, passengerCount: 1);

        Assert.Equal("E1", result?.Id);
    }

    [Fact]
    public void SelectElevator_FallbackNeverReceivesRestrictedElevator()
    {
        IElevator restrictedElevator = CreateElevator(id: "E3", isAtCapacity: false);
        IElevator otherElevator = CreateElevator(id: "E1", isAtCapacity: false);

        List<IElevator> elevators = new List<IElevator> { otherElevator, restrictedElevator };

        IReadOnlyList<IElevator>? capturedElevators = null;

        Mock<IDispatchStrategy> fallback = new Mock<IDispatchStrategy>();
        fallback
            .Setup(f => f.SelectElevator(It.IsAny<IReadOnlyList<IElevator>>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<IReadOnlyList<IElevator>, int, int>((els, _, _) => capturedElevators = els)
            .Returns(otherElevator);

        RestrictedFloorStrategy strategy = new RestrictedFloorStrategy(
            restrictedElevatorId: "E3",
            allowedFloors: new HashSet<int> { 1, 2, 3 },
            fallbackStrategy: fallback.Object);

        strategy.SelectElevator(elevators, requestedFloor: 5, passengerCount: 1);

        Assert.NotNull(capturedElevators);
        Assert.DoesNotContain(capturedElevators!, e => e.Id == "E3");
    }

    private static IElevator CreateElevator(string id, bool isAtCapacity)
    {
        var mock = new Mock<IElevator>();
        mock.Setup(e => e.Id).Returns(id);
        mock.Setup(e => e.IsAtCapacity).Returns(isAtCapacity);
        mock.Setup(e => e.PassengerCount).Returns(isAtCapacity ? 10 : 0);
        mock.Setup(e => e.MaxCapacity).Returns(10);
        return mock.Object;
    }
}
