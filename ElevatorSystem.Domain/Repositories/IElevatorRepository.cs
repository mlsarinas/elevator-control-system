namespace ElevatorSystem.Domain.Repositories;

public interface IElevatorRepository
{
    Task<ElevatorCar> GetByIdAsync(int id);
    Task<IEnumerable<ElevatorCar>> GetAllAsync();
    Task UpdateAsync(ElevatorCar car);
}