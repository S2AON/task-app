using System.ComponentModel.DataAnnotations.Schema;
using TaskApp.Domain.Common;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Domain.Entities;

[Table("Task",  Schema = "Core")]
public class Task: BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public TaskStatus Status { get; set; }
    public Guid? AssignedTo { get; set; }

    // Navigation properties
    public User? Creator { get; set; }
    public User? AssignedUser { get; set; }
}
