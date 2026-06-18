using ElevatorSim.Domain.Enums;

namespace ElevatorSim.Console.Input;

public sealed class ElevatorTypePrompt
{
    private static ElevatorType PromptForSingleType(int elevatorNumber)
    {
        while (true)
        {
            global::System.Console.Write($"Elevator {elevatorNumber} type (1/2/3): ");
            var input = global::System.Console.ReadLine();

            switch (input)
            {
                case "1":
                    return ElevatorType.Passanger;
                case "2":
                    return ElevatorType.HighSpeed;
                case "3":
                    return ElevatorType.Freight;
                default:
                    global::System.Console.WriteLine("  Please enter 1, 2, or 3.\n");
                    break;
            }
        }
    }
    public List<ElevatorType> PromptForTypes(int elevatorCount)
    {
        var types = new List<ElevatorType>();

        global::System.Console.WriteLine("Now let's choose a type for each elevator.");
        global::System.Console.WriteLine("Options: 1) Passenger  2) HighSpeed  3) Freight\n");

        for (var i = 1; i <= elevatorCount; i++)
        {
            types.Add(PromptForSingleType(i));
        }

        return types;
    }
}