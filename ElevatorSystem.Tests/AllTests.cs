using Moq;
using FluentAssertions;
using ElevatorSystem.Domain.Entities;
using ElevatorSystem.Domain.Events;
using ElevatorSystem.Domain.Repositories;
using ElevatorSystem.Application.Interfaces;
using ElevatorSystem.Application.Services;

namespace ElevatorSystem.Tests;

public class ElevatorServiceTests
{
    private readonly Mock<IElevatorRepository> _repositoryMock;
    private readonly Mock<INotificationService> _notificationMock;
    private readonly ElevatorService _sut;
    private readonly List<ElevatorCar> _elevators;

    public ElevatorServiceTests()
    {
        _repositoryMock = new Mock<IElevatorRepository>();
        _notificationMock = new Mock<INotificationService>();
        _sut = new ElevatorService(_repositoryMock.Object, _notificationMock.Object);
            
        // Setup test elevators
        _elevators = new List<ElevatorCar>
        {
            new ElevatorCar(1) { /* Floor 1 */ },
            new ElevatorCar(2) { /* Floor 1 */ },
            new ElevatorCar(3) { /* Floor 1 */ },
            new ElevatorCar(4) { /* Floor 1 */ }
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(_elevators);
    }

    [Theory]
    [InlineData(0)]  // Below minimum
    [InlineData(11)] // Above maximum
    public async Task RequestElevator_InvalidFloor_LogsErrorAndDoesNotProcess(int floor)
    {
        // Act
        await _sut.RequestElevatorAsync(floor, Direction.Up);

        // Assert
        _notificationMock.Verify(
            n => n.LogMessageAsync(It.Is<string>(s => s.Contains("Invalid floor request"))),
            Times.Once);
            
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task RequestElevator_AllElevatorsMoving_NoElevatorAssigned()
    {
        // Arrange
        foreach (var elevator in _elevators)
        {
            elevator.UpdateStatus(elevator.CurrentFloor, Direction.Up, true);
        }

        // Act
        await _sut.RequestElevatorAsync(5, Direction.Up);

        // Assert
        _notificationMock.Verify(
            n => n.LogMessageAsync(It.Is<string>(s => s.Contains("No idle elevators available"))),
            Times.Once);
    }

    [Fact]
    public async Task RequestElevator_MultipleIdleElevators_SelectsClosestElevator()
    {
        // Arrange
        _elevators[0].UpdateStatus(2, Direction.Idle, false);
        _elevators[1].UpdateStatus(4, Direction.Idle, false);
        _elevators[2].UpdateStatus(6, Direction.Idle, false);
        _elevators[3].UpdateStatus(8, Direction.Idle, false);

        // Act
        await _sut.RequestElevatorAsync(5, Direction.Up);

        // Assert
        _notificationMock.Verify(
            n => n.LogMessageAsync(It.Is<string>(msg => 
                msg.Contains("Assigned car") && msg.Contains("2"))), // Car 2 (floor 4) should be selected
            Times.Once);
    }

    [Fact]
    public async Task MoveElevator_SingleDestination_CompletesJourney()
    {
        // Arrange
        var elevator = new ElevatorCar(1);
        elevator.AddDestination(5); // Request to go to floor 5

        var statusUpdates = new List<ElevatorStatusChanged>();
        _notificationMock.Setup(n => n.NotifyStatusChangeAsync(It.IsAny<ElevatorStatusChanged>()))
            .Callback<ElevatorStatusChanged>(update => statusUpdates.Add(update))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.MoveElevatorAsync(elevator);

        // Assert
        statusUpdates.Should().HaveCount(4); // Should have 4 updates (floors 2,3,4,5)
        elevator.CurrentFloor.Should().Be(5);
        elevator.IsMoving.Should().BeFalse();
        elevator.Direction.Should().Be(Direction.Idle);
    }

    [Fact]
    public async Task GetNextDestination_MultipleDestinations_OptimizesRoute()
    {
        // Arrange
        var elevator = new ElevatorCar(1);
        elevator.UpdateStatus(3, Direction.Up, false);
        elevator.AddDestination(7);
        elevator.AddDestination(5);
        elevator.AddDestination(9);

        // Act
        await _sut.MoveElevatorAsync(elevator);

        // Assert
        var statusUpdates = new List<string>();
        _notificationMock.Verify(n => n.LogMessageAsync(It.IsAny<string>()), 
            (Times.AtLeast(3)));
            
        // Should visit floors in order: 5, 7, 9
        elevator.CurrentFloor.Should().Be(9);
    }

    [Fact]
    public async Task ProcessPendingRequests_MultipleConcurrentRequests_HandlesAllRequests()
    {
        // Arrange
        var processedRequests = new List<(int Floor, Direction Direction)>();
        _notificationMock.Setup(n => n.LogMessageAsync(It.IsAny<string>()))
            .Callback<string>(msg => {
                if (msg.Contains("Assigned car"))
                    processedRequests.Add((5, Direction.Up));
            })
            .Returns(Task.CompletedTask);

        // Act
        await Task.WhenAll(
            _sut.RequestElevatorAsync(5, Direction.Up),
            _sut.RequestElevatorAsync(3, Direction.Down),
            _sut.RequestElevatorAsync(7, Direction.Up)
        );

        // Assert
        processedRequests.Should().NotBeEmpty();
        _notificationMock.Verify(
            n => n.LogMessageAsync(It.Is<string>(s => s.Contains("New"))),
            Times.Exactly(3));
    }
}

public class ElevatorCarTests
{
    [Fact]
    public void UpdateStatus_ValidParameters_UpdatesState()
    {
        // Arrange
        var elevator = new ElevatorCar(1);

        // Act
        elevator.UpdateStatus(5, Direction.Up, true);

        // Assert
        elevator.CurrentFloor.Should().Be(5);
        elevator.Direction.Should().Be(Direction.Up);
        elevator.IsMoving.Should().BeTrue();
    }

    [Fact]
    public void AddDestination_NewDestination_AddsToSet()
    {
        // Arrange
        var elevator = new ElevatorCar(1);

        // Act
        elevator.AddDestination(5);
        elevator.AddDestination(5); // Duplicate
        elevator.AddDestination(7);

        // Assert
        elevator.Destinations.Should().BeEquivalentTo(new[] { 5, 7 });
    }

    [Fact]
    public void RemoveDestination_ExistingDestination_RemovesFromSet()
    {
        // Arrange
        var elevator = new ElevatorCar(1);
        elevator.AddDestination(5);
        elevator.AddDestination(7);

        // Act
        elevator.RemoveDestination(5);

        // Assert
        elevator.Destinations.Should().BeEquivalentTo(new[] { 7 });
    }
}