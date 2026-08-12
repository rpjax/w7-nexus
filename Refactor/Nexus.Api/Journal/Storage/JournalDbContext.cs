using Microsoft.EntityFrameworkCore;

namespace Refactor.Nexus.Api.Journal.Storage;

/// <summary>
/// Journal EF Core context over the shared Nexus Postgres database.
/// Maps only <c>journal_*</c> tables; Accounts and other domains own their own tables.
/// </summary>
public sealed class JournalDbContext : DbContext
{
    public JournalDbContext(DbContextOptions<JournalDbContext> options)
        : base(options)
    {
    }

    public DbSet<JournalEntryRecord> Entries => Set<JournalEntryRecord>();

    public DbSet<JournalIndexKeyRecord> IndexKeys => Set<JournalIndexKeyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JournalEntryConfiguration());
    }
}
