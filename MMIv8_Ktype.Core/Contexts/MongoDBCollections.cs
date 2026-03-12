using MongoDB.Driver;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using Serilog;
using MMIv8_Ktype.Models.Collections;

namespace MMIv8_Ktype.Core.Contexts
{
    public class MongoDBCollections
    {
        public MongoDBCollections(IConfiguration configuration, IMongoDatabase mongoDatabase)
        {
            Version = GetCollection<Models.Collections.Version>(mongoDatabase, "Version");
            EntityRelation = GetCollection<EntityRelation>(mongoDatabase);
            User = GetCollection<User>(mongoDatabase);

            SourceTecDocPC = GetCollection<SourceTecDocPC>(mongoDatabase, "SourceTecDocPC");
            SourceMMIv8 = GetCollection<SourceMMIv8>(mongoDatabase, "SourceMMIv8");

            MatchEntity = GetCollection<MatchEntity>(mongoDatabase);
            MatchMakeModel = GetCollection<MatchMakeModel>(mongoDatabase);
            MatchBase = GetCollection<MatchBase>(mongoDatabase);


            CreateViews(mongoDatabase, false);

            SourceTecDocPCModel = GetCollection<MongoSourceEntityModel>(mongoDatabase, "SourceTecDocPCModel");
            SourceMMIv8Model = GetCollection<MongoSourceEntityModel>(mongoDatabase, "SourceMMIv8Model");

            //MatchRefine = GetCollection<MatchRefine>(mongoDatabase);

            CreateAllIndexes(false);
        }

        public readonly IMongoCollection<Models.Collections.Version> Version;
        public readonly IMongoCollection<EntityRelation> EntityRelation;
        public readonly IMongoCollection<User> User;

        public readonly IMongoCollection<SourceTecDocPC> SourceTecDocPC;
        public readonly IMongoCollection<SourceMMIv8> SourceMMIv8;

        public readonly IMongoCollection<MatchEntity> MatchEntity;
        public readonly IMongoCollection<MatchMakeModel> MatchMakeModel;

        public readonly IMongoCollection<MatchBase> MatchBase;

        public readonly IMongoCollection<MongoSourceEntityModel> SourceTecDocPCModel;
        public readonly IMongoCollection<MongoSourceEntityModel> SourceMMIv8Model;
        //TODO Create MatchRefine View
        //public readonly IMongoCollection<MatchRefine> MatchRefine;

        private static IMongoCollection<T> GetCollection<T>(IMongoDatabase mongoDatabase, string? collectionName = null)
        {
            collectionName ??= typeof(T).Name;
            Log.Information("Connecting to collection {collectionName} [{collectionType}]", collectionName, typeof(T).ToString());
            return mongoDatabase.GetCollection<T>(collectionName);
        }

