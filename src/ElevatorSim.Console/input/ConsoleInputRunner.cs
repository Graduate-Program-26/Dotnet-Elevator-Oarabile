using ElevatorSim.Application.Controllers;
using ElevatorSim.Console.Display;
using ElevatorSim.Domain.Exceptions;
using System.Text;

namespace ElevatorSim.Console.Input;

public sealed class ConsoleInputRunner
{
    /// <summary>
    /// Continuously processes user input for the elevator simulation, handling
    /// simulation control commands and elevator requests until the simulation
    /// is cancelled or the user exits.
    /// </summary>
    /// <param name="controller">The elevator controller that processes elevator requests.</param>
    /// <param name="commandParser">Parses user input into elevator commands.</param>
    /// <param name="autopilotRunner">Manages automatic request generation.</param>
    /// <param name="adaptiveStrategyRunner">Manages adaptive dispatch strategy behavior.</param>
    /// <param name="simulationStatus">Tracks the current state of the simulation.</param>
    /// <param name="statusBoard">Displays status messages and user feedback.</param>
    /// <param name="simulationCts">Controls cancellation of the simulation loop.</param>
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
            if (!System.Console.KeyAvailable)
            {
                Thread.Sleep(25);
                continue;
            }

            var key = System.Console.ReadKey(intercept: true);

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

    /// <summary>
    /// Processes simulation control commands entered by the user, such as
    /// enabling or disabling autopilot, adjusting the request rate, toggling
    /// emergency mode, and managing elevator service status.
    /// </summary>
    /// <param name="input">The raw command entered by the user.</param>
    /// <param name="controller">The elevator controller that manages elevator state.</param>
    /// <param name="autopilotRunner">The autopilot manager for generating automatic requests.</param>
    /// <param name="adaptiveStrategyRunner">The component responsible for adaptive dispatch strategy behavior.</param>
    /// <param name="simulationStatus">The simulation status model used to update the dashboard.</param>
    /// <param name="statusBoard">The status board used to log user-facing messages.</param>
    /// <returns>
    /// <c>true</c> if the input was recognized and handled as a control command;
    /// otherwise, <c>false</c>.
    /// </returns>
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
