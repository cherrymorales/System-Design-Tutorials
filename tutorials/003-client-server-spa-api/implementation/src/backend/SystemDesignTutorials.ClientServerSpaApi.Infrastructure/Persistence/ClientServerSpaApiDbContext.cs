using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ClientServerSpaApi.Domain.Entities;
using SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Identity;

namespace SystemDesignTutorials.ClientServerSpaApi.Infrastructure.Persistence;

public sealed class ClientServerSpaApiDbContext(DbContextOptions<ClientServerSpaApiDbContext> options)
    : IdentityDbContext<AppIdentityUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectTask> Tasks => Set<ProjectTask>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskActivity> TaskActivities => Set<TaskActivity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique();
            entity.HasOne<Project>()
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProjectTask>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.BlockerNote).HasMaxLength(1000);
            entity.HasIndex(x => new { x.ProjectId, x.Status });
            entity.HasIndex(x => new { x.ProjectId, x.AssigneeUserId });
            entity.HasOne<Project>()
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TaskComment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            entity.HasOne<ProjectTask>()
                .WithMany()
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TaskActivity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Summary).HasMaxLength(4000).IsRequired();
            entity.HasIndex(x => new { x.TaskId, x.CreatedAt });
            entity.HasOne<ProjectTask>()
                .WithMany()
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
