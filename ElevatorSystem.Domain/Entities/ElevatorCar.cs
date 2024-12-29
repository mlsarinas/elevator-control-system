using ElevatorSystem.Domain.Entities;

public class ElevatorCar(int id)
{
    public int Id { get; } = id;
    public int CurrentFloor { get; private set; } = 1;
    public Direction Direction { get; private set; } = Direction.Idle;
    public HashSet<int> Destinations { get; } = new();
    public bool IsMoving { get; private set; }

    public void UpdateStatus(int floor, Direction direction, bool isMoving)
    {
        CurrentFloor = floor;
        Direction = direction;
        IsMoving = isMoving;
    }

    public void AddDestination(int floor)
    {
        Destinations.Add(floor);
    }

    public void RemoveDestination(int floor)
    {
        Destinations.Remove(floor);
    }
}

public class ElevatorRequest(int floor, Direction direction)
{
    public int Floor { get; } = floor;
    public Direction Direction { get; } = direction;
    public DateTime Timestamp { get; } = DateTime.Now;
}