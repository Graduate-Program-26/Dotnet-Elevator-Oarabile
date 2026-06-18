# ElevatorSim

ElevatorSim is a .NET console elevator simulation. It lets you configure a building, choose elevator types, send passenger requests, watch a live dashboard, enable autopilot traffic, switch strategies automatically, mark elevators out of service, and simulate emergency evacuation behavior.

## Features

- Configure the number of floors and elevators at startup.
- Choose each elevator type:
  - Passenger elevator: balanced capacity and speed.
  - High-speed elevator: faster but lower capacity.
  - Freight elevator: higher capacity but slower.
- Live Spectre.Console dashboard showing:
  - elevator id
  - elevator type
  - current floor
  - direction
  - state
  - passenger count
  - current dispatch strategy
  - autopilot status
  - emergency mode status
  - pending request count
  - busy elevator count
  - current typed input
- Passenger requests support pickup and destination floors.
- Elevators travel to the pickup floor, board passengers, travel to the destination floor, and unload.
- Strategies skip elevators that cannot fit the full passenger group.
- Adaptive strategy switching based on traffic load and emergency mode.
- Autopilot mode generates realistic request traffic.
- Emergency mode simulates evacuation traffic from upper floors to the lobby.
- Elevators can be marked out of service and returned to service.

## Requirements

- .NET SDK that supports `net10.0`.
- A terminal that supports interactive console rendering.

The project treats warnings as errors in the main projects, so compile warnings should be fixed rather than ignored.

## Project Structure

```text
src/
  ElevatorSim.Domain/          Core entities, enums, value objects, exceptions, interfaces
  ElevatorSim.Application/     Controller, factories, dispatch strategies, DI setup
  ElevatorSim.Infrastructure/  Validation and infrastructure DI setup
  ElevatorSim.Console/         Console UI, dashboard, input handling, autopilot runner

tests/
  ElevatorSim.Tests/           xUnit tests for domain, application, and infrastructure behavior
```

## Running the App

From the repository root:

```bash
dotnet restore
dotnet run --project src/ElevatorSim.Console/ElevatorSim.Console.csproj
```

At startup, the app asks for:

1. Number of floors.
2. Number of elevators.
3. Type for each elevator.

Elevator type options:

```text
1 = Passenger
2 = HighSpeed
3 = Freight
```

After setup, the live dashboard starts.

## Console Commands

### Passenger Requests

Send passengers from one floor to another:

```text
from,to,passengers
```

Example:

```text
7,1,5
```

This means 5 passengers are waiting on floor 7 and want to go to floor 1.

Shortcut from lobby:

```text
to,passengers
```

Example:

```text
8,3
```

This means 3 passengers are waiting on lobby floor 1 and want to go to floor 8.

### Autopilot

Enable generated traffic:

```text
auto on
```

Disable generated traffic:

```text
auto off
```

Set generated request volume:

```text
rate 20
```

The number is requests per minute. The app clamps the value to a supported range.

### Elevator Service State

Mark an elevator out of service:

```text
service E1 out
```

Return an elevator to service:

```text
service E1 in
```

Out-of-service elevators are shown on the dashboard and skipped by dispatch strategies.

### Emergency Simulation

Enable emergency mode:

```text
emergency on
```

Disable emergency mode:

```text
emergency off
```

Emergency mode forces the emergency dispatch strategy, enables autopilot, increases request volume, and generates evacuation-style traffic from upper floors down to the lobby.

### Exit

Stop the simulation:

```text
exit
```

## Dispatch Strategies

The app can switch strategies automatically while running.

### Normal

Uses `NearestElevatorStrategy`.

Chooses the closest available elevator to the passenger pickup floor.

### Morning Peak

Uses `MorningPeakStrategy`.

Prioritizes elevators that are already moving upward or positioned well for lobby-to-upper-floor traffic. This strategy is selected automatically when autopilot volume is high.

### Emergency

Uses `EmergencyStrategy`.

Prioritizes the elevator with the fastest estimated time to reach the pickup floor and skips out-of-service elevators. This strategy is selected when emergency mode is enabled or when traffic is heavily congested.

### Capacity Rules

All strategies use shared capacity rules:

- skip elevators already at capacity
- skip elevators that cannot fit the full passenger group
- skip out-of-service elevators

If no elevator in the building can fit the passenger group, the controller rejects the request.

## Elevator States

Elevators can show these states:

```text
Idle
Moving
DoorsOpen
Boarding
OutOfService
```

During a normal trip:

1. Elevator moves to pickup floor.
2. Elevator enters `Boarding`.
3. Passengers board.
4. Elevator moves to destination floor.
5. Elevator enters `Boarding` while passengers unload.
6. Elevator returns to `Idle`.

## Running Tests

From the repository root:

```bash
dotnet test
```

Run a specific test project:

```bash
dotnet test tests/ElevatorSim.Tests/ElevatorSim.Tests.csproj
```

The test project uses:

- xUnit
- FluentAssertions
- Moq
- coverlet.collector

## Development Notes

- Console-specific services are registered in `src/ElevatorSim.Console/extensions/ServiceCollectionsExtensions.cs`.
- `Program.cs` should stay thin: setup services, collect startup configuration, and hand off to `ElevatorSimulationRunner`.
- `ConsoleInputRunner` handles runtime commands and keeps typed input visible inside the live dashboard.
- `ElevatorSimulationRunner` coordinates dashboard, autopilot, adaptive strategy updates, input, and status refresh tasks.
- `ElevatorController` owns dispatching, queueing, pickup/drop-off lifecycle, and elevator service state changes.
- Strategy implementations live under `src/ElevatorSim.Application/strategies/`.

## Useful Commands

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/ElevatorSim.Console/ElevatorSim.Console.csproj
```

## Troubleshooting

If `dotnet` is not found, install a compatible .NET SDK and verify:

```bash
dotnet --info
```

If the dashboard appears to overwrite typed text, use the `Input:` line in the dashboard. Runtime input is intentionally rendered there while the live dashboard refreshes.
