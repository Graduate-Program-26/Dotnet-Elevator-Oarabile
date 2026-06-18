namespace ElevatorSim.Domain.Interfaces;

public interface IElevatorController
{
    IReadOnlyList<IElevator> Elevators {get;}
    Task RequestElevatorAsync(int requestedFloor, int passengerCount, CancellationToken cancellationToken);
    Task RequestElevatorAsync(int pickupFloor, int destinationFloor, int passengerCount, CancellationToken cancellationToken);
    void SetDispatchStrategy(IDispatchStrategy strategy);
    void MarkElevatorOutOfService(string elevatorId);
    void ReturnElevatorToService(string elevatorId);
}
