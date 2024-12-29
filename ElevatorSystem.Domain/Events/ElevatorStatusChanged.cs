using ElevatorSystem.Domain.Entities;

namespace ElevatorSystem.Domain.Events;

public class ElevatorStatusChanged(int carId, int floor, Direction direction)
{
    public int CarId { get; } = carId;
    public int Floor { get; } = floor;
    public Direction Direction { get; } = direction;
}