using ElevatorSystem.Domain.Repositories;

namespace ElevatorSystem.Infrastructure.Repositories;

public class InMemoryElevatorRepository : IElevatorRepository
{
    private readonly Dictionary<int, ElevatorCar> _elevators;

    public InMemoryElevatorRepository(int carCount = 4)
    {
        _elevators = Enumerable.Range(1, carCount)
            .ToDictionary(
                id => id,
                id => new ElevatorCar(id)
            );
    }

    public Task<ElevatorCar> GetByIdAsync(int id)
    {
        return Task.FromResult(_elevators[id]);
    }

    public Task<IEnumerable<ElevatorCar>> GetAllAsync()
    {
        return Task.FromResult(_elevators.Values.AsEnumerable());
    }

    public Task UpdateAsync(ElevatorCar car)
    {
        _elevators[car.Id] = car;
        return Task.CompletedTask;
    }
}