        public async void CreateViews(IMongoDatabase mongoDatabase, bool regenerate = false)
        {
            if (regenerate)
                await mongoDatabase.DropCollectionAsync("SourceTecDocPCModel");

            var sourceTDModelAggregate = SourceTecDocPC.Aggregate()
                .Sort(new BsonDocument
                    {
                        { "SourceEntityModelHash", 1 },
                        { "Make", 1 },
                        { "SalesDesc", 1 },
                        { "Model", 1 },
                    }
                )
                .Group(new BsonDocument
                    {
                        { "_id", new BsonDocument
                            {
                                { "SourceEntityModelHash", "$SourceEntityModelHash" },
                                { "Make", "$Make" },
                                { "Model", "$SalesDesc" },
                                { "DetailedModel", new BsonDocument
                                    ("$cond", new BsonDocument
                                        {
                                            { "if", new BsonDocument ("$eq", new BsonArray { "$Model", "" }) },
                                            { "then", "$$REMOVE" },
                                            { "else", "$Token_Model" }
                                        }
                                    )
                                }
                            }
                        },
                        { "Popularity", new BsonDocument("$count", new BsonDocument()) }
                    }
                )
                .Group(new BsonDocument
                    {
                        { "_id", new BsonDocument
                            {
                                { "SourceEntityModelHash", "$_id.SourceEntityModelHash" },
                                { "Make", "$_id.Make" },
                                { "Model", "$_id.Model" }
                            }
                        },
                        { "DetailedModels", new BsonDocument("$push", "$_id.DetailedModel") },
                        { "Popularity", new BsonDocument("$sum", "$Popularity") }
                    }
                )
                .Project<MongoSourceEntityModel>(new BsonDocument
                    {
                        { "_id", "$_id.SourceEntityModelHash" },
                        { "Make", "$_id.Make" },
                        { "Model", "$_id.Model" },
                        { "DetailedModels", "$DetailedModels" },
                        { "Popularity", "$Popularity" }
                    }
                );
            var sourceTDModelPipeline = PipelineDefinition<SourceTecDocPC, MongoSourceEntityModel>.Create(sourceTDModelAggregate.Stages);
            await mongoDatabase.CreateViewAsync("SourceTecDocPCModel", "SourceTecDocPC", sourceTDModelPipeline);


            if (regenerate)
                await mongoDatabase.DropCollectionAsync("SourceMMIv8Model");

            var sourceMMIv8ModelAggregate = SourceMMIv8.Aggregate()
                .Sort(new BsonDocument
                    {
                                    { "SourceEntityModelHash", 1 },
                                    { "Manufacturer", 1 },
                                    { "Model", 1 },
                    }
                )
                .Group(new BsonDocument
                    {
                                    { "_id", new BsonDocument
                                        {
                                           { "SourceEntityModelHash", "$SourceEntityModelHash" },
                                           { "Make", "$Manufacturer" },
                                           { "Model", "$Model" }
                                        }
                                    },
                                    { "Popularity", new BsonDocument("$count", new BsonDocument()) }
                    }
                )
                .Group(new BsonDocument
                    {
                                    { "_id", new BsonDocument
                                        {
                                            { "SourceEntityModelHash", "$_id.SourceEntityModelHash" },
                                            { "Make", "$_id.Make" },
                                            { "Model", "$_id.Model" }
                                        }
                                    },
                                    //{ "DetailedModels", new BsonDocument("$push", "$_id.DetailedModel") },
                                    { "Popularity", new BsonDocument("$sum", "$Popularity") }
                    }
                )
                .Project<MongoSourceEntityModel>(new BsonDocument
                    {
                                    { "_id", "$_id.SourceEntityModelHash" },
                                    { "Make", "$_id.Make" },
                                    { "Model", "$_id.Model" },
                                    //{ "DetailedModels", "$DetailedModels" },
                                    { "Popularity", "$Popularity" }
                    }
                );
            var sourceMMIv8ModelPipeline = PipelineDefinition<SourceMMIv8, MongoSourceEntityModel>.Create(sourceMMIv8ModelAggregate.Stages);
            await mongoDatabase.CreateViewAsync("SourceMMIv8Model", "SourceMMIv8", sourceMMIv8ModelPipeline);

            /*
                        //WAS WIP MatchRefine View Creation, can be removed
                        if (regenerate)
                            await mongoDatabase.DropCollectionAsync("MatchRefine");

                        //TODO check if MMIv8EntityId needs to be _id for indexes
                        var matchEntityAggregate = MatchEntity.Aggregate()
                            .Group(new BsonDocument
                                {
                                    { "_id", "$MatchRefine"},
                                }
                            )
                            .ReplaceRoot<MatchRefine>("$_id");
                        var matchRefinePipeline = PipelineDefinition<MatchEntity, MatchRefine>.Create(matchEntityAggregate.Stages);
                        await mongoDatabase.CreateViewAsync("MatchRefine", "MatchEntity", matchRefinePipeline);
            */

        }

