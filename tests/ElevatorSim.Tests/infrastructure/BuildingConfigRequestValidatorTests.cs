using ElevatorSim.Application.Models;
using ElevatorSim.Infrastructure.Validation;
using Xunit;

namespace ElevatorSim.Tests.Infrastructure;

public class BuildingConfigRequestValidatorTests
{
    private readonly BuildingConfigRequestValidator _validator = new();

    [Fact]
    public void Validate_Succeeds_WhenValuesAreWithinRange()
    {
        var request = new BuildingConfigRequest(FloorCount: 10, ElevatorCount: 3);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(201)]
    public void Validate_Fails_WhenFloorCountIsOutOfRange(int floorCount)
    {
        var request = new BuildingConfigRequest(FloorCount: floorCount, ElevatorCount: 3);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(BuildingConfigRequest.FloorCount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Validate_Fails_WhenElevatorCountIsOutOfRange(int elevatorCount)
    {
        var request = new BuildingConfigRequest(FloorCount: 10, ElevatorCount: elevatorCount);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(BuildingConfigRequest.ElevatorCount));
    }
}