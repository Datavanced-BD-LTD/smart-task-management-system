using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("TaskItems");
        builder.HasKey(taskItem => taskItem.Id);

        builder.Property(taskItem => taskItem.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(taskItem => taskItem.Description)
            .HasMaxLength(2000);

        builder.Property(taskItem => taskItem.Status)
            .IsRequired();

        builder.Property(taskItem => taskItem.Priority)
            .IsRequired();

        builder.HasOne(taskItem => taskItem.Project)
            .WithMany(project => project.TaskItems)
            .HasForeignKey(taskItem => taskItem.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(taskItem => taskItem.AssignedToUser)
            .WithMany()
            .HasForeignKey(taskItem => taskItem.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(taskItem => taskItem.CreatedByUser)
            .WithMany()
            .HasForeignKey(taskItem => taskItem.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(taskItem => taskItem.ProjectId);
        builder.HasIndex(taskItem => taskItem.AssignedToUserId);
        builder.HasIndex(taskItem => taskItem.Status);
        builder.HasIndex(taskItem => taskItem.Priority);
        builder.HasIndex(taskItem => taskItem.DueDate);
    }
}
