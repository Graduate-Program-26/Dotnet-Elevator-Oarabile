namespace ElevatorSim.Domain.Exceptions;

public class CapacityExceededException : ElevatorSimException
{
    public CapacityExceededException(
        string elevatorId,
        int currentPassengers,
        int maxCapacity,
        int incomingPassengers)
        : base(
            $"Elevator {elevatorId} cannot accept {incomingPassengers} passenger(s). " +
            $"Current load is {currentPassengers}/{maxCapacity}.") { }
}