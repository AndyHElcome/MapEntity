using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;
using System;
using Serilog;
using MongoDB.Bson;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models;
using System.Reflection.Metadata;

namespace MMIv8_Ktype.Core.Services
{
    public class EntityRelationService(MongoDBContext MMIv8_Ktype) : BaseService<EntityRelation, ObjectId>(MMIv8_Ktype.Collections.EntityRelation)
    {
        public async Task<IAsyncCursor<EntityRelation>> GetByVersion(ObjectId versionID)
        {
            var filter = Builders<EntityRelation>.Filter.Eq(e => e.VersionID, versionID);

            return await base.GetFindFluent(filter: filter).ToCursorAsync();
        }

        public async Task<IAsyncCursor<EntityRelation>> GetByExternalID(ObjectId versionID, SourceIndex sourceIndex, int externalID)
        {
            var filterBuilder = Builders<EntityRelation>.Filter;
            var filter = filterBuilder.Eq(e => e.VersionID, versionID);

            if (sourceIndex == SourceIndex.MMIv8)
                filter &= filterBuilder.Eq(e => e.MMI_V8_Key, externalID);

            if (sourceIndex == SourceIndex.TecDocPC)
                filter &= filterBuilder.Eq(e => e.KTypNr, externalID);

            return await base.GetFindFluent(filter: filter).ToCursorAsync();
        }

        public async Task<Result<EntityRelation>> GetByExternalIds(ObjectId versionID, int mmi_V8_Key, int kTypNr)
        {
            try
            {
                var filterBuilder = Builders<EntityRelation>.Filter;
                var filter = filterBuilder.Eq(e => e.VersionID, versionID)
                           & filterBuilder.Eq(e => e.MMI_V8_Key, mmi_V8_Key)
                           & filterBuilder.Eq(e => e.KTypNr, kTypNr);

                var result = await base.GetFindFluent(filter: filter).FirstOrDefaultAsync();
                return result is not null ? result : Error.NotFound("EntityRelation.NotFoundByExternalIds", $"No EntityRelation exists with MMI {mmi_V8_Key} KtypNr {kTypNr}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error EntityRelations with MMI {mmi_V8_Key} KtypNr {kTypNr}", mmi_V8_Key, kTypNr);
                return Error.Failure($"EntityRelation.GetByExternalIdsFailure", $"Error getting EntityRelation exists with MMI {mmi_V8_Key} KtypNr {kTypNr}. Error: {ex.Message}");
            }
        }

        public async Task<Result<DeleteResult>> DeleteAllByVersion(ObjectId versionID)
        {
            var filter = Builders<EntityRelation>.Filter.Eq(e => e.VersionID, versionID);
            return await base.DeleteByFilter(filter);
        }
    }
}
