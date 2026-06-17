using ElevatorSim.Domain.Enums;
using ElevatorSim.Domain.Interfaces;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ElevatorSim.Console.Display;

/// <summary>
/// Renders a live-updating view of elevator statuses alongside a recent activity log.
/// </summary>
public sealed class StatusBoard
{
    /// <summary>The maximum number of recent activity messages retained for display.</summary>
    public const int MaxActivityMessages = 8;

    private readonly Queue<string> _activityLog = new();
    private readonly object _activityLock = new();
    private readonly SimulationStatus _simulationStatus;

    public StatusBoard(SimulationStatus simulationStatus)
    {
        _simulationStatus = simulationStatus;
    }

    /// <summary>
    /// Adds a message to the activity log shown beneath the elevator table.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public void LogActivity(string message)
    {
        lock (_activityLock)
        {
            _activityLog.Enqueue(message);

            while (_activityLog.Count > MaxActivityMessages)
            {
                _activityLog.Dequeue();
            }
        }
    }

    /// <summary>
    /// Builds the full renderable view: elevator status table plus recent activity log.
    /// </summary>
    /// <param name="elevators">The elevators to display.</param>
    /// <returns>A renderable combining the table and activity log.</returns>
    public IRenderable BuildView(IReadOnlyList<IElevator> elevators)
    {
        var summary = BuildSummaryPanel();
        var table = BuildTable(elevators);
        var activityPanel = BuildActivityPanel();

        var rows = new Rows(summary, table, activityPanel);
        return rows;
    }

    private Panel BuildSummaryPanel()
    {
        var status = _simulationStatus.Snapshot();
        var autopilot = status.AutopilotEnabled
            ? $"[green]On[/] ({status.RequestsPerMinute}/min)"
            : "[grey]Off[/]";

        var content =
            $"Strategy: [bold yellow]{FormatStrategyName(status.StrategyName)}[/]   " +
            $"Autopilot: {autopilot}   " +
            $"Emergency: {(status.EmergencyModeEnabled ? "[red]On[/]" : "[grey]Off[/]")}   " +
            $"Pending: [bold]{status.PendingRequests}[/]   " +
            $"Busy: [bold]{status.BusyElevators}[/]";

        return new Panel(content)
            .Header("[bold]Simulation[/]")
            .Expand();
    }

    private Table BuildTable(IReadOnlyList<IElevator> elevators)
    {
        var table = new Table().Title("[bold]Elevator Status[/]");

        table.AddColumn("Elevator");
        table.AddColumn("Type");
        table.AddColumn("Floor");
        table.AddColumn("Direction");
        table.AddColumn("State");
        table.AddColumn("Passengers");

        foreach (var elevator in elevators)
        {
            table.AddRow(
                elevator.Id,
                elevator.Type.ToString(),
                elevator.CurrentFloor.ToString(),
                FormatDirection(elevator.Direction),
                FormatState(elevator.State),
                $"{elevator.PassengerCount}/{elevator.MaxCapacity}");
        }

        return table;
    }

    private Panel BuildActivityPanel()
    {
        string content;

        lock (_activityLock)
        {
            content = _activityLog.Count == 0
                ? "[grey](no activity yet)[/]"
                : string.Join("\n", _activityLog);
        }

        return new Panel(content)
            .Header("[bold]Recent Activity[/]")
            .Expand();
    }

    private static string FormatDirection(ElevatorDirection direction)
    {
        return direction switch
        {
            ElevatorDirection.Up => "[green]▲ Up[/]",
            ElevatorDirection.Down => "[red]▼ Down[/]",
            ElevatorDirection.Idle => "[grey]— Idle[/]",
            _ => direction.ToString(),
        };
    }

    private static string FormatState(ElevatorState state)
    {
        return state switch
        {
            ElevatorState.Moving => "[yellow]Moving[/]",
            ElevatorState.DoorsOpen => "[blue]Doors Open[/]",
            ElevatorState.Boarding => "[blue]Boarding[/]",
            ElevatorState.OutOfService => "[red]Out of Service[/]",
            ElevatorState.Idle => "[grey]Idle[/]",
            _ => state.ToString(),
        };
    }

    private static string FormatStrategyName(string strategyName)
    {
        return strategyName
            .Replace("Strategy", string.Empty, StringComparison.Ordinal)
            .Replace("NearestElevator", "Normal", StringComparison.Ordinal)
            .Replace("Emergencystrategy", "Emergency", StringComparison.Ordinal)
            .Replace("MorningPeak", "Morning Peak", StringComparison.Ordinal);
    }
}
