using ElevatorSim.Application.Models;
using FluentValidation;

namespace ElevatorSim.Infrastructure.Validation;

public sealed class BuildingConfigRequestValidator : AbstractValidator<BuildingConfigRequest>
{
    public const int MinFloorCount = 2;

    public const int MaxFloorCount = 200;

    public const int MinElevatorCount = 1;

    public const int MaxElevatorCount = 20;

    public BuildingConfigRequestValidator()
    {
        RuleFor(x => x.FloorCount)
            .InclusiveBetween(MinFloorCount, MaxFloorCount)
            .WithMessage($"Floor count must be between {MinFloorCount} and {MaxFloorCount}.");

        RuleFor(x => x.ElevatorCount)
            .InclusiveBetween(MinElevatorCount, MaxElevatorCount)
            .WithMessage($"Elevator count must be between {MinElevatorCount} and {MaxElevatorCount}.");
    }
}