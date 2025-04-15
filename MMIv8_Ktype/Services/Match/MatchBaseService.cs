using MMIv8_Ktype.Models.Util;
using MongoDB.Driver;
using Serilog;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Core.Contexts;

namespace MMIv8_Ktype.Core.Services.Match
{
    public class MatchBaseService(MongoDBContext MMIv8_Ktype, 
                                  MongoBaseContext BaseContext,
                                  IVersionProvider versionProvider) : IMongoCollectionService<MatchBase>
    {
        public IMongoCollection<MatchBase> Collection => MMIv8_Ktype.Collections.MatchBase;

        public async Task<IAsyncCursor<MatchBase>> GetAll(FilterDefinition<MatchBase>? filter = null, int? batchSize = null)
        {
            return await BaseContext.GetCursor(Collection, filter: filter, batchSize: batchSize);
        }

        public async Task<IAsyncCursor<MatchBase>> GetAll(MatchBaseType matchBaseType, FilterDefinition<MatchBase>? filter = null, int? batchSize = null)
        {
            var builder = Builders<MatchBase>.Filter;
            filter ??= builder.Empty;
            filter &= builder.Eq(c => c.MatchBaseType, matchBaseType);
            return await BaseContext.GetCursor(Collection, filter: filter, batchSize: batchSize);
        }

        public async Task<IAsyncCursor<MatchBase>> GetAll(MatchBaseType matchBaseType, MatchBaseMethod method, FilterDefinition<MatchBase>? filter = null, int? batchSize = null)
        {
            var builder = Builders<MatchBase>.Filter;
            filter ??= builder.Empty;
            filter &= builder.Eq(c => c.MatchBaseType, matchBaseType)
                    & builder.Eq(c => c.MatchBaseMethod, method);
            return await BaseContext.GetCursor(Collection, filter: filter, batchSize: batchSize);
        }

        public async Task<MatchBase?> GetById(string matchHash)
        {
            var filter = Builders<MatchBase>.Filter.Eq(e => e.MatchHash, matchHash);
            return await BaseContext.GetSingleDocument(Collection, filter: filter);
        }

        public CombinationPipeline<MatchBase> UpdateMatchBase(MatchBase matchBase)
        {
            var filter = Builders<MatchBase>.Filter.Eq(c => c.MatchHash, matchBase.MatchHash);

            return new CombinationPipeline<MatchBase>(Collection, filter);
        }

        public CombinationPipeline<MatchBase> UpdateScore(MatchBase matchBase, decimal newScore)
        {
            var filter = Builders<MatchBase>.Filter.Eq(c => c.MatchHash, matchBase.MatchHash);

            return new CombinationPipeline<MatchBase>(Collection, filter)
                .AppendUpdate(c => c.UpdateMatchBaseScore(newScore)) //TODO Should I move this into the query or leave it here as always necessary?
                .AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Updated, $"Score: {newScore.ToString()}")));
        }

        public async Task Create(MatchBaseType matchBaseType, MatchBase model)
        {
            // validate
            var builder = Builders<MatchBase>.Filter;
            var filter = builder.Eq(e => e.MatchBaseType, matchBaseType)
                       & builder.Eq(e => e.MatchHash, model.MatchHash);

            if (await BaseContext.GetSingleDocument(Collection, filter: filter) is not null)
            {
                Log.Information("Model already exists: {model}", model.ToString());
                return;
            }
            // save
            await BaseContext.Create(Collection, model.Reset(versionProvider));
        }

        public async Task Create(MatchBaseType matchBaseType, List<MatchBase> model)
        {
            model = model.DistinctBy(c => new { c.MatchBaseType, c.MatchHash }).ToList();

            var builder = Builders<MatchBase>.Filter;
            var filter = builder.Eq(e => e.MatchBaseType, matchBaseType)
                       & builder.In(e => e.MatchHash, model.Select(m => m.MatchHash)); //TODO maybe make enumerable call

            var existingMatches = await BaseContext.GetMultipleDocuments(Collection, filter);

            model = model.Except(existingMatches).ToList();

            // save
            if (model.Count != 0)
            {
                foreach (var item in model)
                {
                    await BaseContext.Create(Collection, item.Reset(versionProvider));
                }
            }
        }

        public async Task CreateBulk(List<MatchBase> model) //TODO reveiw using a replaceMany with Upsert
        {

            model = model.DistinctBy(c => new { c.MatchBaseType, c.MatchHash }).Select(c => c.Reset(versionProvider)).ToList();

            await BaseContext.Create(Collection, model.ToArray());
        }

        public async Task DeleteAll(MatchBaseType matchBaseType)
        {
            var builder = Builders<MatchBase>.Filter;
            var filter = builder.Eq(e => e.MatchBaseType, matchBaseType);
            await BaseContext.Delete(Collection, filter);  
        }

        public async Task Delete(MatchBaseType matchBaseType, string MatchHash)
        {
            var builder = Builders<MatchBase>.Filter;
            var filter = builder.Eq(e => e.MatchBaseType, matchBaseType);
            filter &= builder.Eq(e => e.MatchHash, MatchHash);
            await BaseContext.Delete(Collection, filter);  
        }
    }
}
