using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Projects;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Abstractions.Projects;

public interface IProjectStore
{
    Task<Project?> FindByIdAsync(Guid projectId, CancellationToken cancellationToken);

    Task<PagedResult<Project>> ListAsync(
        ProjectListQuery query,
        Guid? projectManagerId,
        Guid? memberUserId,
        CancellationToken cancellationToken);

    Task<bool> IsMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<ProjectMember?> FindMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProjectMember>> ListMembersAsync(
        Guid projectId,
        CancellationToken cancellationToken);

    Task AddMemberAsync(
        ProjectMember member,
        CancellationToken cancellationToken);

    void RemoveMember(ProjectMember member);

    Task AddAsync(Project project, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
