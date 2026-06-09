using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Mongo.Linq;
using MongoDB.Driver;
using Nexus.Legacy.Database.Models;
using Nexus.Legacy.SigiloPay.Application;
using Nexus.Legacy.SigiloPay.Application.Models;

namespace Nexus.Legacy.SigiloPay.Infrastructure;

public sealed class SigiloPayApiCredentialsRepository : ISigiloPayApiCredentialsRepository
{
    private readonly IMongoCollection<SigiloPayApiCredentialsRecord> _collection;

    private static readonly Expression<Func<SigiloPayApiCredentialsRecord, SigiloPayApiCredentials>> ToProjection = r =>
        new SigiloPayApiCredentials
        {
            Id = r.Id.ToString(),
            StrawManId = r.StrawManId,
            Name = r.Name,
            PublicKey = r.PublicKey,
            SecretKey = r.SecretKey,
            Enabled = r.Enabled
        };

    public SigiloPayApiCredentialsRepository(IMongoCollection<SigiloPayApiCredentialsRecord> collection)
    {
        _collection = collection;
    }

    public IAsyncQueryable<SigiloPayApiCredentials> AsQueryable()
    {
        var source = _collection.AsQueryable().Select(ToProjection);
        return new MongoAsyncQueryable<SigiloPayApiCredentials>(source);
    }

    public Task CreateAsync(SigiloPayApiCredentials entity)
    {
        throw new NotSupportedException("Use ISigiloPayApiKeysService to manage SigiloPay credentials.");
    }

    public Task CreateAsync(IEnumerable<SigiloPayApiCredentials> entities)
    {
        throw new NotSupportedException("Use ISigiloPayApiKeysService to manage SigiloPay credentials.");
    }

    public Task DeleteAsync(SigiloPayApiCredentials entity)
    {
        throw new NotSupportedException("Use ISigiloPayApiKeysService to manage SigiloPay credentials.");
    }

    public Task<long> DeleteAsync(Expression<Func<SigiloPayApiCredentials, bool>> predicate)
    {
        throw new NotSupportedException("Use ISigiloPayApiKeysService to manage SigiloPay credentials.");
    }

    public Task UpdateAsync(SigiloPayApiCredentials entity)
    {
        throw new NotSupportedException("Use ISigiloPayApiKeysService to manage SigiloPay credentials.");
    }

    public Task<long> UpdateAsync(System.Linq.Expressions.Expression expression)
    {
        throw new NotSupportedException("Use ISigiloPayApiKeysService to manage SigiloPay credentials.");
    }
}
