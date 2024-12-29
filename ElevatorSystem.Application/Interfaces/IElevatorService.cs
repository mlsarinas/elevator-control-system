using ElevatorSystem.Domain.Entities;

namespace ElevatorSystem.Application.Interfaces;

public interface IElevatorService
{
    Task RequestElevatorAsync(int floor, Direction direction);
    Task ProcessPendingRequestsAsync();
    Task MoveElevatorAsync(ElevatorCar car);
}