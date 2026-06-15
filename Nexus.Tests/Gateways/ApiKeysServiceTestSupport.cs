using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Application.Contracts;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.Wintech.Application.Contracts;

namespace Nexus.Tests.Gateways;

internal static class ApiKeysServiceTestSupport
{
    internal static string Repeat(char c, int count) => new(c, count);

    internal sealed class EmptyFrendzCredentialsRepository : IFrendzApiCredentialsRepository
    {
        public IAsyncQueryable<FrendzApiCredentials> AsQueryable()
            => new QueryableToAsyncQueryableAdapter<FrendzApiCredentials>(Array.Empty<FrendzApiCredentials>().AsQueryable());

        public Task<FrendzApiCredentials> CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<FrendzApiCredentials>.CreateAsync(FrendzApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<FrendzApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    internal sealed class EmptySigiloPayCredentialsRepository : ISigiloPayApiCredentialsRepository
    {
        public IAsyncQueryable<SigiloPayApiCredentials> AsQueryable()
            => new QueryableToAsyncQueryableAdapter<SigiloPayApiCredentials>(Array.Empty<SigiloPayApiCredentials>().AsQueryable());

        public Task<SigiloPayApiCredentials> CreateAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<SigiloPayApiCredentials>.CreateAsync(SigiloPayApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<SigiloPayApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<SigiloPayApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    internal sealed class EmptyWintechCredentialsRepository : IWintechApiCredentialsRepository
    {
        public IAsyncQueryable<WintechApiCredentials> AsQueryable()
            => new QueryableToAsyncQueryableAdapter<WintechApiCredentials>(Array.Empty<WintechApiCredentials>().AsQueryable());

        public Task<WintechApiCredentials> CreateAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<WintechApiCredentials>.CreateAsync(WintechApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<WintechApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<WintechApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    internal sealed class AsyncInMemoryAccountRepository : IAccountRepository
    {
        private readonly List<Account> _store = new();

        public IAsyncQueryable<Account> AsQueryable()
            => new QueryableToAsyncQueryableAdapter<Account>(_store.AsQueryable());

        public Task<Account> CreateAsync(Account entity)
        {
            var persisted = string.IsNullOrWhiteSpace(entity.Id)
                ? new Account(
                    Guid.NewGuid().ToString("N"),
                    entity.Username,
                    entity.PasswordHash,
                    entity.Roles,
                    entity.Permissions,
                    entity.CreatedAt,
                    entity.LastUpdatedAt)
                : entity;

            _store.Add(persisted);
            return Task.FromResult(persisted);
        }

        async Task IRepository<Account>.CreateAsync(Account entity)
        {
            await CreateAsync(entity);
        }

        public Task CreateAsync(IEnumerable<Account> entities)
        {
            _store.AddRange(entities);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Account entity)
        {
            _store.RemoveAll(a => a.Id == entity.Id);
            return Task.CompletedTask;
        }

        public Task<long> DeleteAsync(Expression<Func<Account, bool>> predicate)
        {
            var compiled = predicate.Compile();
            var removed = _store.RemoveAll(a => compiled(a));
            return Task.FromResult((long)removed);
        }

        public Task UpdateAsync(Account entity)
        {
            var index = _store.FindIndex(a => a.Id == entity.Id);
            if (index >= 0)
                _store[index] = entity;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }
}
