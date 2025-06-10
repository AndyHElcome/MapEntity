using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.Outputs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services.Match
{
    public static class MatchEntityUpdateExtensions
    {
        public static UpdateDefinition<MatchEntity> SetFailedFlag(this UpdateDefinition<MatchEntity> update, bool failed, string? detail = null)
        {
            update = update.Set(c => c.MatchResult.Failed, failed);

            if (failed)
                update = update.Set(c => c.Matched, false);

            if (detail != null)
                update = update.Set(c => c.MatchResult.FailDetail, detail);

            return update;
        }

        public static UpdateDefinition<MatchEntity> SetMatchedFlag(this UpdateDefinition<MatchEntity> update, bool matched, string? detail = null)
        {
            update = update.Set(c => c.Matched, matched);

            if (matched)
                update = update.Set(c => c.MatchResult.Failed, false);

            if (detail != null)
                update = update.Set(c => c.MatchDetail, detail);

            return update;
        }

        public static UpdateDefinition<MatchEntity> SetPreviousMatchedFlag(this UpdateDefinition<MatchEntity> update, bool matched)
        {
            update = update.Set(c => c.MatchResult.PreviousMatch, matched);
            return update;
        }

        //public static UpdateDefinition<MatchEntity> UpdateEntity(this UpdateDefinition<MatchEntity> update, SourceEntity sourceEntity)
        //{
        //    update = update.Set(c => c.TecDocEntity, sourceEntity);
        //    return update;
        //}

        public static UpdateDefinition<MatchEntity> UpdateEntity(this UpdateDefinition<MatchEntity> update, SourceEntity sourceEntity) // TODO Check this works
        {
            update = sourceEntity.SourceIndex switch
            {
                SourceIndex.TecDocPC => update.Set(c => c.TecDocEntity, (SourceTecDocPC)sourceEntity),
                SourceIndex.MMIv8 => update.Set(c => c.MMIv8Entity, (SourceMMIv8)sourceEntity),
                SourceIndex.TecDocEngine => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
            return update;
        }

        public static UpdateDefinition<MatchEntity> UpdateMakeModelMatch(this UpdateDefinition<MatchEntity> update, MatchMakeModel matchMakeModel) // TODO Check this works
        {
            update = update.Set(c => c.MatchMakeModelMatchID, matchMakeModel.DocumentId);
            return update;
        }

        [Obsolete("not in use?")]
        public static UpdateDefinition<MatchEntity> UpdateEntity(this UpdateDefinition<MatchEntity> update, SourceTecDocPC sourceEntity)
        {
            update = update.Set(c => c.TecDocEntity, sourceEntity);
            return update;
        }

        [Obsolete("not in use?")]
        public static UpdateDefinition<MatchEntity> UpdateEntity(this UpdateDefinition<MatchEntity> update, SourceMMIv8 sourceEntity)
        {
            update = update.Set(c => c.MMIv8Entity, sourceEntity);
            return update;
        }

        public static UpdateDefinition<MatchEntity> UpdateMatchRefine(this UpdateDefinition<MatchEntity> update, MatchRefine matchRefine)
        {
            update = update.Set(c => c.MatchRefine, matchRefine);
            return update;
        }

        public static PipelineDefinition<MatchEntity, MatchEntity> UpdateMatchBase(this PipelineDefinition<MatchEntity, MatchEntity> pipeline, MatchBase matchBase)// TODO Try and convert to driver based query maybe once this is its own class
        {
            return pipeline.AppendStage<MatchEntity, MatchEntity, MatchEntity>(@"
                      {
                        $set: { 
                            'EntityComparison." + matchBase.MatchBaseType.ToString() + @"' : " + matchBase.ToBsonDocument() + @" 
                        } 
                      }
                    ");
        }

        public static PipelineDefinition<MatchEntity, MatchEntity> UpdateScoreMatchResult(this PipelineDefinition<MatchEntity, MatchEntity> pipeline)// TODO Try and convert to driver based query
        {
            return pipeline.AppendStage<MatchEntity, MatchEntity, MatchEntity>(@"
                      {
                        $addFields: {
                          input: {$objectToArray: '$EntityComparison'}
                        }
                      }
                    ")
                .AppendStage<MatchEntity, MatchEntity, MatchEntity>(@"
                            {
                            $addFields: {
                                failed: {
                                $filter: {
                                    input: '$input',
                                    cond: { $lte: ['$$this.v.Score', 0] }
                                  }
                                },
                                passed: {
                                $filter: {
                                    input: '$input',
                                    cond: { $gt: ['$$this.v.Score', 0] }
                                  }
                                },
                                perfect: {
                                $filter: {
                                    input: '$input',
                                    cond: {
                                    $gte: [ '$$this.v.Score',  { $convert: { input: '$$this.v.DefaultScore', to: 'decimal' } } ]
                                    }
                                  }
                                }
                              }
                            }
                        ")
                .AppendStage<MatchEntity, MatchEntity, MatchEntity>(@"
                            {
                            $set: {
                              MatchResult: {
                                ComparisonCount: { $size: '$input' },
                                MatchCount: { $size: '$passed' },
                                Failed: { $cond: [ {$ne: [ {$size: '$failed'}, 0 ]}, true, false ] },
                                FailDetail: {
                                  $cond: [ 
                                    {$ne: [ {$size: '$failed'}, 0 ]},
                                    {$reduce: {
                                      input: '$failed',
                                      initialValue: '',
                                      in: {
                                      $concat: [
                                        '$$value',
                                        {$cond: [ {$eq: ['$$value', '']}, 'Failed on DetailValidation Checks on: ', ', ' ]},
                                        '$$this.v.MatchBaseType' ]
                                        }
                                      }
                                    },
                                    null ]
                                  },
                                FailCount: { $size: '$failed' },
                                PerfectCount: { $size: '$perfect' }
                                },
                              ScoreSum: {
                                $reduce: {
                                  input: '$input',
                                  initialValue: 0,
                                  in: { $add: ['$$value', '$$this.v.Score']}
                                  }
                                }
                              }
                            }
                        ")
                .AppendStage<MatchEntity, MatchEntity, MatchEntity>(@"
                            {
                            $set: {
                              ScoreAverage: {
                                    $divide: [ '$ScoreSum', {$size: '$input'} ]
                                }
                              }
                            }
                        ")
                .AppendStage<MatchEntity, MatchEntity, MatchEntity>(@"
                            {
                            $unset: ['input', 'failed', 'passed', 'perfect']
                            }
                        ");
        }
    }
}
