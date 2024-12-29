using ElevatorSystem.Application.Interfaces;
using ElevatorSystem.Application.Services;
using ElevatorSystem.Domain.Entities;
using ElevatorSystem.Domain.Repositories;
using ElevatorSystem.Infrastructure.Repositories;
using ElevatorSystem.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = new ServiceCollection()
    .AddSingleton<IElevatorRepository, InMemoryElevatorRepository>()
    .AddSingleton<INotificationService, ConsoleNotificationService>()
    .AddSingleton<IElevatorService, ElevatorService>()
    .BuildServiceProvider();

var elevatorService = serviceProvider.GetRequiredService<IElevatorService>();

var random = new Random();
while (true)
{
    var floor = random.Next(1, 11);
    var direction = random.Next(2) == 0 ? Direction.Up : Direction.Down;
                
    await elevatorService.RequestElevatorAsync(floor, direction);
    await Task.Delay(random.Next(5000, 15001));
}