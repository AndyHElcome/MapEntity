using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using System.Reflection.Metadata;
using System.Xml.Linq;

namespace MMIv8_Ktype.Core.Services.Source
{
    public class SourceMMIv8Service(MongoDBContext MMIv8_Ktype, IVersionProvider versionProvider) : SourceEntityService<SourceMMIv8>(MMIv8_Ktype.Collections.SourceMMIv8, versionProvider);

    public class SourceTecDocPCService(MongoDBContext MMIv8_Ktype, IVersionProvider versionProvider) : SourceEntityService<SourceTecDocPC>(MMIv8_Ktype.Collections.SourceTecDocPC, versionProvider);

    public abstract class SourceEntityService<T>(IMongoCollection<T> Collection, IVersionProvider versionProvider) : BaseServiceWithDifferences<T, ObjectId>(Collection, versionProvider)
        where T : SourceEntity
    {
        public async Task<Result<T>> GetByExternalId(int externalId)
        {
            var filter = Builders<T>.Filter.Eq(e => e.ExternalId, externalId);
            var document = await base.GetFindFluent(filter).FirstOrDefaultAsync();

            return document is not null ? document : Error.NotFound($"{typeof(T)}.NotFoundByExternalId", $"No {typeof(T).Name} exists with ExternalId {externalId}");
        }

        public async Task<Result<List<T>>> GetByModelId(string SourceEntityModelHash)
        {
            var filter = Builders<T>.Filter.Eq(e => e.SourceEntityModelHash, SourceEntityModelHash);
            var documents = await base.GetFindFluent(filter: filter).ToListAsync();

            return documents is { Count: > 0 } ? documents : Error.NotFound($"{typeof(T)}.NotFoundBySourceEntityModelHash", $"No {typeof(T).Name} exists with SourceEntityModelHash {SourceEntityModelHash}");
        }
    }
}
