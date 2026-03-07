namespace Core.Common.Interfaces;

/// <summary>
/// Marker interface for entities that support soft deletion.
/// Entities implementing this interface are never hard-deleted; instead,
/// <see cref="IsDeleted"/> is set to true and <see cref="DeletedAtUtc"/> records the timestamp.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}
