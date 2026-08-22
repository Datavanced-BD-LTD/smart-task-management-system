using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Application.Abstractions.Projects;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Projects;
using SmartTaskManagement.Domain.Constants;
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
            .Include(project => project.ProjectManager)
            .Include(project => project.CreatedByUser)
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
            .Include(project => project.ProjectManager)
            .Include(project => project.CreatedByUser)
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
            .ThenInclude(user => user!.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .Where(member => member.ProjectId == projectId)
            .OrderBy(member => member.AddedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PagedResult<AvailableProjectMemberResponse>> ListAvailableMembersAsync(
        Guid projectId,
        AvailableProjectMemberQuery query,
        CancellationToken cancellationToken)
    {
        var users = dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.IsActive &&
                user.UserRoles.Any(userRole =>
                    userRole.Role != null &&
                    userRole.Role.Name == RoleNames.TeamMember) &&
                !dbContext.ProjectMembers.Any(member =>
                    member.ProjectId == projectId &&
                    member.UserId == user.UserId));

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var pattern = $"%{query.Keyword.Trim().ToLowerInvariant()}%";
            users = users.Where(user =>
                EF.Functions.Like(user.FirstName.ToLower(), pattern) ||
                EF.Functions.Like(user.LastName.ToLower(), pattern) ||
                EF.Functions.Like(user.Email.ToLower(), pattern));
        }

        var orderedUsers = users
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ThenBy(user => user.Email);

        var totalCount = await orderedUsers.CountAsync(cancellationToken);
        var skip = (long)(query.PageNumber - 1) * query.PageSize;
        var items = skip >= totalCount
            ? []
            : await orderedUsers
                .Skip((int)skip)
                .Take(query.PageSize)
                .Select(user => new AvailableProjectMemberResponse(
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.FirstName + " " + user.LastName,
                    user.Email,
                    RoleNames.TeamMember))
                .ToArrayAsync(cancellationToken);

        return new PagedResult<AvailableProjectMemberResponse>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
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
