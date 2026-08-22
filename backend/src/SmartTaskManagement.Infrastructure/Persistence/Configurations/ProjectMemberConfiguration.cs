using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Infrastructure.Persistence.Configurations;

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");
        builder.HasKey(member => new { member.ProjectId, member.UserId });

        builder.HasOne(member => member.Project)
            .WithMany(project => project.ProjectMembers)
            .HasForeignKey(member => member.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(member => member.User)
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(member => member.AddedByUser)
            .WithMany()
            .HasForeignKey(member => member.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(member => new { member.UserId, member.ProjectId });
    }
}
