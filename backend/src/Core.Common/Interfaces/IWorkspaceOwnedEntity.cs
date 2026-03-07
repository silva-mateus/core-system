namespace Core.Common.Interfaces;

/// <summary>
/// Marker interface for entities scoped to a workspace in multi-tenant applications.
/// Used by EF Core global query filters to enforce tenant isolation.
/// </summary>
public interface IWorkspaceOwnedEntity
{
    int WorkspaceId { get; set; }
}
