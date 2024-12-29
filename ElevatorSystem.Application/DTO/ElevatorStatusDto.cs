using ElevatorSystem.Domain.Entities;

namespace ElevatorSystem.Application.DTO;

public record ElevatorStatusDto(int CarId, int Floor, Direction Direction, bool IsMoving);