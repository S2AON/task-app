namespace TaskApp.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    public void MarkAsDeleted()
    {
        IsDeleted = true;

        Touch();
    }

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
