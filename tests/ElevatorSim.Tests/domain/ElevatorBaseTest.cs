using ElevatorSim.Domain.Enums;
using ElevatorSim.Domain.Exceptions;
using ElevatorSim.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace ElevatorSim.Tests.Domain;

public class ElevatorBaseTests
{
    [Fact]
    public void Elevator_StartsIdle_AtStartingFloor()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 1);

        elevator.CurrentFloor.Should().Be(1);
        elevator.State.Should().Be(ElevatorState.Idle);
        elevator.Direction.Should().Be(ElevatorDirection.Idle);
        elevator.PassengerCount.Should().Be(0);
        elevator.IsAtCapacity.Should().BeFalse();
    }

    [Fact]
    public void AddPassengers_IncreasesPassengerCount()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 1);

        elevator.AddPassengers(4); 

        elevator.PassengerCount.Should().Be(4);
    }

    [Fact]
    public void AddPassengers_ThrowsWhenAtCapacity()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 5, secondsPerFloor: 0.01, startingFloor: 1);
        elevator.AddPassengers(5);

        var act = () => elevator.AddPassengers(1);

        act.Should().Throw<CapacityExceededException>()
            .WithMessage("*E1*");
    }

    [Fact]
    public void AddPassengers_Throws_WhenCountIsNegative()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 1);

        var act = () => elevator.AddPassengers(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RemovePassengers_DecreasesPassengerCount()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 1);
        elevator.AddPassengers(5);

        elevator.RemovePassengers(2);

        elevator.PassengerCount.Should().Be(3);
    }

    [Fact]
    public void RemovePassengers_DoesNotGoBelowZero()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 1);
        elevator.AddPassengers(2);

        elevator.RemovePassengers(10);

        elevator.PassengerCount.Should().Be(0);
    }

    [Fact]
    public async Task MoveToFloorAsync_UpdatesCurrentFloor_AndDirection()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 1);

        await elevator.MoveToFloorAsync(3, CancellationToken.None);

        elevator.CurrentFloor.Should().Be(3);
        elevator.State.Should().Be(ElevatorState.DoorsOpen);
        elevator.Direction.Should().Be(ElevatorDirection.Idle);
    }

    [Fact]
    public async Task MoveToFloorAsync_InvokesOnFloorChanged_OncePerFloor()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 1);

        await elevator.MoveToFloorAsync(4, CancellationToken.None);

        elevator.FloorChangedCount.Should().Be(3);
    }

    [Fact]
    public async Task MoveToFloorAsync_InvokesOnArrived_Once()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 1);

        await elevator.MoveToFloorAsync(3, CancellationToken.None);

        elevator.ArrivedCount.Should().Be(1);
    }

    [Fact]
    public async Task MoveToFloorAsync_DoesNothing_WhenAlreadyAtDestinationFloor()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 5);

        await elevator.MoveToFloorAsync(5, CancellationToken.None);

        elevator.FloorChangedCount.Should().Be(0);
        elevator.ArrivedCount.Should().Be(0);
        elevator.State.Should().Be(ElevatorState.Idle);
    }

    [Fact]
    public async Task MoveToFloorAsync_Throws_WhenDoorsAreOpen()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 0.01, startingFloor: 1);
        await elevator.MoveToFloorAsync(3, CancellationToken.None);

        elevator.State.Should().Be(ElevatorState.DoorsOpen);

        var act = async () => await elevator.MoveToFloorAsync(5, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MoveToFloorAsync_CanBeCancelled()
    {
        TestElevator elevator = new TestElevator("E1", maxCapacity: 10, secondsPerFloor: 1, startingFloor: 1);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await elevator.MoveToFloorAsync(5, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}