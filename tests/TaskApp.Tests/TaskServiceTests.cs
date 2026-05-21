using Microsoft.EntityFrameworkCore;
using TaskApp.Application.Dtos;
using TaskApp.Domain.Entities;
using TaskApp.Infrastructure.Data;
using TaskApp.Infrastructure.Repositories;
using TaskApp.Infrastructure.Services;
using Xunit;
using Task = TaskApp.Domain.Entities.Task;
using TaskFactory = TaskApp.Application.Factories.TaskFactory;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Tests;

public class TaskServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TaskService _service;
    private readonly Guid _testUserId;

    public TaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var repository = new Repository<Task>(_context);
        var unitOfWork = new UnitOfWork(_context);
        var taskFactory = new TaskFactory();

        _service = new TaskService(repository, unitOfWork, taskFactory);
        _testUserId = Guid.NewGuid();

        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            Id = _testUserId,
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var tasks = new[]
        {
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Urgent Bug Fix",
                Description = "Fix critical bug in production",
                Status = TaskStatus.InProgress,
                DueDate = DateTime.UtcNow.AddDays(1),
                CreatedBy = _testUserId,
                AssignedTo = _testUserId,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Team Meeting",
                Description = "Weekly team standup meeting",
                Status = TaskStatus.Pending,
                DueDate = DateTime.UtcNow.AddDays(3),
                CreatedBy = _testUserId,
                AssignedTo = _testUserId,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Task
            {
                Id = Guid.NewGuid(),
                Title = "Write Report",
                Description = "Monthly performance report",
                Status = TaskStatus.Done,
                DueDate = DateTime.UtcNow.AddDays(-1),
                CreatedBy = _testUserId,
                AssignedTo = _testUserId,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        _context.Users.Add(user);
        _context.Tasks.AddRange(tasks);
        _context.SaveChanges();
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAllAsync_ShouldReturnAllTasksForUser()
    {
        var result = await _service.GetAllAsync(_testUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data!.Count());
    }

    [Fact]
    public async System.Threading.Tasks.Task GetByIdAsync_WithValidId_ShouldReturnTask()
    {
        var existingTask = await _context.Tasks.FirstAsync();

        var result = await _service.GetByIdAsync(existingTask.Id, _testUserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingTask.Id, result.Data!.Id);
        Assert.Equal(existingTask.Title, result.Data.Title);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetByIdAsync_WithInvalidId_ShouldReturnFailure()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid(), _testUserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateAsync_ShouldAddNewTask()
    {
        var dto = new CreateTaskDto(
            Title: "New Test Task",
            Description: "Testing task creation",
            DueDate: DateTime.UtcNow.AddDays(7),
            AssignedTo: null,
            CreatedBy: _testUserId
        );

        var result = await _service.CreateAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Data!.Id);
        Assert.Equal("New Test Task", result.Data.Title);

        var count = await _context.Tasks.CountAsync();
        Assert.Equal(4, count);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateAsync_ShouldModifyExistingTask()
    {
        var existingTask = await _context.Tasks.FirstAsync();

        var dto = new UpdateTaskDto(
            Id: existingTask.Id,
            Title: "Updated Title",
            Description: existingTask.Description,
            DueDate: existingTask.DueDate,
            Status: TaskStatus.Done,
            AssignedTo: existingTask.AssignedTo,
            UpdatedBy: _testUserId
        );

        var result = await _service.UpdateAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Title", result.Data!.Title);
        Assert.Equal(TaskStatus.Done, result.Data.Status);
        Assert.NotNull(result.Data.UpdatedAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteAsync_WithValidId_ShouldSoftDeleteTask()
    {
        var taskToDelete = await _context.Tasks.FirstAsync();
        var taskId = taskToDelete.Id;

        var result = await _service.DeleteAsync(taskId, _testUserId);

        Assert.True(result.IsSuccess);

        var deletedTask = await _context.Tasks.FindAsync(taskId);
        Assert.NotNull(deletedTask);
        Assert.True(deletedTask.IsDeleted);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteAsync_WithInvalidId_ShouldReturnFailure()
    {
        var result = await _service.DeleteAsync(Guid.NewGuid(), _testUserId);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchAsync_ByTitle_ShouldReturnMatchingTasks()
    {
        var result = await _service.SearchAsync(_testUserId, searchTerm: "Bug");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Contains("Bug", result.Data.First().Title);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchAsync_ByStatus_ShouldReturnFilteredTasks()
    {
        var result = await _service.SearchAsync(_testUserId, status: TaskStatus.InProgress);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Equal(TaskStatus.InProgress, result.Data.First().Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchAsync_WithMultipleFilters_ShouldReturnCorrectResults()
    {
        var result = await _service.SearchAsync(_testUserId, searchTerm: "Report", status: TaskStatus.Done);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Contains("Report", result.Data.First().Title);
        Assert.Equal(TaskStatus.Done, result.Data.First().Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchAsync_WithSorting_ShouldReturnSortedResults()
    {
        var result = await _service.SearchAsync(_testUserId, sortBy: "title", sortDescending: false);

        Assert.True(result.IsSuccess);
        var tasks = result.Data!.ToList();
        Assert.Equal(3, tasks.Count);
        Assert.Equal("Team Meeting", tasks[0].Title);
    }

    [Fact]
    public async System.Threading.Tasks.Task SearchAsync_WithDueDateFilter_ShouldReturnTasksInRange()
    {
        var tomorrow = DateTime.UtcNow.AddDays(1);
        var nextWeek = DateTime.UtcNow.AddDays(7);

        var result = await _service.SearchAsync(_testUserId, dueDateFrom: tomorrow, dueDateTo: nextWeek);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.Any());
        Assert.All(result.Data, task =>
        {
            Assert.True(task.DueDate >= tomorrow);
            Assert.True(task.DueDate <= nextWeek);
        });
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
