using ElevatorSim.Domain.Enums;

namespace ElevatorSim.Domain.Interfaces;

public interface IElevator
{
    string Id { get; }
    ElevatorType Type { get; }
    int CurrentFloor { get; }
    ElevatorDirection Direction { get; }
    ElevatorState State { get; }
    int PassengerCount { get; }
    int MaxCapacity { get; }
    double SecondsPerFloor { get; }
    bool IsAtCapacity { get; }
    Task MoveToFloorAsync(int destinatonFloor, CancellationToken cancellationToken);
    void AddPassengers(int count);
    void RemovePassengers(int count);
}