using Microsoft.EntityFrameworkCore;
using TaskApp.Application.Common;
using TaskApp.Application.Dtos;
using TaskApp.Application.Interfaces;
using Task = TaskApp.Domain.Entities.Task;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Infrastructure.Services;

public class TaskService(IRepository<Task> taskRepository, IUnitOfWork unitOfWork, ITaskFactory taskFactory) : ITaskService
{
    public async Task<Result<IEnumerable<TaskDto>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var tasks = await taskRepository.GetAllAsync(
            q => q.Include(t => t.Creator)
                  .Include(t => t.AssignedUser)
                  .Where(t => t.CreatedBy == userId || t.AssignedTo == userId)
                  .OrderByDescending(t => t.CreatedAt),
            ct
        );

        return Result<IEnumerable<TaskDto>>.Success(tasks.Select(ToDto));
    }

    public async Task<Result<TaskDto>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var task = await taskRepository.GetByIdAsync(
            id,
            q => q.Include(t => t.Creator).Include(t => t.AssignedUser),
            ct
        );

        if (task is null || (task.CreatedBy != userId && task.AssignedTo != userId))
            return Result<TaskDto>.Failure("Task not found");

        return Result<TaskDto>.Success(ToDto(task));
    }

    public async Task<Result<TaskDto>> CreateAsync(CreateTaskDto dto, CancellationToken ct = default)
    {
        var task = taskFactory.Create(dto);

        await taskRepository.AddAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var created = await taskRepository.GetByIdAsync(
            task.Id,
            q => q.Include(t => t.Creator).Include(t => t.AssignedUser),
            ct
        );

        return Result<TaskDto>.Success(ToDto(created!));
    }

    public async Task<Result<TaskDto>> UpdateAsync(UpdateTaskDto dto, CancellationToken ct = default)
    {
        var task = await taskRepository.GetByIdAsync(dto.Id, ct);

        if (task is null || task.CreatedBy != dto.UpdatedBy)
            return Result<TaskDto>.Failure("Task not found or access denied");

        taskFactory.ApplyUpdate(task, dto);

        await taskRepository.UpdateAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var updated = await taskRepository.GetByIdAsync(
            task.Id,
            q => q.Include(t => t.Creator).Include(t => t.AssignedUser),
            ct
        );

        return Result<TaskDto>.Success(ToDto(updated!));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var task = await taskRepository.GetByIdAsync(id, ct);

        if (task is null || task.CreatedBy != userId)
            return Result.Failure("Task not found or access denied");

        await taskRepository.DeleteAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<TaskDto>>> SearchAsync(
        Guid userId,
        string? searchTerm = null,
        TaskStatus? status = null,
        Guid? assignedTo = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken ct = default)
    {
        var tasks = await taskRepository.FindAsync(
            null,
            q =>
            {
                q = q.Include(t => t.Creator)
                     .Include(t => t.AssignedUser)
                     .Where(t => t.CreatedBy == userId || t.AssignedTo == userId);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                    q = q.Where(t => t.Title.Contains(searchTerm) || t.Description.Contains(searchTerm));

                if (status.HasValue)
                    q = q.Where(t => t.Status == status.Value);

                if (assignedTo.HasValue)
                    q = q.Where(t => t.AssignedTo == assignedTo.Value);

                if (dueDateFrom.HasValue)
                    q = q.Where(t => t.DueDate >= dueDateFrom.Value);

                if (dueDateTo.HasValue)
                    q = q.Where(t => t.DueDate <= dueDateTo.Value);

                q = sortBy?.ToLower() switch
                {
                    "title" => sortDescending ? q.OrderByDescending(t => t.Title) : q.OrderBy(t => t.Title),
                    "duedate" => sortDescending ? q.OrderByDescending(t => t.DueDate) : q.OrderBy(t => t.DueDate),
                    "status" => sortDescending ? q.OrderByDescending(t => t.Status) : q.OrderBy(t => t.Status),
                    _ => sortDescending ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt)
                };

                return q;
            },
            ct
        );

        return Result<IEnumerable<TaskDto>>.Success(tasks.Select(ToDto));
    }

    private static TaskDto ToDto(Task t) => new(
        t.Id,
        t.Title,
        t.Description,
        t.DueDate,
        t.Status,
        t.CreatedBy ?? Guid.Empty,
        t.Creator?.FullName ?? string.Empty,
        t.AssignedTo,
        t.AssignedUser?.FullName,
        t.CreatedAt,
        t.UpdatedAt
    );
}
