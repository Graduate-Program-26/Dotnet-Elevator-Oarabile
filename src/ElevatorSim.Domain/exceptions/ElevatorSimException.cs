namespace ElevatorSim.Domain.Exceptions;

public class ElevatorSimException : Exception
{
    public ElevatorSimException(string message)
        : base(message) { }

    public ElevatorSimException(string message, Exception innerException)
        : base(message, innerException) { }
}

