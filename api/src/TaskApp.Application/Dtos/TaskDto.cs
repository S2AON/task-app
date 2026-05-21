using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Application.Dtos;

public record TaskDto(
    Guid Id,
    string Title,
    string Description,
    DateTime DueDate,
    TaskStatus Status,
    Guid CreatedBy,
    string CreatorName,
    Guid? AssignedTo,
    string? AssignedToName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateTaskDto(
    string Title,
    string Description,
    DateTime DueDate,
    Guid? AssignedTo,
    Guid CreatedBy
);

public record UpdateTaskDto(
    Guid Id,
    string Title,
    string Description,
    DateTime DueDate,
    TaskStatus Status,
    Guid? AssignedTo,
    Guid UpdatedBy
);
