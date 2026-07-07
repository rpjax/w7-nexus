using MongoDB.Bson;

namespace Nexus.Database;

internal static class MongoObjectIds
{
    public static ObjectId Resolve(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return ObjectId.GenerateNewId();

        return ObjectId.TryParse(id, out var objectId)
            ? objectId
            : ObjectId.GenerateNewId();
    }

    public static ObjectId Require(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var objectId))
            throw new FormatException($"'{id}' is not a valid MongoDB ObjectId.");

        return objectId;
    }
}
