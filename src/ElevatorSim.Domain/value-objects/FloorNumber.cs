using ElevatorSim.Domain.Exceptions;

namespace ElevatorSim.Domain.ValueObjects;

public readonly record struct FloorNumber
{
    public int Value { get; }

    public FloorNumber(int value, int minFloor, int maxFloor)
    {
        if(value < minFloor || value > maxFloor)
        {
            throw new InvalidFloorException(value, minFloor, maxFloor);
        }
        
        Value = value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}