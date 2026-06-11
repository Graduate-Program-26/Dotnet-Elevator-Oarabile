namespace ElevatorSim.Domain.Interfaces;

public interface IDispatchStrategy
{
    IElevator ? SelectElevator(IReadOnlyList<IElevator> elevators, int requestedFloor, int passengerCount);
}