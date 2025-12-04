using Microsoft.EntityFrameworkCore;
using OmniCalendar.Api.Domain.Entities;

namespace OmniCalendar.Api.Infrastructure;

public class OmniCalendarDbContext : DbContext
{
    public OmniCalendarDbContext(DbContextOptions<OmniCalendarDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskNote> TaskNotes => Set<TaskNote>();
    public DbSet<TaskTag> TaskTags => Set<TaskTag>();
    public DbSet<HolidayCache> HolidayCache => Set<HolidayCache>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClerkId).HasColumnName("clerk_id").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Nickname).HasColumnName("nickname").HasMaxLength(100);
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.ExperiencePoints).HasColumnName("experience_points");
            entity.Property(e => e.CurrentRank).HasColumnName("current_rank").HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.ClerkId).HasDatabaseName("idx_users_clerk_id");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.ColorHex).HasColumnName("color_hex").HasMaxLength(7);
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.IsDefault).HasColumnName("is_default");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
            entity.Property(e => e.ColorHex).HasColumnName("color_hex").HasMaxLength(7);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Tags)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.IsAllDay).HasColumnName("is_all_day");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.RecurrenceRule).HasColumnName("recurrence_rule").HasMaxLength(255);
            entity.Property(e => e.ParentTaskId).HasColumnName("parent_task_id");
            entity.Property(e => e.IsCompleted).HasColumnName("is_completed");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Tasks)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ParentTask)
                .WithMany(t => t.ChildTasks)
                .HasForeignKey(e => e.ParentTaskId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.UserId, e.StartTime }).HasDatabaseName("idx_tasks_user_starttime");
            entity.HasIndex(e => new { e.UserId, e.DueDate }).HasDatabaseName("idx_tasks_user_duedate");
            entity.HasIndex(e => e.CategoryId).HasDatabaseName("idx_tasks_category");
        });

        modelBuilder.Entity<TaskNote>(entity =>
        {
            entity.ToTable("task_notes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");

            entity.HasOne(e => e.Task)
                .WithMany(t => t.Notes)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskTag>(entity =>
        {
            entity.ToTable("task_tags");
            entity.HasKey(e => new { e.TaskId, e.TagId });
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.TagId).HasColumnName("tag_id");

            entity.HasOne(e => e.Task)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HolidayCache>(entity =>
        {
            entity.ToTable("holiday_cache");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(5);
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.DataJson).HasColumnName("data_json").HasColumnType("jsonb");
            entity.Property(e => e.FetchedAt).HasColumnName("fetched_at");
        });
    }
}


