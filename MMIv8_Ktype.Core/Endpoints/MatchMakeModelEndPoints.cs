using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class MatchMakeModelEndPoints(MatchMakeModelService matchMakeModelService, 
                                         MappingService mappingService,
                                         BulkMappingService bulkMappingService) : BaseEndpointsWithVersion<MatchMakeModel, ObjectId, FilterQuery<MatchMakeModel>, MatchMakeModelSortRequest>(matchMakeModelService), IMatchMakeModelEndpoints
    {

        public async Task<SerializableResult<List<MatchMakeModel>>> GenerateMakeModelMatch()
        {
            return await bulkMappingService.GenerateMakeModelMatch();
        }

        public async Task<SerializableResult<MatchMakeModel>> GetMakeModelMatch(string TD_SourceEntityModelHash, string MMI_SourceEntityModelHash)
        {
            return await matchMakeModelService.GetByModelIds(TD_SourceEntityModelHash, MMI_SourceEntityModelHash);
        }

        public async Task RegenerateMakeModelSort()
        {
            await mappingService.RegenerateMakeModelSort();
        }

        public async Task<SerializableResult<List<MatchMakeModel>>> GetByModelId(SourceIndex SourceIndex, string SourceEntityModelHash)
        {
            return await matchMakeModelService.GetByModelId(SourceIndex, SourceEntityModelHash);
        }

        public async Task<SerializableResult<MatchMakeModel>> CreateMatchMakeModel(string TD_SourceEntityModelHash = "", string MMI_SourceEntityModelHash = "")
        {
            if (TD_SourceEntityModelHash == "" && MMI_SourceEntityModelHash == "")
                return Error.Validation("MatchMatchModel.CreateValidation","Both parameters cannot be empty");

            return await mappingService.CreateMakeModelMatch(TD_SourceEntityModelHash, MMI_SourceEntityModelHash);
        }

        public async Task<Result> DeleteMakeModelMatch(ObjectId MatchID)
        {
            return await mappingService.DeleteMakeModelMatch(MatchID);
        }

        public async Task<Result> DeleteAll()
        {
            return await matchMakeModelService.DeleteAll();
        }
    }
}
