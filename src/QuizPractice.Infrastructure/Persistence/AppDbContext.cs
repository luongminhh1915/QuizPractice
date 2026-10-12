using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using QuizPractice.Application.Abstractions;
using QuizPractice.Domain.Entities;
using QuizPractice.Infrastructure.Persistence.Conventions;

namespace QuizPractice.Infrastructure.Persistence;

/// <summary>The 19 tables of SDS I.3. Also the unit of work a service commits through.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Slider> Sliders => Set<Slider>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<PricePackage> PricePackages => Set<PricePackage>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<SubjectDimension> SubjectDimensions => Set<SubjectDimension>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<QuestionDimension> QuestionDimensions => Set<QuestionDimension>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizRule> QuizRules => Set<QuizRule>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<AttemptQuestion> AttemptQuestions => Set<AttemptQuestion>();

    public async Task<bool> TrySaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Nothing was written; forget the stale changes so this context is not reused with them.
            ChangeTracker.Clear();
            return false;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct = default)
    {
        await using var transaction = await Database.BeginTransactionAsync(ct);
        await work(ct);
        await transaction.CommitAsync(ct);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Times are datetime2(0) in UTC; money is whole VND (SDS I.3).
        configurationBuilder.Properties<DateTime>().HavePrecision(0);
        configurationBuilder.Properties<decimal>().HavePrecision(12, 0);

        // Foreign keys are ON DELETE NO ACTION unless a configuration says Cascade
        // (only QuizQuestions and QuizRules do). SQL Server replaces the base cascade
        // convention with its own subclass, so both have to go.
        configurationBuilder.Conventions.Remove<CascadeDeleteConvention>();
        configurationBuilder.Conventions.Remove<SqlServerOnDeleteConvention>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        NamingConvention.Apply(modelBuilder);
    }
}
