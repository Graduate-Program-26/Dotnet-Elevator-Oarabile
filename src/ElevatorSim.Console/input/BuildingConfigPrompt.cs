using ElevatorSim.Application.Models;
using FluentValidation;


namespace ElevatorSim.Console.Input;

public sealed class BuildingConfigPrompt
{
    private readonly IValidator<BuildingConfigRequest> _validator;

    public BuildingConfigPrompt(IValidator<BuildingConfigRequest> validator)
    {
        _validator = validator;
    }

    public BuildingConfigRequest Prompt()
    {
        while (true)
        {
            int floorCount = PromptForInt("How many floors does the building have? ");
            int elevatorCount = PromptForInt("How many elevators does the building have? ");

            BuildingConfigRequest request = new BuildingConfigRequest(floorCount, elevatorCount);
            var result = _validator.Validate(request);

            if (result.IsValid)
            {
                return request;
            }

            global::System.Console.WriteLine();
            foreach (var error in result.Errors)
            {
                global::System.Console.WriteLine($"{error.ErrorMessage}");
            }
            global::System.Console.WriteLine();
        }
    }

    private static int PromptForInt(string message)
    {
        while (true)
        {
            global::System.Console.Write(message);
            var input = global::System.Console.ReadLine();

            if (int.TryParse(input, out var value))
            {
                return value;
            }

            global::System.Console.WriteLine("Please enter a valid whole number.\n");
        }
    }
}