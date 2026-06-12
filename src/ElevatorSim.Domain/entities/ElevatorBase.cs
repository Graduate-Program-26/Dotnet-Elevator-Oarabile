using ElevatorSim.Domain.Enums;
using ElevatorSim.Domain.Exceptions;
using ElevatorSim.Domain.Interfaces;

namespace ElevatorSim.Domain.Entities;

public abstract class ElevatorBase : IElevator
{
    private int _currentFloor;
    private int _passengerCount;
    private ElevatorDirection _direction;
    private ElevatorState _state;
    public string Id { get; }
    public ElevatorType Type { get; }
    public int CurrentFloor => _currentFloor;
    public ElevatorDirection Direction => _direction;
    public ElevatorState State => _state;
    public int PassengerCount => _passengerCount;
    public int MaxCapacity { get; }
    public double SecondsPerFloor { get; }
    public bool IsAtCapacity => _passengerCount >= MaxCapacity;

    protected ElevatorBase(string id, ElevatorType type, int maxCapacity, double secondsPerFloor, int startingFloor)
    {
        Id = id;
        Type = type;
        MaxCapacity = maxCapacity;
        SecondsPerFloor = secondsPerFloor;
        _currentFloor = startingFloor;
        _direction = ElevatorDirection.Idle;
        _state = ElevatorState.Idle;
    }

    public void AddPassengers(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Passenger count cannot be negative.");
        }

        if (_passengerCount + count > MaxCapacity)
        {
            throw new CapacityExceededException(Id, _passengerCount, MaxCapacity, count);
        }

        _passengerCount += count;
    }

    public void RemovePassengers(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Passenger count cannot be negative.");
        }

        _passengerCount = Math.Max(0, _passengerCount - count);
    }

    public async Task MoveToFloorAsync(int destinationFloor, CancellationToken cancellationToken)
    {
        if (_state == ElevatorState.DoorsOpen || _state == ElevatorState.Boarding)
        {
            throw new InvalidOperationException(
                $"Elevator {Id} cannot move while in state '{_state}'. Doors must be closed first.");
        }

        if (destinationFloor == _currentFloor)
        {
            return;
        }

        _direction = destinationFloor > _currentFloor ? ElevatorDirection.Up : ElevatorDirection.Down;
        _state = ElevatorState.Moving;

        var step = _direction == ElevatorDirection.Up ? 1 : -1;

        while (_currentFloor != destinationFloor)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(TimeSpan.FromSeconds(SecondsPerFloor), cancellationToken);

            _currentFloor += step;

            OnFloorChanged();
        }

        _direction = ElevatorDirection.Idle;
        _state = ElevatorState.DoorsOpen;

        OnArrived();
    }
    protected virtual void OnFloorChanged() { }
    protected virtual void OnArrived() { }

}