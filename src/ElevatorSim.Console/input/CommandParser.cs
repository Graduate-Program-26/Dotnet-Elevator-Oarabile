namespace ElevatorSim.Console.Input;

public sealed record ElevatorRequestCommand(int PickupFloor, int DestinationFloor, int PassengerCount);

public sealed class CommandParser
{
    public bool TryParse(string? input, out ElevatorRequestCommand? command)
    {
        command = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var parts = input.Split(',', StringSplitOptions.TrimEntries);

        if (parts.Length is not (2 or 3))
        {
            return false;
        }

        if (parts.Length == 2)
        {
            if (!int.TryParse(parts[0], out var destinationFloor) || !int.TryParse(parts[1], out var passengerCount))
            {
                return false;
            }

            command = new ElevatorRequestCommand(1, destinationFloor, passengerCount);
            return true;
        }

        if (!int.TryParse(parts[0], out var pickupFloor)
            || !int.TryParse(parts[1], out var destination)
            || !int.TryParse(parts[2], out var count))
        {
            return false;
        }

        command = new ElevatorRequestCommand(pickupFloor, destination, count);
        return true;
    }
}
