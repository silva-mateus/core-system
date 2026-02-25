namespace Core.Common.Entities;

public interface IAuditableEntity
{
    int? CreatedByUserId { get; set; }
    int? UpdatedByUserId { get; set; }
}
