using System.Linq.Expressions;
using Nexus.Gateways.Frendz.Application.Contracts;
using Aidan.Core.Linq;
using Aidan.Mongo.Linq;
using MongoDB.Driver;
using Nexus.Database.Models;
using Nexus.Gateways.Frendz.Application;
using Nexus.Gateways.Frendz.Application.Models;

namespace Nexus.Gateways.Frendz.Application;

public sealed class FrendzApiCredentialsRepository : IFrendzApiCredentialsRepository
{
    private readonly IMongoCollection<FrendzApiCredentialsRecord> _collection;

    private static readonly Expression<Func<FrendzApiCredentialsRecord, FrendzApiCredentials>> ToProjection = r =>
        new FrendzApiCredentials
        {
            Id = r.Id.ToString(),
            StrawManId = r.StrawManId,
            Name = r.Name,
            Token = r.Token,
            Enabled = r.Enabled
        };

    public FrendzApiCredentialsRepository(IMongoCollection<FrendzApiCredentialsRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<FrendzApiCredentials> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<FrendzApiCredentials>(source);
    }

    public Task CreateAsync(FrendzApiCredentials entity)
    {
        throw new NotSupportedException("Use IFrendzApiKeysService to manage Frendz credentials.");
    }

    public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities)
    {
        throw new NotSupportedException("Use IFrendzApiKeysService to manage Frendz credentials.");
    }

    public Task DeleteAsync(FrendzApiCredentials entity)
    {
        throw new NotSupportedException("Use IFrendzApiKeysService to manage Frendz credentials.");
    }

    public Task<long> DeleteAsync(Expression<Func<FrendzApiCredentials, bool>> predicate)
    {
        throw new NotSupportedException("Use IFrendzApiKeysService to manage Frendz credentials.");
    }

    public Task UpdateAsync(FrendzApiCredentials entity)
    {
        throw new NotSupportedException("Use IFrendzApiKeysService to manage Frendz credentials.");
    }

    public Task<long> UpdateAsync(System.Linq.Expressions.Expression expression)
    {
        throw new NotSupportedException("Use IFrendzApiKeysService to manage Frendz credentials.");
    }
}
