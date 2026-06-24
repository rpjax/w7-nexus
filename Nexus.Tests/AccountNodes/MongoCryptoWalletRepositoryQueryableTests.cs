using Aidan.Core.Linq.Extensions;
using Aidan.Mongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.AccountNodes.Aggregates;
using Nexus.AccountNodes.Infrastructure.Persistance;
using Nexus.Database.Models;
using Xunit;

namespace Nexus.Tests.AccountNodes;

public sealed class MongoCryptoWalletRepositoryQueryableTests
{
    [Fact]
    public async Task CountAsync_OnInMemoryEnumerableQueryable_ThrowsArgumentException()
    {
        var inMemory = new MongoAsyncQueryable<CryptoWallet>(Array.Empty<CryptoWallet>().AsQueryable());

        await Assert.ThrowsAsync<ArgumentException>(() => inMemory.CountAsync());
    }

    [Fact]
    public async Task AsQueryable_CountAsync_WithLocalMongo_DoesNotThrow()
    {
        IMongoCollection<AccountNodeCryptoWalletRecord>? collection;
        try
        {
            var client = new MongoClient("mongodb://127.0.0.1:27017");
            await client.ListDatabaseNamesAsync();
            var database = client.GetDatabase($"nexus_test_{Guid.NewGuid():N}");
            collection = database.GetCollection<AccountNodeCryptoWalletRecord>("crypto_wallets");
        }
        catch (Exception ex) when (ex is MongoConnectionException or TimeoutException)
        {
            return;
        }

        var repository = new MongoCryptoWalletRepository(collection);
        var matchingId = ObjectId.GenerateNewId();
        var otherId = ObjectId.GenerateNewId();

        await collection.InsertManyAsync(
        [
            new AccountNodeCryptoWalletRecord
            {
                Id = matchingId,
                StrawManId = "straw-match",
                Addresses =
                {
                    new CryptoWalletAddressRecord
                    {
                        Namespace = AddressNamespace.Tron,
                        Address = "TMatch",
                    },
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new AccountNodeCryptoWalletRecord
            {
                Id = otherId,
                StrawManId = "straw-other",
                Addresses =
                {
                    new CryptoWalletAddressRecord
                    {
                        Namespace = AddressNamespace.Tron,
                        Address = "TOther",
                    },
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        ]);

        var count = await repository.AsQueryable()
            .Where(w => w.StrawManId == "straw-match")
            .CountAsync();

        Assert.Equal(1, count);
    }
}
