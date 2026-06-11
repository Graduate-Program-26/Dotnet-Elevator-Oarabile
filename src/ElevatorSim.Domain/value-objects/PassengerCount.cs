using ElevatorSim.Domain.Exceptions;

namespace ElevatorSim.Domain.ValueObjects;

public readonly record struct PassengerCount
{
    public int Value { get; }
    public PassengerCount(int value, int maxCapacity)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Passenger count cannot be negative.");
        }

        if (value > maxCapacity)
        {
            throw new CapacityExceededException(
                elevatorId: "N/A",
                currentPassengers: 0,
                maxCapacity: maxCapacity,
                incomingPassengers: value);
        }

        Value = value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}