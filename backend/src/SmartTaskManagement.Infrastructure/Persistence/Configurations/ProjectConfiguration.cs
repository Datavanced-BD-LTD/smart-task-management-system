using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(project => project.ProjectId);

        builder.Property(project => project.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasMaxLength(2000);

        builder.Property(project => project.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasOne(project => project.ProjectManager)
            .WithMany()
            .HasForeignKey(project => project.ProjectManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(project => project.CreatedByUser)
            .WithMany()
            .HasForeignKey(project => project.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(project => project.DeletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(project => new
        {
            project.ProjectManagerId,
            project.IsDeleted,
            project.UpdatedAtUtc
        });

        builder.HasIndex(project => new
        {
            project.IsDeleted,
            project.Name
        });
    }
}
