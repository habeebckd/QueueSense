using System.Text.Json;
using JobFlow.Core.Entities;
using JobFlow.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Infrastructure.Data;

public class JobFlowDbContext : DbContext
{
    public JobFlowDbContext(DbContextOptions<JobFlowDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<JobDefinition> JobDefinitions => Set<JobDefinition>();
    public DbSet<JobSchedule> JobSchedules => Set<JobSchedule>();
    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();
    public DbSet<JobLog> JobLogs => Set<JobLog>();
    public DbSet<JobExecutionEvent> JobExecutionEvents => Set<JobExecutionEvent>();
    public DbSet<AIAnalysisResult> AIAnalysisResults => Set<AIAnalysisResult>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(u => u.Role).IsRequired().HasConversion<string>().HasMaxLength(30);
            entity.Property(u => u.CreatedAt).IsRequired();
        });

        // 2. JobDefinition
        modelBuilder.Entity<JobDefinition>(entity =>
        {
            entity.HasKey(j => j.Id);
            entity.Property(j => j.Name).IsRequired().HasMaxLength(150);
            entity.HasIndex(j => j.Name);
            entity.Property(j => j.Description).HasMaxLength(1000);
            entity.Property(j => j.HandlerType).IsRequired().HasMaxLength(150);
            entity.Property(j => j.Configuration).HasColumnType("text");
            entity.Property(j => j.IsEnabled).IsRequired().HasDefaultValue(true);
            entity.HasIndex(j => j.IsEnabled);
            entity.Property(j => j.CreatedAt).IsRequired();
            entity.Property(j => j.UpdatedAt).IsRequired();

            entity.HasOne(j => j.Creator)
                .WithMany(u => u.CreatedJobs)
                .HasForeignKey(j => j.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 3. JobSchedule
        modelBuilder.Entity<JobSchedule>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.ScheduleType).IsRequired().HasConversion<string>().HasMaxLength(30);
            entity.Property(s => s.CronExpression).HasMaxLength(100);
            entity.Property(s => s.TimeZone).IsRequired().HasMaxLength(50).HasDefaultValue("UTC");
            entity.Property(s => s.IsEnabled).IsRequired().HasDefaultValue(true);
            entity.HasIndex(s => new { s.IsEnabled, s.NextRunAt });

            entity.HasOne(s => s.JobDefinition)
                .WithMany(j => j.Schedules)
                .HasForeignKey(s => s.JobDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 4. JobExecution
        modelBuilder.Entity<JobExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Progress).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.AttemptNumber).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.QueuedAt).IsRequired();
            entity.Property(e => e.WorkerId).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasColumnType("text");
            entity.Property(e => e.Result).HasColumnType("text");

            entity.HasIndex(e => new { e.Status, e.QueuedAt });
            entity.HasIndex(e => new { e.Status, e.LeaseExpiresAt });

            entity.HasOne(e => e.JobDefinition)
                .WithMany(j => j.Executions)
                .HasForeignKey(e => e.JobDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentExecution)
                .WithMany(p => p.ChildExecutions)
                .HasForeignKey(e => e.ParentExecutionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 5. JobLog
        modelBuilder.Entity<JobLog>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Level).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(l => l.Message).IsRequired().HasColumnType("text");
            entity.Property(l => l.Timestamp).IsRequired();
            entity.Property(l => l.SequenceNumber).IsRequired();

            entity.HasIndex(l => new { l.ExecutionId, l.SequenceNumber });

            entity.HasOne(l => l.Execution)
                .WithMany(e => e.Logs)
                .HasForeignKey(l => l.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 6. JobExecutionEvent
        modelBuilder.Entity<JobExecutionEvent>(entity =>
        {
            entity.HasKey(ev => ev.Id);
            entity.Property(ev => ev.PreviousStatus).IsRequired().HasMaxLength(30);
            entity.Property(ev => ev.NewStatus).IsRequired().HasMaxLength(30);
            entity.Property(ev => ev.Reason).HasMaxLength(500);
            entity.Property(ev => ev.Timestamp).IsRequired();
            entity.Property(ev => ev.CorrelationId).IsRequired().HasMaxLength(64);

            entity.HasIndex(ev => new { ev.ExecutionId, ev.Timestamp });

            entity.HasOne(ev => ev.Execution)
                .WithMany(e => e.Events)
                .HasForeignKey(ev => ev.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 7. AIAnalysisResult
        modelBuilder.Entity<AIAnalysisResult>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Summary).IsRequired().HasColumnType("text");
            entity.Property(a => a.ProbableCauses).IsRequired().HasColumnType("text");
            entity.Property(a => a.SuggestedSolutions).IsRequired().HasColumnType("text");
            entity.Property(a => a.Severity).IsRequired().HasMaxLength(20);
            entity.Property(a => a.Confidence).IsRequired().HasMaxLength(20);
            entity.Property(a => a.RetryRecommended).IsRequired().HasDefaultValue(false);
            entity.Property(a => a.CreatedAt).IsRequired();

            entity.HasIndex(a => a.ExecutionId).IsUnique();

            entity.HasOne(a => a.Execution)
                .WithOne(e => e.AIAnalysisResult)
                .HasForeignKey<AIAnalysisResult>(a => a.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 8. Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Type).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(n => n.Message).IsRequired().HasColumnType("text");
            entity.Property(n => n.IsRead).IsRequired().HasDefaultValue(false);
            entity.Property(n => n.CreatedAt).IsRequired();

            entity.HasIndex(n => new { n.UserId, n.IsRead });

            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Execution)
                .WithMany()
                .HasForeignKey(n => n.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed Data
        SeedInitialData(modelBuilder);
    }

    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        var adminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        // Valid verified BCrypt hash for "AdminPassword123!"
        var adminPasswordHash = "$2a$11$TKHys93kM2KIp9A57Cimh.jmBJKGCAKsDCC8XllMkKM/2o.SXzeoi";

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = adminUserId,
            Name = "System Administrator",
            Email = "admin@jobflow.ai",
            PasswordHash = adminPasswordHash,
            Role = UserRole.Admin,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        var sampleJobId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var batchJobId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var reportJobId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var failureJobId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var longRunJobId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        modelBuilder.Entity<JobDefinition>().HasData(
            new JobDefinition
            {
                Id = sampleJobId,
                Name = "Sample Data Processing Job",
                Description = "Simulates standard ETL data processing, verification, and transformation with progressive checkpoint reporting.",
                HandlerType = "SampleDataProcessingJobHandler",
                Configuration = "{\"batchSize\": 100, \"dataSource\": \"external-crm\"}",
                IsEnabled = true,
                CreatedBy = adminUserId,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new JobDefinition
            {
                Id = batchJobId,
                Name = "Batch Record Processing Job",
                Description = "High-throughput batch processor with record-level validation, chunk commits, and performance telemetry.",
                HandlerType = "BatchRecordProcessingJobHandler",
                Configuration = "{\"totalRecords\": 500, \"chunkSize\": 50, \"simulateFailureRate\": 0}",
                IsEnabled = true,
                CreatedBy = adminUserId,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new JobDefinition
            {
                Id = reportJobId,
                Name = "Report Generation Job",
                Description = "Aggregates multi-table statistics, renders analytic matrices, and produces downloadable report artifacts.",
                HandlerType = "ReportGenerationJobHandler",
                Configuration = "{\"format\": \"PDF\", \"includeCharts\": true, \"dateRange\": \"last-30-days\"}",
                IsEnabled = true,
                CreatedBy = adminUserId,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new JobDefinition
            {
                Id = failureJobId,
                Name = "Simulated Failure Testing Job",
                Description = "Intentionally throws simulated failures (e.g. database timeout or socket failure) to validate automated AI root cause diagnosis.",
                HandlerType = "SimulatedFailureJobHandler",
                Configuration = "{\"failureScenario\": \"DatabaseConnectionTimeout\", \"failAfterSeconds\": 3}",
                IsEnabled = true,
                CreatedBy = adminUserId,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new JobDefinition
            {
                Id = longRunJobId,
                Name = "Configurable Long-Running Test Job",
                Description = "Executes an extended loop to validate cooperative cancellation tokens, heartbeat lease renewal, and timeout guards.",
                HandlerType = "ConfigurableLongRunningJobHandler",
                Configuration = "{\"durationSeconds\": 45, \"intervalSeconds\": 2}",
                IsEnabled = true,
                CreatedBy = adminUserId,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed a sample recurring schedule for the Sample Data Processing job
        modelBuilder.Entity<JobSchedule>().HasData(new JobSchedule
        {
            Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
            JobDefinitionId = sampleJobId,
            ScheduleType = ScheduleType.RecurringCron,
            CronExpression = "0 */4 * * *",
            TimeZone = "UTC",
            NextRunAt = new DateTime(2026, 1, 1, 4, 0, 0, DateTimeKind.Utc),
            IsEnabled = true
        });
    }
}
