using Microsoft.Extensions.Hosting;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services
{
    public static class StatusUpdateExtensions
    {
        public static PipelineDefinition<T, T> AppendStatus<T>(this PipelineDefinition<T, T> pipeline, StatusChange statusChange) // TODO Try and convert to driver based query
            where T : IStatusHistory
        {
            var newStatus = statusChange;

            var appendStatus = new BsonDocument("$set",
                    new BsonDocument("Status.History",
                        new BsonDocument("$cond",
                            new BsonArray {
                                new BsonDocument("$and",
                                    new BsonArray {
                                        new BsonDocument( "$ne", new BsonArray { "$Status.Current.Status", newStatus.Status.ToString() } ),
                                        new BsonDocument( "$ne", new BsonArray { new BsonDocument("$ifNull", new BsonArray {"$Status.Current.Detail",""}), newStatus.Detail ?? "" } )
                                    }
                                ),
                                new BsonDocument("$concatArrays",
                                    new BsonArray {
                                        "$Status.History",
                                        new BsonArray { newStatus.ToBsonDocument() }
                                    }
                                ),
                                "$Status.History"
                            }
                        )
                    )
                );

            var calculateCurrent = new BsonDocument("$set", new BsonDocument("Status.Current", new BsonDocument("$last", "$Status.History")));

            return pipeline.AppendStage<T, T, T>(appendStatus)
                           .AppendStage<T, T, T>(calculateCurrent);
        }

        public static PipelineDefinition<IStatusHistory, IStatusHistory> ForceAppendStatus<IStatusHistory>(this PipelineDefinition<IStatusHistory, IStatusHistory> pipeline, StatusChange statusChange)// TODO Try and convert to driver based query
        {
            var newStatus = statusChange;
            //var newStatus = StatusChange.GetStatus(Globals.CurrentVersion, status, detail);

            return pipeline.AppendStage<IStatusHistory, IStatusHistory, IStatusHistory>(@" 
                      {
                        $set:{
                          Status: {
                            History: {
                              $concatArrays: [
                                '$Status.History',
                                [ " + newStatus.ToBsonDocument() + @" ]
                              ]
                            }
                          }
                        }
                      }
                    ")
                .AppendStage<IStatusHistory, IStatusHistory, IStatusHistory>(@" { $set: { 'Status.Current': { $last: '$Status.History' } } } ");
        }

        public static PipelineDefinition<IStatusHistory, IStatusHistory> RemoveStatus<IStatusHistory>(this PipelineDefinition<IStatusHistory, IStatusHistory> pipeline, Status status)// TODO Try and convert to driver based query
        {
            return pipeline.AppendStage<IStatusHistory, IStatusHistory, IStatusHistory>(@" 
                      {
                        $set: {
                          Status: {
                            $cond: [
                              {
                                $eq: [
                                  '$Status.Current.Status',
                                  '" + status.ToString() + @"'
                                ]
                              },
                              {
                                History: {
                                  $slice: [
                                    '$Status.History',
                                    {
                                      $subtract: [
                                        {
                                          $size: '$Status.History'
                                        },
                                        1
                                      ]
                                    }
                                  ]
                                }
                              },
                              '$Status'
                            ]
                          }
                        }
                      }
                    ")
                .AppendStage<IStatusHistory, IStatusHistory, IStatusHistory>(@" { $set: { 'Status.Current': { $last: '$Status.History' } } } ");
        }
    }
}
