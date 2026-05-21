using System.ComponentModel.DataAnnotations.Schema;
using TaskApp.Domain.Common;

namespace TaskApp.Domain.Entities;

[Table("User",  Schema = "Auth")]
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? GoogleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Task> CreatedTasks { get; set; } = new List<Task>();
    public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
}
