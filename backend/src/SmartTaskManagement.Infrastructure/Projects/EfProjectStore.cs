using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Application.Abstractions.Projects;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Projects;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Infrastructure.Persistence;

namespace SmartTaskManagement.Infrastructure.Projects;

public sealed class EfProjectStore(ApplicationDbContext dbContext) : IProjectStore
{
    public Task<Project?> FindByIdAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return dbContext.Projects
            .SingleOrDefaultAsync(
                project => project.ProjectId == projectId && !project.IsDeleted,
                cancellationToken);
    }

    public async Task<PagedResult<Project>> ListAsync(
        ProjectListQuery query,
        Guid? projectManagerId,
        Guid? memberUserId,
        CancellationToken cancellationToken)
    {
        var projects = dbContext.Projects
            .AsNoTracking()
            .Where(project => !project.IsDeleted);

        if (projectManagerId.HasValue)
        {
            projects = projects.Where(project =>
                project.ProjectManagerId == projectManagerId.Value);
        }

        if (memberUserId.HasValue)
        {
            projects = projects.Where(project =>
                project.ProjectMembers.Any(member => member.UserId == memberUserId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            projects = projects.Where(project =>
                EF.Functions.Like(project.Name, pattern) ||
                (project.Description != null && EF.Functions.Like(project.Description, pattern)));
        }

        var isDescending = string.Equals(
            query.SortDirection,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        projects = query.SortBy.ToLowerInvariant() switch
        {
            "name" => isDescending
                ? projects.OrderByDescending(project => project.Name)
                    .ThenBy(project => project.ProjectId)
                : projects.OrderBy(project => project.Name)
                    .ThenBy(project => project.ProjectId),
            "updatedat" => isDescending
                ? projects.OrderByDescending(project => project.UpdatedAtUtc)
                    .ThenBy(project => project.ProjectId)
                : projects.OrderBy(project => project.UpdatedAtUtc)
                    .ThenBy(project => project.ProjectId),
            _ => isDescending
                ? projects.OrderByDescending(project => project.CreatedAtUtc)
                    .ThenBy(project => project.ProjectId)
                : projects.OrderBy(project => project.CreatedAtUtc)
                    .ThenBy(project => project.ProjectId)
        };

        var totalCount = await projects.CountAsync(cancellationToken);
        var skip = (long)(query.Page - 1) * query.PageSize;
        var items = skip >= totalCount
            ? []
            : await projects
                .Skip((int)skip)
                .Take(query.PageSize)
                .ToArrayAsync(cancellationToken);

        return new PagedResult<Project>(
            items,
            query.Page,
            query.PageSize,
            totalCount);
    }

    public Task<bool> IsMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.ProjectMembers
            .AnyAsync(
                member => member.ProjectId == projectId && member.UserId == userId,
                cancellationToken);
    }

    public Task<ProjectMember?> FindMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.ProjectMembers
            .Include(member => member.User)
            .SingleOrDefaultAsync(
                member => member.ProjectId == projectId && member.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProjectMember>> ListMembersAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProjectMembers
            .AsNoTracking()
            .Include(member => member.User)
            .Where(member => member.ProjectId == projectId)
            .OrderBy(member => member.AddedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddMemberAsync(
        ProjectMember member,
        CancellationToken cancellationToken)
    {
        await dbContext.ProjectMembers.AddAsync(member, cancellationToken);
    }

    public void RemoveMember(ProjectMember member)
    {
        dbContext.ProjectMembers.Remove(member);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        await dbContext.Projects.AddAsync(project, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
