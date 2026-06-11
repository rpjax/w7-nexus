using System.Linq.Expressions;
using Nexus.Gateways.Wintech.Application.Contracts;
using Aidan.Core.Linq;
using Aidan.Mongo.Linq;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Gateways.Wintech.Application;
using Nexus.Gateways.Wintech.Application.Models;

namespace Nexus.Gateways.Wintech.Application;

public sealed class WintechApiCredentialsRepository : IWintechApiCredentialsRepository
{
    private readonly IMongoCollection<WintechApiCredentialsRecord> _collection;

    private static readonly Expression<Func<WintechApiCredentialsRecord, WintechApiCredentials>> ToProjection = r =>
        new WintechApiCredentials
        {
            Id = r.Id.ToString(),
            StrawManId = r.StrawManId,
            Name = r.Name,
            PublicKey = r.PublicKey,
            SecretKey = r.SecretKey,
            Enabled = r.Enabled
        };

    public WintechApiCredentialsRepository(IMongoCollection<WintechApiCredentialsRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<WintechApiCredentials> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<WintechApiCredentials>(source);
    }

    public Task CreateAsync(WintechApiCredentials entity)
    {
        throw new NotSupportedException("Use IWintechApiKeysService to manage Wintech credentials.");
    }

    public Task CreateAsync(IEnumerable<WintechApiCredentials> entities)
    {
        throw new NotSupportedException("Use IWintechApiKeysService to manage Wintech credentials.");
    }

    public Task DeleteAsync(WintechApiCredentials entity)
    {
        throw new NotSupportedException("Use IWintechApiKeysService to manage Wintech credentials.");
    }

    public Task<long> DeleteAsync(Expression<Func<WintechApiCredentials, bool>> predicate)
    {
        throw new NotSupportedException("Use IWintechApiKeysService to manage Wintech credentials.");
    }

    public Task UpdateAsync(WintechApiCredentials entity)
    {
        throw new NotSupportedException("Use IWintechApiKeysService to manage Wintech credentials.");
    }

    public Task<long> UpdateAsync(System.Linq.Expressions.Expression expression)
    {
        throw new NotSupportedException("Use IWintechApiKeysService to manage Wintech credentials.");
    }
}
