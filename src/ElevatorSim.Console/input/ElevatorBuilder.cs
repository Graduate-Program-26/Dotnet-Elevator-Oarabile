using ElevatorSim.Domain.Enums;
using ElevatorSim.Domain.Interfaces;

namespace ElevatorSim.Console.Input;

public sealed class ElevatorBuilder
{
    private readonly IElevatorFactory _factory;

    public ElevatorBuilder(IElevatorFactory factory)
    {
        _factory = factory;
    }

    public List<IElevator> CreateElevators(IReadOnlyList<ElevatorType> types)
    {
        var elevators = new List<IElevator>();

        for (var i = 0; i < types.Count; i++)
        {
            var id = $"E{i + 1}";
            elevators.Add(_factory.Create(types[i], id, startingFloor: 1));
        }

        return elevators;
    }
}
