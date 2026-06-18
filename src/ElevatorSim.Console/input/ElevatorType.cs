using ElevatorSim.Domain.Enums;

namespace ElevatorSim.Console.Input;

public sealed class ElevatorTypePrompt
{
    private static ElevatorType PromptForSingleType(int elevatorNumber)
    {
        while (true)
        {
            System.Console.Write($"Elevator {elevatorNumber} type (1/2/3): ");
            var input = System.Console.ReadLine();

            switch (input)
            {
                case "1":
                    return ElevatorType.Passanger;
                case "2":
                    return ElevatorType.HighSpeed;
                case "3":
                    return ElevatorType.Freight;
                default:
                    System.Console.WriteLine("  Please enter 1, 2, or 3.\n");
                    break;
            }
        }
    }
    /// <summary>
    /// Prompts the user to select an <see cref="ElevatorType"/> for each elevator
    /// being created and returns the selected types.
    /// </summary>
    /// <param name="elevatorCount"></param>
    /// <returns>
    /// A list of <see cref="ElevatorType"/> values representing the user's
    /// selection for each elevator. 
    /// </returns>

    public List<ElevatorType> PromptForTypes(int elevatorCount)
    {
        var types = new List<ElevatorType>();

        System.Console.WriteLine("Now let's choose a type for each elevator.");
        System.Console.WriteLine("Options: 1) Passenger  2) HighSpeed  3) Freight\n");

        for (var i = 1; i <= elevatorCount; i++)
        {
            types.Add(PromptForSingleType(i));
        }

        return types;
    }
}