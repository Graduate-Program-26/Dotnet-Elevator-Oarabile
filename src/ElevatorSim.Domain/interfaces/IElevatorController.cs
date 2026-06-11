namespace ElevatorSim.Domain.Interfaces;

public interface IElevatorController
{
    IReadOnlyList<IElevator> Elevators {get;}
    Task RequestElevatorAsync(int requestedFloor, int passengerCount, CancellationToken cancellationToken);
}