        public void CreateAllIndexes(bool regenerate = false)
        {
            #region Match Entity
            var matchEntityIndexBuilder = Builders<MatchEntity>.IndexKeys;
            var matchEntityIndexModels = new List<CreateIndexModel<MatchEntity>>
            {
                //new (matchEntityIndexBuilder.Ascending(c => c.MMIv8Entity.SourceEntityID)
                //                            .Ascending(c => c.TecDocEntity.SourceEntityID),
                //     new() { Name = "MMIv8Entity.SourceEntityID_TecDocEntity.SourceEntityID", Unique = true, Background = true }
                //),
                new (matchEntityIndexBuilder.Ascending(c => c.MMIv8Entity.DocumentId),
                     new() { Name = "MMIv8Entity.SourceEntityID", Unique = false, Background = true }
                ),
                new (matchEntityIndexBuilder.Ascending(c => c.TecDocEntity.DocumentId),
                     new() { Name = "TecDocEntity.SourceEntityID", Unique = false, Background = true }
                ),
                new (matchEntityIndexBuilder.Ascending(c => c.MMIv8Entity.ExternalId),//TODO Remove if change Matchrefine to not use ExternalId
                     new() { Name = "MMIv8Entity.ExternalId", Unique = false, Background = true }
                ),
                new (matchEntityIndexBuilder.Ascending(c => c.TecDocEntity.ExternalId),
                     new() { Name = "TecDocEntity.ExternalId", Unique = false, Background = true }
                ),
                new (matchEntityIndexBuilder.Ascending(c => c.MMIv8Entity.ExternalId)
                                            .Ascending(c => c.TecDocEntity.ExternalId),
                     new() { Name = "MMIv8Entity.ExternalId_TecDocEntity.ExternalId", Unique = true, Background = true }
                ),
                new (matchEntityIndexBuilder.Ascending(c => c.MatchMakeModelMatchID),
                     new() { Name = "MatchMakeModelMatchID", Unique = false, Background = true }
                ),
                new (matchEntityIndexBuilder.Ascending(c => c.DateIntersection.date_Intersection),
                     new() { Name = "DateIntersection.date_Intersection", Unique = false, Background = true }
                ),
                new (matchEntityIndexBuilder.Ascending(c => c.Status.Current.Status),
                     new() { Name = "Status.Current.Status", Unique = false, Background = true }
                ),
                new (matchEntityIndexBuilder.Ascending(c => c.MatchResult.Failed)
                                            .Ascending(c => c.MatchResult.FailCount),
                     new() { Name = "MatchResult.Failed_FailCount", Unique = false, Background = true }
                ),
                //new (matchEntityIndexBuilder.Ascending(c => c.MatchRefine),
                //     new() { Name = "MatchRefine", Unique = false, Background = true }
                //),
                //new (matchEntityIndexBuilder.Ascending(c => c.MatchRefine.IsCheck),
                //     new() { Name = "MatchRefine.IsCheck", Unique = false, Background = true }
                //),
                //new (matchEntityIndexBuilder.Ascending(c => c.MatchRefine.Difference),
                //     new() { Name = "MatchRefine.Difference", Unique = false, Background = true }
                //),
                new (matchEntityIndexBuilder.Ascending(c => c.Matched),
                     new() { Name = "Matched", Unique = false, Background = true }
                ),
                //new (matchEntityIndexBuilder.Ascending(c => c.MMIv8Entity.DocumentId)
                //                            .Descending(c => c.ScoreSum),
                //     new() { Name = "MMIv8Entity.SourceEntityID_ScoreSum", Unique = false, Background = true }
                //),
                new (matchEntityIndexBuilder.Descending(c => c.ScoreSum),
                     new() { Name = "ScoreSum", Unique = false, Background = true }
                ),
            };

            foreach (MatchBaseType matchBaseType in (MatchBaseType[])Enum.GetValues(typeof(MatchBaseType)))
            {
                var indexModel = new CreateIndexModel<MatchEntity>(
                     matchEntityIndexBuilder.Ascending($"EntityComparison.{matchBaseType.ToString()}.MatchBaseMethod")
                                            .Ascending($"EntityComparison.{matchBaseType.ToString()}._id")
                                            .Ascending($"EntityComparison.{matchBaseType.ToString()}.Score"),
                     new() { Name = $"EntityComparison.{matchBaseType.ToString()}._id_MatchBaseMethod_Score", Unique = false, Background = true }
                     );
                matchEntityIndexModels.Add(indexModel);
            }

            CreateIndex(MatchEntity, matchEntityIndexModels, regenerate);
            #endregion

            #region Entity Relation
            var entityRelationIndexBuilder = Builders<EntityRelation>.IndexKeys;
            var entityRelationIndexModels = new List<CreateIndexModel<EntityRelation>>
            {
                new (entityRelationIndexBuilder.Ascending(c => c.VersionID),
                     new() { Name = "VersionID", Unique = false, Background = true }
                ),
                new (entityRelationIndexBuilder.Ascending(c => c.VersionID)
                                               .Ascending(c => c.MMI_V8_Key)
                                               .Ascending(c => c.KTypNr),
                     new() { Name = "VersionID_MMI_V8_Key_KTypNr", Unique = true, Background = true }
                ),
                new (entityRelationIndexBuilder.Ascending(c => c.RelationKey),
                     new() { Name = "RelationKey", Unique = false, Background = true }
                ),
            };

            CreateIndex(EntityRelation, entityRelationIndexModels, regenerate);
            #endregion

            #region Match Base
            var matchBaseIndexBuilder = Builders<MatchBase>.IndexKeys;
            var matchBaseIndexModels = new List<CreateIndexModel<MatchBase>>
            { //TODO Stopped Working
                new (matchBaseIndexBuilder.Ascending(c => c.MatchBaseType),
                     new() { Name = "MatchBaseType", Unique = false, Background = true }
                ),
                new (matchBaseIndexBuilder.Ascending(c => c.MatchBaseType)
                                          .Ascending(c => c.MatchBaseMethod),
                     new() { Name = "MatchBaseType_MatchBaseMethod", Unique = false, Background = true }
                ),
                new (matchBaseIndexBuilder.Ascending(c => c.Status.Current.Status),
                     new() { Name = "Status.Current.Status", Unique = false, Background = true }
                ),
            };

            CreateIndex(MatchBase, matchBaseIndexModels, regenerate);
            #endregion

            #region Match Make Model
            var matchMakeModelIndexBuilder = Builders<MatchMakeModel>.IndexKeys;
            var matchMakeModelIndexModels = new List<CreateIndexModel<MatchMakeModel>>
            {
                new (matchMakeModelIndexBuilder.Ascending(c => c.MMIv8Model.DocumentId)
                                               .Ascending(c => c.TecDocModel.DocumentId),
                     new() { Name = "MMIv8Model.SourceEntityModelHash_TecDocModel.SourceEntityModelHash", Unique = true, Background = true }
                ),
                new (matchMakeModelIndexBuilder.Ascending(c => c.MMIv8Model.DocumentId),
                     new() { Name = "MMIv8Model.SourceEntityModelHash", Unique = false, Background = true }
                ),
                new (matchMakeModelIndexBuilder.Ascending(c => c.TecDocModel.DocumentId),
                     new() { Name = "TecDocModel.SourceEntityModelHash", Unique = false, Background = true }
                ),
                new (matchMakeModelIndexBuilder.Ascending(c => c.Status.Current.Status),
                     new() { Name = "Status.Current.Status", Unique = false, Background = true }
                ),
                new (matchMakeModelIndexBuilder.Ascending(c => c.TextSort),
                     new() { Name = "TextSort", Unique = false, Background = true }
                ),
            };

            CreateIndex(MatchMakeModel, matchMakeModelIndexModels, regenerate);
            #endregion

            #region Source TecDoc PC
            var sourceTecDocPCIndexBuilder = Builders<SourceTecDocPC>.IndexKeys;
            var sourceTecDocPCIndexModels = new List<CreateIndexModel<SourceTecDocPC>>
            {
                new (sourceTecDocPCIndexBuilder.Ascending(c => c.KTypNr),
                     new() { Name = "KTypNr", Unique = true, Background = true }
                ),
                new (sourceTecDocPCIndexBuilder.Ascending(c => c.ExternalId),
                     new() { Name = "ExternalId", Unique = true, Background = true }
                ),
                new (sourceTecDocPCIndexBuilder.Ascending(c => c.SourceEntityModelHash),
                     new() { Name = "SourceEntityModelHash", Unique = false, Background = true }
                ),
                new (sourceTecDocPCIndexBuilder.Ascending(c => c.SourceEntityModelHash)
                                               .Ascending(c => c.Make)
                                               .Ascending(c => c.SalesDesc)
                                               .Ascending(c => c.Model),
                     new() { Name = "SourceEntityModelHash_Make_SalesDesc_Model", Unique = false, Background = true }
                ),
                new (sourceTecDocPCIndexBuilder.Ascending(c => c.Status.Current.Status),
                     new() { Name = "Status.Current.Status", Unique = false, Background = true }
                ),
                new (sourceTecDocPCIndexBuilder.Ascending(c => c.MatchRefine),
                     new() { Name = "MatchRefine", Unique = false, Background = true }
                ),
                new (sourceTecDocPCIndexBuilder.Ascending(c => c.MatchRefine.IsCheck),
                     new() { Name = "MatchRefine.IsCheck", Unique = false, Background = true }
                ),
                new (sourceTecDocPCIndexBuilder.Ascending(c => c.MatchRefine.Difference),
                     new() { Name = "MatchRefine.Difference", Unique = false, Background = true }
                ),
                new (sourceTecDocPCIndexBuilder.Ascending(c => c.TextSort),
                     new() { Name = "TextSort", Unique = false, Background = true }
                ),
            };

            CreateIndex(SourceTecDocPC, sourceTecDocPCIndexModels, regenerate);
            #endregion

            #region Source MMIv8
            var sourceMMIv8IndexBuilder = Builders<SourceMMIv8>.IndexKeys;
            var sourceMMIv8IndexModels = new List<CreateIndexModel<SourceMMIv8>>
            {
                new (sourceMMIv8IndexBuilder.Ascending(c => c.MMI_V8_Key),
                     new() { Name = "MMI_V8_Key", Unique = true, Background = true }
                ),
                new (sourceMMIv8IndexBuilder.Ascending(c => c.ExternalId),
                     new() { Name = "ExternalId", Unique = true, Background = true }
                ),
                new (sourceMMIv8IndexBuilder.Ascending(c => c.SourceEntityModelHash),
                     new() { Name = "SourceEntityModelHash", Unique = false, Background = true }
                ),
                new (sourceMMIv8IndexBuilder.Ascending(c => c.SourceEntityModelHash)
                                            .Ascending(c => c.Manufacturer)
                                            .Ascending(c => c.Model),
                     new() { Name = "SourceEntityModelHash_Manufacturer_Model", Unique = false, Background = true }
                ),
                new (sourceMMIv8IndexBuilder.Ascending(c => c.Status.Current.Status),
                     new() { Name = "Status.Current.Status", Unique = false, Background = true }
                ),
                new (sourceMMIv8IndexBuilder.Ascending(c => c.MatchRefine),
                     new() { Name = "MatchRefine", Unique = false, Background = true }
                ),
                new (sourceMMIv8IndexBuilder.Ascending(c => c.MatchRefine.IsCheck),
                     new() { Name = "MatchRefine.IsCheck", Unique = false, Background = true }
                ),
                new (sourceMMIv8IndexBuilder.Ascending(c => c.MatchRefine.Difference),
                     new() { Name = "MatchRefine.Difference", Unique = false, Background = true }
                ),
                new (sourceMMIv8IndexBuilder.Ascending(c => c.TextSort),
                     new() { Name = "TextSort", Unique = false, Background = true }
                ),
            };

            CreateIndex(SourceMMIv8, sourceMMIv8IndexModels, regenerate);
            #endregion

            #region Version
            var versionIndexBuilder = Builders<Models.Collections.Version>.IndexKeys;
            var versionIndexModels = new List<CreateIndexModel<Models.Collections.Version>>
            {
                new (versionIndexBuilder.Ascending(c => c.VersionNumber),
                     new() { Name = "VersionNumber", Unique = true, Background = true }
                ),
            };

            CreateIndex(Version, versionIndexModels, regenerate);
            #endregion

            #region User
            var userIndexBuilder = Builders<User>.IndexKeys;
            var userIndexModels = new List<CreateIndexModel<User>>
            {
                new (userIndexBuilder.Ascending(c => c.Name),
                     new() { Name = "Name", Unique = true, Background = true }
                ),
            };

            CreateIndex(User, userIndexModels, regenerate);
            #endregion
        }

        public async void CreateIndex<T>(IMongoCollection<T> collection, List<CreateIndexModel<T>> indexes, bool regenerate)
        {
            if (regenerate)
                await collection.Indexes.DropAllAsync();

            IEnumerable<string> createdIndex = await collection.Indexes.CreateManyAsync(indexes);
            Log.Information("Created indexes for {Type} {Indexes}", typeof(T).Name, createdIndex);
        }
    }
}
