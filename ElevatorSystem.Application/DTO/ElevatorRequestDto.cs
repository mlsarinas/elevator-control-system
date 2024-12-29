using ElevatorSystem.Domain.Entities;

namespace ElevatorSystem.Application.DTO;

public record ElevatorRequestDto(int Floor, Direction Direction);