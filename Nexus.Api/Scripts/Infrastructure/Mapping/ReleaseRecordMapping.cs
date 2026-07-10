using System.Text;
using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Scripts.Aggregates;

namespace Nexus.Scripts.Infrastructure.Mapping;

internal static class ReleaseRecordMapping
{
    public static Release ToRelease(ReleaseRecord record)
    {
        var sourceCodeSizeBytes = record.SourceCodeSizeBytes;
        if (sourceCodeSizeBytes <= 0 && !string.IsNullOrEmpty(record.SourceCode))
            sourceCodeSizeBytes = Encoding.UTF8.GetByteCount(record.SourceCode);

        return new(
            record.Id.ToString(),
            record.ScriptId,
            new SemanticVersion(record.Major, record.Minor, record.Patch),
            record.SourceCode,
            sourceCodeSizeBytes,
            ContentHash.Create(record.Hash).Value!,
            record.CreatedAt,
            record.IsDeprecated);
    }

    public static ReleaseRecord ToRecord(Release release) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(release.Id)
                ? ObjectId.GenerateNewId()
                : ObjectId.Parse(release.Id),
            ScriptId = release.ScriptId,
            Major = release.Version.Major,
            Minor = release.Version.Minor,
            Patch = release.Version.Patch,
            SourceCode = release.SourceCode,
            SourceCodeSizeBytes = release.SourceCodeSizeBytes,
            Hash = release.Hash.Value,
            IsDeprecated = release.IsDeprecated,
            CreatedAt = release.CreatedAt,
        };
}
