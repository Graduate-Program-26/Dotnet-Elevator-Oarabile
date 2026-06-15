using ElevatorSim.Domain.Entities;
using ElevatorSim.Domain.Enums;
using ElevatorSim.Domain.Interfaces;

namespace ElevatorSim.Application.Factories;

public sealed class ElevatorFactory : IElevatorFactory
{
    public IElevator Create(ElevatorType type, string id, int startingFloor)
    {
        return type switch
        {
            ElevatorType.Passanger => new PassengerElevator(id, startingFloor),
            ElevatorType.HighSpeed => new HighSpeedElevator(id, startingFloor),
            ElevatorType.Freight => new FreightElevator(id, startingFloor),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported elevator type."),
        };
    }
}