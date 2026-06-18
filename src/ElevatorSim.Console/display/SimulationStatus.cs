namespace ElevatorSim.Console.Display;

public sealed class SimulationStatus
{
    private readonly object _lock = new();
    private string _strategyName = "Normal";
    private bool _autopilotEnabled;
    private bool _emergencyModeEnabled;
    private int _requestsPerMinute;
    private int _pendingRequests;
    private int _busyElevators;
    private string _currentInput = string.Empty;

    public void UpdateStrategy(string strategyName)
    {
        lock (_lock)
        {
            _strategyName = strategyName;
        }
    }

    public void UpdateAutopilot(bool enabled, int requestsPerMinute)
    {
        lock (_lock)
        {
            _autopilotEnabled = enabled;
            _requestsPerMinute = requestsPerMinute;
        }
    }

    public void UpdateLoad(int pendingRequests, int busyElevators)
    {
        lock (_lock)
        {
            _pendingRequests = pendingRequests;
            _busyElevators = busyElevators;
        }
    }

    public void UpdateEmergencyMode(bool enabled)
    {
        lock (_lock)
        {
            _emergencyModeEnabled = enabled;
        }
    }

    public void UpdateCurrentInput(string currentInput)
    {
        lock (_lock)
        {
            _currentInput = currentInput;
        }
    }

    public SimulationStatusSnapshot Snapshot()
    {
        lock (_lock)
        {
            return new SimulationStatusSnapshot(
                _strategyName,
                _autopilotEnabled,
                _emergencyModeEnabled,
                _requestsPerMinute,
                _pendingRequests,
                _busyElevators,
                _currentInput);
        }
    }
}

public sealed record SimulationStatusSnapshot(
    string StrategyName,
    bool AutopilotEnabled,
    bool EmergencyModeEnabled,
    int RequestsPerMinute,
    int PendingRequests,
    int BusyElevators,
    string CurrentInput);
