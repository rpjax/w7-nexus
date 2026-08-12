using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Refactor.Nexus.Api.Journal.Models;

namespace Refactor.Nexus.Api.Journal.Storage;

public sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntryRecord>
{
    public void Configure(EntityTypeBuilder<JournalEntryRecord> builder)
    {
        builder.HasMany(entry => entry.IndexKeys)
            .WithOne()
            .HasForeignKey(indexKey => indexKey.JournalEntrySequence)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.PublishPolicy)
            .HasConversion<int>();
    }
}
