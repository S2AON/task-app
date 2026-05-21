using TaskApp.Application.Dtos;
using Task = TaskApp.Domain.Entities.Task;

namespace TaskApp.Application.Interfaces;

public interface ITaskFactory
{
    Task Create(CreateTaskDto dto);
    Task ApplyUpdate(Task existing, UpdateTaskDto dto);
}
