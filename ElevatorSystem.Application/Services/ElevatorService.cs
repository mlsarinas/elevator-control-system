using ElevatorSystem.Application.Interfaces;
using ElevatorSystem.Domain.Entities;
using ElevatorSystem.Domain.Events;
using ElevatorSystem.Domain.Repositories;

namespace ElevatorSystem.Application.Services
{
    public class ElevatorService(
        IElevatorRepository repository,
        INotificationService notificationService)
        : IElevatorService
    {
        private readonly Queue<ElevatorRequest> _pendingRequests = new();
        private const int FloorCount = 10;
        private const int TravelTimeMs = 10000;
        private const int DoorTimeMs = 10000;

        public async Task RequestElevatorAsync(int floor, Direction direction)
        {
            if (floor < 1 || floor > FloorCount)
            {
                await notificationService.LogMessageAsync($"Invalid floor request: {floor}");
                return;
            }

            var request = new ElevatorRequest(floor, direction);
            _pendingRequests.Enqueue(request);
            await notificationService.LogMessageAsync($"New {direction} request from floor {floor}");
            
            await ProcessPendingRequestsAsync();
        }

        public async Task ProcessPendingRequestsAsync()
        {
            if (!_pendingRequests.TryPeek(out var request)) return;

            var cars = await repository.GetAllAsync();
            var selectedCars = await SelectBestElevatorsAsync(cars.ToList(), request);

            if (selectedCars.Any())
            {
                _pendingRequests.Dequeue();
                
                // Assign to the first car in the sorted list
                var selectedCar = selectedCars.First();
                await AssignRequestToCarAsync(selectedCar, request);
                
                // Start moving all cars that have destinations
                foreach (var car in cars.Where(c => c.Destinations.Any()))
                {
                    // Use Task.Run to handle multiple elevators concurrently
                    _ = Task.Run(async () => await MoveElevatorAsync(car));
                }
            }
        }

        private async Task<List<ElevatorCar>> SelectBestElevatorsAsync(List<ElevatorCar> cars, ElevatorRequest request)
        {
            // Get all idle elevators
            var idleCars = cars.Where(car => !car.IsMoving).ToList();
            
            if (!idleCars.Any())
            {
                await notificationService.LogMessageAsync("No idle elevators available.");
                return new List<ElevatorCar>();
            }

            // Score each idle elevator based on multiple factors
            var scoredCars = idleCars.Select(car =>
            {
                var score = CalculateElevatorScore(car, request);
                return (Car: car, Score: score);
            })
            .OrderBy(x => x.Score)  // Lower score is better
            .ToList();

            await notificationService.LogMessageAsync(
                $"Found {scoredCars.Count} idle elevators. Best score: {scoredCars.First().Score}");

            return scoredCars.Select(x => x.Car).ToList();
        }

        private double CalculateElevatorScore(ElevatorCar car, ElevatorRequest request)
        {
            var score = 0.0;

            // Factor 1: Distance to requested floor (primary factor)
            var distance = Math.Abs(car.CurrentFloor - request.Floor);
            score += distance * 10.0;  // Weight distance more heavily

            // Factor 2: Direction compatibility
            if (car.Direction != Direction.Idle)
            {
                var carGoingUp = car.Direction == Direction.Up;
                var requestGoingUp = request.Direction == Direction.Up;
                var requestAboveCar = request.Floor > car.CurrentFloor;

                // Penalize if elevator is going in opposite direction
                if (carGoingUp != requestGoingUp)
                {
                    score += 50.0;
                }
                // Penalize if request is behind elevator's current direction
                if (carGoingUp != requestAboveCar)
                {
                    score += 30.0;
                }
            }

            // Factor 3: Current load (number of destinations)
            score += car.Destinations.Count * 5.0;

            return score;
        }

        public async Task MoveElevatorAsync(ElevatorCar car)
        {
            if (!car.Destinations.Any() || car.IsMoving) return;

            car.UpdateStatus(car.CurrentFloor, car.Direction, true);
            await repository.UpdateAsync(car);

            while (car.Destinations.Any())
            {
                var nextFloor = GetNextDestination(car);
                if (!nextFloor.HasValue) break;

                var direction = nextFloor.Value > car.CurrentFloor ? Direction.Up : Direction.Down;
                var floorsToTravel = Math.Abs(nextFloor.Value - car.CurrentFloor);

                for (int i = 0; i < floorsToTravel; i++)
                {
                    await Task.Delay(TravelTimeMs);
                    var newFloor = car.CurrentFloor + (direction == Direction.Up ? 1 : -1);
                    car.UpdateStatus(newFloor, direction, true);
                    await repository.UpdateAsync(car);
                    
                    await notificationService.NotifyStatusChangeAsync(
                        new ElevatorStatusChanged(car.Id, car.CurrentFloor, direction));
                }

                car.RemoveDestination(nextFloor.Value);
                await notificationService.LogMessageAsync($"Car {car.Id} arrived at floor {car.CurrentFloor}");
                await Task.Delay(DoorTimeMs);
            }

            car.UpdateStatus(car.CurrentFloor, Direction.Idle, false);
            await repository.UpdateAsync(car);
            await ProcessPendingRequestsAsync();
        }

        private int? GetNextDestination(ElevatorCar car)
        {
            if (!car.Destinations.Any()) return null;

            if (car.Direction == Direction.Up)
            {
                var nextUp = car.Destinations.Where(f => f > car.CurrentFloor);
                return nextUp.Any() ? nextUp.Min() : car.Destinations.Max();
            }
            else if (car.Direction == Direction.Down)
            {
                var nextDown = car.Destinations.Where(f => f < car.CurrentFloor);
                return nextDown.Any() ? nextDown.Max() : car.Destinations.Min();
            }

            return car.Destinations
                .OrderBy(floor => Math.Abs(floor - car.CurrentFloor))
                .First();
        }

        private async Task AssignRequestToCarAsync(ElevatorCar car, ElevatorRequest request)
        {
            car.AddDestination(request.Floor);
            await repository.UpdateAsync(car);
            await notificationService.LogMessageAsync(
                $"Assigned car {car.Id} to {request.Direction} request on floor {request.Floor}");
        }
    }
}