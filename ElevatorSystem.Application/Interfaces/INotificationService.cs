using ElevatorSystem.Domain.Events;

namespace ElevatorSystem.Application.Interfaces;

public interface INotificationService
{
    Task NotifyStatusChangeAsync(ElevatorStatusChanged statusChanged);
    Task LogMessageAsync(string message);
}