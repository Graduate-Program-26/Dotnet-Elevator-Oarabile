using ElevatorSim.Domain.Enums;

namespace ElevatorSim.Domain.Interfaces;

public interface IElevatorFactory
{
    IElevator Create(ElevatorType type, string id, int startingFloor);
}