using ElevatorSim.Application.Controllers;
using ElevatorSim.Console.Display;
using ElevatorSim.Domain.Exceptions;
using System.Text;

namespace ElevatorSim.Console.Input;

public sealed class ConsoleInputRunner
{
    public void RunInputLoop(
    ElevatorController controller,
    CommandParser commandParser,
    AutopilotRunner autopilotRunner,
    AdaptiveStrategyRunner adaptiveStrategyRunner,
    SimulationStatus simulationStatus,
    StatusBoard statusBoard,
    CancellationTokenSource simulationCts)
    {
        while (!simulationCts.IsCancellationRequested)
        {
            var input = ReadVisibleLine(simulationStatus, simulationCts.Token);
            if (input is null)
            {
                return;
            }

            if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
            {
                simulationCts.Cancel();
                return;
            }

            if (TryHandleControlCommand(input, controller, autopilotRunner, adaptiveStrategyRunner, simulationStatus, statusBoard))
            {
                continue;
            }

            if (!commandParser.TryParse(input, out var command))
            {
                statusBoard.LogActivity("[red] Invalid format. Use 'from,to,passengers' or 'to,passengers'.[/]");
                continue;
            }

            try
            {
                var requestTask = controller.RequestElevatorAsync(
                    command!.PickupFloor,
                    command.DestinationFloor,
                    command.PassengerCount,
                    simulationCts.Token);
                _ = requestTask.ContinueWith(
                    task =>
                    {
                        if (task.IsFaulted && task.Exception?.GetBaseException() is Exception ex)
                        {
                            statusBoard.LogActivity($"[red] Request failed: {ex.Message}[/]");
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

                statusBoard.LogActivity($"[green] Request received: {command.PickupFloor}->{command.DestinationFloor}, {command.PassengerCount} passengers.[/]");
            }
            catch (Exception ex) when (ex is ElevatorSimException or ArgumentOutOfRangeException)
            {
                statusBoard.LogActivity($"[red]✗ {ex.Message}[/]");
            }
        }
    }

    private static string? ReadVisibleLine(SimulationStatus simulationStatus, CancellationToken cancellationToken)
    {
        var input = new StringBuilder();
        simulationStatus.UpdateCurrentInput(string.Empty);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!global::System.Console.KeyAvailable)
            {
                Thread.Sleep(25);
                continue;
            }

            var key = global::System.Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                var line = input.ToString();
                simulationStatus.UpdateCurrentInput(string.Empty);
                return line;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (input.Length > 0)
                {
                    input.Length--;
                    simulationStatus.UpdateCurrentInput(input.ToString());
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                input.Append(key.KeyChar);
                simulationStatus.UpdateCurrentInput(input.ToString());
            }
        }

        return null;
    }

    public static bool TryHandleControlCommand(
    string? input,
    ElevatorController controller,
    AutopilotRunner autopilotRunner,
    AdaptiveStrategyRunner adaptiveStrategyRunner,
    SimulationStatus simulationStatus,
    StatusBoard statusBoard)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var command = parts[0].ToLowerInvariant();

        if (command == "auto" && parts.Length == 2)
        {
            if (parts[1].Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                autopilotRunner.SetEnabled(true);
                simulationStatus.UpdateAutopilot(true, autopilotRunner.RequestsPerMinute);
                statusBoard.LogActivity("[green]Autopilot enabled[/]");
                return true;
            }

            if (parts[1].Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                autopilotRunner.SetEnabled(false);
                simulationStatus.UpdateAutopilot(false, autopilotRunner.RequestsPerMinute);
                statusBoard.LogActivity("[grey]Autopilot disabled[/]");
                return true;
            }
        }

        if (command == "rate" && parts.Length == 2 && int.TryParse(parts[1], out var requestsPerMinute))
        {
            autopilotRunner.SetRequestsPerMinute(requestsPerMinute);
            simulationStatus.UpdateAutopilot(autopilotRunner.Enabled, autopilotRunner.RequestsPerMinute);
            statusBoard.LogActivity($"[green]Autopilot rate set to {autopilotRunner.RequestsPerMinute}/min[/]");
            return true;
        }

        if (command == "service" && parts.Length == 3)
        {
            try
            {
                if (parts[2].Equals("out", StringComparison.OrdinalIgnoreCase))
                {
                    controller.MarkElevatorOutOfService(parts[1]);
                    statusBoard.LogActivity($"[red]{parts[1]} marked out of service[/]");
                    return true;
                }

                if (parts[2].Equals("in", StringComparison.OrdinalIgnoreCase))
                {
                    controller.ReturnElevatorToService(parts[1]);
                    statusBoard.LogActivity($"[green]{parts[1]} returned to service[/]");
                    return true;
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                statusBoard.LogActivity($"[red] {ex.Message}[/]");
                return true;
            }
        }

        if (command == "emergency" && parts.Length == 2)
        {
            if (parts[1].Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                adaptiveStrategyRunner.SetEmergencyMode(true);
                autopilotRunner.SetEmergencyMode(true);
                autopilotRunner.SetEnabled(true);
                autopilotRunner.SetRequestsPerMinute(Math.Max(autopilotRunner.RequestsPerMinute, 24));
                simulationStatus.UpdateEmergencyMode(true);
                simulationStatus.UpdateAutopilot(true, autopilotRunner.RequestsPerMinute);
                statusBoard.LogActivity("[red]Emergency simulation enabled[/]");
                return true;
            }

            if (parts[1].Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                adaptiveStrategyRunner.SetEmergencyMode(false);
                autopilotRunner.SetEmergencyMode(false);
                simulationStatus.UpdateEmergencyMode(false);
                statusBoard.LogActivity("[grey]Emergency simulation cleared[/]");
                return true;
            }
        }

        return false;
    }
}
