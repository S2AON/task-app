using TaskApp.Application.Common;
using TaskApp.Application.Dtos;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Application.Interfaces;

public interface ITaskService
{
    Task<Result<IEnumerable<TaskDto>>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<Result<TaskDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Result<TaskDto>> CreateAsync(CreateTaskDto task, CancellationToken ct = default);
    Task<Result<TaskDto>> UpdateAsync(UpdateTaskDto task, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);

    Task<Result<IEnumerable<TaskDto>>> SearchAsync(
        Guid userId,
        string? searchTerm = null,
        TaskStatus? status = null,
        Guid? assignedTo = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken ct = default
    );
}
