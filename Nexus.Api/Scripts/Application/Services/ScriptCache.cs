using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Nexus.Scripts.Aggregates;
using Nexus.Scripts.Application.Requests;
using Nexus.Scripts.Application.Responses;

namespace Nexus.Scripts.Application.Services;

public sealed class ScriptCache
{
    private const string KeyPrefix = "scripts:resolve:";
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly ConcurrentDictionary<string, byte> _keys = new();
    private readonly TimeSpan _ttl;

    public ScriptCache(TimeSpan? ttl = null)
    {
        _ttl = ttl ?? TimeSpan.FromSeconds(60);
    }

    public async Task<ResolveScriptsResponse> GetOrCreateAsync(
        string cacheKey,
        Func<Task<ResolveScriptsResponse>> factory)
    {
        var fullKey = KeyPrefix + cacheKey;

        if (_cache.TryGetValue(fullKey, out ResolveScriptsResponse? cached) && cached is not null)
            return cached;

        var value = await factory();
        _cache.Set(fullKey, value, _ttl);
        _keys.TryAdd(fullKey, 0);
        return value;
    }

    public void InvalidateAll()
    {
        foreach (var key in _keys.Keys)
            _cache.Remove(key);

        _keys.Clear();
    }

    public static string BuildCacheKey(ResolveScriptsRequest request, string channelKey)
    {
        var version = string.IsNullOrWhiteSpace(request.Version) ? "current" : request.Version.Trim();
        var deprecated = request.AllowDeprecated ? 1 : 0;

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var hostPart = string.IsNullOrWhiteSpace(request.Host)
                ? string.Empty
                : $":host:{HostPattern.NormalizeHost(request.Host)}";

            return $"name:{request.Name.Trim().ToLowerInvariant()}:{channelKey}{hostPart}:v:{version}:dep:{deprecated}";
        }

        return $"host:{HostPattern.NormalizeHost(request.Host)}:{channelKey}:v:{version}:dep:{deprecated}";
    }

    public static string ComputeAggregateHash(IEnumerable<ResolvedScriptItem> items)
    {
        var builder = new StringBuilder();

        foreach (var item in items.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
            builder.Append(item.Hash);

        if (builder.Length == 0)
            return "empty";

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
