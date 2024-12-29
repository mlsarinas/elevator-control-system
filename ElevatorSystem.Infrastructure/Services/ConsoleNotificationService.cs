using ElevatorSystem.Application.Interfaces;
using ElevatorSystem.Domain.Events;

namespace ElevatorSystem.Infrastructure.Services;

public class ConsoleNotificationService : INotificationService
{
    public Task NotifyStatusChangeAsync(ElevatorStatusChanged statusChanged)
    {
        Console.WriteLine($"{DateTime.Now:HH:mm:ss}: Car {statusChanged.CarId} is on floor {statusChanged.Floor}, moving {statusChanged.Direction}");
        return Task.CompletedTask;
    }
    

    public Task LogMessageAsync(string message)
    {
        Console.WriteLine($"{DateTime.Now:HH:mm:ss}: {message}");
        return Task.CompletedTask;
    }
}