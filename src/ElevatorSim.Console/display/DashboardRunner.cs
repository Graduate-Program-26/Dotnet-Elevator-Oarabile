using ElevatorSim.Domain.Interfaces;
using Spectre.Console;

namespace ElevatorSim.Console.Display;
public sealed class DashboardRunner
{
    public const int RefreshIntervalMilliseconds = 200;
    private readonly StatusBoard _statusBoard;

    public DashboardRunner(StatusBoard statusBoard)
    {
        _statusBoard = statusBoard;
    }

    public async Task RunAsync(IReadOnlyList<IElevator> elevators, CancellationToken cancellationToken)
    {
        await AnsiConsole
            .Live(_statusBoard.BuildView(elevators))
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ctx.UpdateTarget(_statusBoard.BuildView(elevators));
                    ctx.Refresh();

                    try
                    {
                        await Task.Delay(RefreshIntervalMilliseconds, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            });
    }
}