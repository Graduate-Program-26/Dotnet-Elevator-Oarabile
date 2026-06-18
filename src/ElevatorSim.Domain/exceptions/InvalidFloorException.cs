namespace ElevatorSim.Domain.Exceptions;

public class InvalidFloorException : ElevatorSimException
{
    public InvalidFloorException(int floorNumber, int minFloor, int maxFloor)
        : base($"Floor {floorNumber} is invalid. Valid floors are between {minFloor} and {maxFloor}.") { }
}