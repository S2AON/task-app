using TaskApp.Application.Dtos;
using TaskApp.Application.Interfaces;
using Task = TaskApp.Domain.Entities.Task;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Application.Factories;

public class TaskFactory : ITaskFactory
{
    public Task Create(CreateTaskDto dto) => new()
    {
        Title = dto.Title,
        Description = dto.Description,
        DueDate = dto.DueDate,
        Status = TaskStatus.Pending,
        CreatedBy = dto.CreatedBy,
        AssignedTo = dto.AssignedTo ?? dto.CreatedBy
    };

    public Task ApplyUpdate(Task existing, UpdateTaskDto dto)
    {
        existing.Title = dto.Title;
        existing.Description = dto.Description;
        existing.DueDate = dto.DueDate;
        existing.Status = dto.Status;
        existing.AssignedTo = dto.AssignedTo;
        existing.UpdatedBy = dto.UpdatedBy;
        return existing;
    }
}
