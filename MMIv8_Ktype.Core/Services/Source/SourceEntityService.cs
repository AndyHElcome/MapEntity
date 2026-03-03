using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Core.Services.Match;
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
    public class SourceMMIv8Service(MongoDBContext MMIv8_Ktype, IVersionProvider versionProvider) : SourceEntityService<SourceMMIv8>(MMIv8_Ktype, MMIv8_Ktype.Collections.SourceMMIv8, versionProvider)
    {
        public override SourceIndex SourceIndex => SourceIndex.MMIv8;
        public override string MatchEntitySourceEntityFieldName => nameof(MatchEntity.MMIv8Entity);
        public override FilterDefinition<MatchEntity> SourceEntityFilter(SourceEntity sourceEntity) => Builders<MatchEntity>.Filter.Eq(e => e.MMIv8Entity.DocumentId, sourceEntity.DocumentId);
        public override FilterDefinition<MatchEntity> SourceEntityFilter(int ExternalId) => Builders<MatchEntity>.Filter.Eq(e => e.MMIv8Entity.ExternalId, ExternalId);

    }

    public class SourceTecDocPCService(MongoDBContext MMIv8_Ktype, IVersionProvider versionProvider) : SourceEntityService<SourceTecDocPC>(MMIv8_Ktype, MMIv8_Ktype.Collections.SourceTecDocPC, versionProvider)
    {
        public override SourceIndex SourceIndex => SourceIndex.TecDocPC;
        public override string MatchEntitySourceEntityFieldName => nameof(MatchEntity.TecDocEntity);
        public override FilterDefinition<MatchEntity> SourceEntityFilter(SourceEntity sourceEntity) => Builders<MatchEntity>.Filter.Eq(e => e.TecDocEntity.DocumentId, sourceEntity.DocumentId);
        public override FilterDefinition<MatchEntity> SourceEntityFilter(int ExternalId) => Builders<MatchEntity>.Filter.Eq(e => e.TecDocEntity.ExternalId, ExternalId);

    }

    public abstract class SourceEntityService<T>(MongoDBContext MMIv8_Ktype, IMongoCollection<T> Collection, IVersionProvider versionProvider) : BaseServiceWithDifferences<T, ObjectId>(Collection, versionProvider)
        where T : SourceEntity
    {
        public abstract SourceIndex SourceIndex { get; }
        public abstract string MatchEntitySourceEntityFieldName { get; }
        public abstract FilterDefinition<MatchEntity> SourceEntityFilter(SourceEntity sourceEntity);
        public abstract FilterDefinition<MatchEntity> SourceEntityFilter(int ExternalId);

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

        public BulkCombinationUpdate CreateBulkCombinationUpdate() //TODO Move into base service with MMIV8KTYPE_CONTEXT
        {
            return new BulkCombinationUpdate(MMIv8_Ktype);
        }

        public async Task RegenerateTextSort()
        {
            await foreach (var entities in base.EnumerateDocuments<T>(Builders<T>.Filter.Empty))
            {
                foreach (var entity in entities)
                {
                    await base.Update(entity).AppendUpdate(c => c.Set(f => f.TextSort, entity.TextSort)).FindAndUpdateDocument();
                }
            }
        }


    }
}
