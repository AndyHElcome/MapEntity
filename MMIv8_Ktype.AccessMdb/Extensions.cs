using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Outputs;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Dynamic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml;

namespace MMIv8_Ktype.AccessMdb
{
    public static class Extensions
    {
        private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions().GetJsonSerializerOptions();

        public static ExpandoObject BuildExpando(this ExpandoObject? expando, MatchEntity obj, string? prefix = null)
        {
            prefix = prefix is null ? string.Empty : $"{prefix}_";
            expando ??= new ExpandoObject();

            expando.AddProperty(prefix + nameof(obj.DocumentId), obj.DocumentId)
                   .AddProperty(prefix + nameof(obj.MatchMakeModelMatchID), obj.MatchMakeModelMatchID)
                   //.AddProperty(prefix + nameof(obj.TecDocEntity.KTypNr), obj.TecDocEntity.KTypNr)
                   //.AddProperty(prefix + nameof(obj.MMIv8Entity.MMI_V8_Key), obj.MMIv8Entity.MMI_V8_Key)
                   .BuildExpando(obj.TecDocEntity, prefix + nameof(obj.TecDocEntity))
                   .BuildExpando(obj.MMIv8Entity, prefix + nameof(obj.MMIv8Entity))

                   ;

            foreach (var key in (MatchBaseType[])Enum.GetValues(typeof(MatchBaseType)))
            {
                expando.BuildExpandoScore(obj.EntityComparison[key], prefix + key.ToString());
            }

            expando.BuildExpando(obj.MatchResult, prefix + nameof(obj.MatchResult))
                   .BuildExpando(obj.MatchRefine, prefix + nameof(obj.MatchRefine))
                   .AddProperty(prefix + nameof(obj.ScoreSum), obj.ScoreSum)
                   .AddProperty(prefix + nameof(obj.ScoreAverage), obj.ScoreAverage)
                   .AddProperty(prefix + nameof(obj.IsBest), obj.IsBest)
                   .AddProperty(prefix + nameof(obj.Matched), obj.Matched)
                   .AddProperty(prefix + nameof(obj.MatchDetail), obj.MatchDetail)
                   .AddProperty(prefix + nameof(obj.Status.Current.Status), obj.Status.Current.Status);

            return expando;
        }

        public static ExpandoObject BuildExpando(this ExpandoObject? expando, MatchResult obj, string? prefix = null)
        {
            prefix = $"{prefix}_" ?? string.Empty;
            expando ??= new ExpandoObject();

            expando.AddProperty(prefix + nameof(obj.ComparisonCount), obj.ComparisonCount)
                   .AddProperty(prefix + nameof(obj.MatchCount), obj.MatchCount)
                   .AddProperty(prefix + nameof(obj.PreviousMatch), obj.PreviousMatch)
                   .AddProperty(prefix + nameof(obj.Failed), obj.Failed)
                   .AddProperty(prefix + nameof(obj.FailDetail), obj.FailDetail)
                   .AddProperty(prefix + nameof(obj.FailCount), obj.FailCount)
                   .AddProperty(prefix + nameof(obj.PerfectCount), obj.PerfectCount)
                   .AddProperty(prefix + nameof(obj.IsPerfect), obj.IsPerfect);

            return expando;
        }

        public static ExpandoObject BuildExpando(this ExpandoObject? expando, MatchRefine obj, string? prefix = null)
        {
            prefix = $"{prefix}_" ?? string.Empty;
            expando ??= new ExpandoObject();

            expando.AddProperty(prefix + nameof(obj.PassCount), obj.PassCount)
                   .AddProperty(prefix + nameof(obj.Difference), obj.Difference)
                   .AddProperty(prefix + nameof(obj.IsCheck), obj.IsCheck)
                   .AddProperty(prefix + nameof(obj.BestScore), obj.BestScore);

            return expando;
        }

        public static ExpandoObject BuildExpando(this ExpandoObject? expando, SourceMMIv8 obj, string? prefix = null)
        {
            prefix = $"{prefix}_" ?? string.Empty;
            expando ??= new ExpandoObject();

            expando.AddProperty(prefix + nameof(obj.MMI_V8_Key), obj.MMI_V8_Key)
                   .AddProperty(prefix + nameof(obj.Manufacturer), obj.Manufacturer)
                   .AddProperty(prefix + nameof(obj.Model), obj.Model)
                   .AddProperty(prefix + nameof(obj.SubModel), obj.SubModel)
                   .AddProperty(prefix + nameof(obj.Mark_or_Series), obj.Mark_or_Series)
                   .AddProperty(prefix + nameof(obj.Token_Identifier), obj.Token_Identifier)
                   .AddProperty(prefix + nameof(obj.Engine_Size), obj.Engine_Size)
                   .AddProperty(prefix + nameof(obj.Cylinders), obj.Cylinders)
                   .AddProperty(prefix + nameof(obj.Cylinder_Layout), obj.Cylinder_Layout)
                   .AddProperty(prefix + nameof(obj.Cam), obj.Cam)
                   .AddProperty(prefix + nameof(obj.Valve), obj.Valve)
                   .AddProperty(prefix + nameof(obj.DateRange.Start), obj.DateRange.Start)
                   .AddProperty(prefix + nameof(obj.DateRange.End), obj.DateRange.End)
                   .AddProperty(prefix + nameof(obj.Body), obj.Body)
                   .AddProperty(prefix + nameof(obj.Doors), obj.Doors)
                   .AddProperty(prefix + nameof(obj.Transmission), obj.Transmission)
                   .AddProperty(prefix + nameof(obj.Gears), obj.Gears)
                   .AddProperty(prefix + nameof(obj.Exact_CC), obj.Exact_CC)
                   .AddProperty(prefix + nameof(obj.Drive), obj.Drive)
                   .AddProperty(prefix + nameof(obj.Fuel), obj.Fuel)
                   .AddProperty(prefix + nameof(obj.BHP), obj.BHP)
                   .AddProperty(prefix + nameof(obj.KW), obj.KW)
                   .AddProperty(prefix + nameof(obj.Engine_Code), obj.Engine_Code)
                   .AddProperty(prefix + nameof(obj.Status.Current.Status), obj.Status.Current.Status);

            return expando;
        }

        public static ExpandoObject BuildExpando(this ExpandoObject? expando, SourceTecDocPC obj, string? prefix = null)
        {
            prefix = $"{prefix}_" ?? string.Empty;
            expando ??= new ExpandoObject();

            expando.AddProperty(prefix + nameof(obj.KTypNr), obj.KTypNr)
                   .AddProperty(prefix + nameof(obj.Make), obj.Make)
                   .AddProperty(prefix + nameof(obj.SalesDesc), obj.SalesDesc)
                   .AddProperty(prefix + nameof(obj.Token_Model), obj.Token_Model)
                   .AddProperty(prefix + nameof(obj.ModelGeneration), obj.ModelGeneration)
                   .AddProperty(prefix + nameof(obj.Token_Type), obj.Token_Type)
                   .AddProperty(prefix + nameof(obj.TypeDesc), obj.TypeDesc)
                   .AddProperty(prefix + nameof(obj.DateRange.Start), obj.DateRange.Start)
                   .AddProperty(prefix + nameof(obj.DateRange.End), obj.DateRange.End)
                   .AddProperty(prefix + nameof(obj.KW), obj.KW)
                   .AddProperty(prefix + nameof(obj.PS), obj.PS)
                   .AddProperty(prefix + nameof(obj.Calc_BHP), obj.Calc_BHP)
                   .AddProperty(prefix + nameof(obj.Litre), obj.Litre)
                   .AddProperty(prefix + nameof(obj.Valves), obj.Valves)
                   .AddProperty(prefix + nameof(obj.Calc_Valve), obj.Calc_Valve)
                   .AddProperty(prefix + nameof(obj.Drive), obj.Drive)
                   .AddProperty(prefix + nameof(obj.FuelType), obj.FuelType)
                   .AddProperty(prefix + nameof(obj.BodyType), obj.BodyType)
                   .AddProperty(prefix + nameof(obj.CCTech), obj.CCTech)
                   .AddProperty(prefix + nameof(obj.Exclude), obj.Exclude)
                   .AddProperty(prefix + nameof(obj.Door), obj.Door)
                   .AddProperty(prefix + nameof(obj.Region), obj.Region)
                   .AddProperty(prefix + nameof(obj.LinkedEngineCodes), obj.LinkedEngineCodes)
                   .AddProperty(prefix + nameof(obj.Status.Current.Status), obj.Status.Current.Status);

            return expando;
        }

        public static ExpandoObject BuildExpando(this ExpandoObject? expando, MatchBase obj, string? prefix = null)
        {
            prefix = $"{prefix}_" ?? string.Empty;
            expando ??= new ExpandoObject();

            expando.AddProperty(prefix + nameof(obj.DocumentId), obj.DocumentId)
                   .AddProperty(prefix + nameof(obj.MatchBaseType), obj.MatchBaseType);

            foreach (var p in obj.TecDocEntity)
            {
                GlobalHelpers.AddProperty(expando, $"{prefix}TD_{p.Key}", p.Value);
            }
            foreach (var p in obj.MMIEntity)
            {
                GlobalHelpers.AddProperty(expando, $"{prefix}MMI_{p.Key}", p.Value);
            }

            expando.AddProperty(prefix + nameof(obj.Score), obj.Score)
                   .AddProperty(prefix + nameof(obj.Status.Current.Status), obj.Status.Current.Status);

            return expando;
        }

        public static ExpandoObject BuildExpandoScore(this ExpandoObject? expando, MatchBase obj, string? prefix = null)
        {
            prefix = $"{prefix}_" ?? string.Empty;
            expando ??= new ExpandoObject();

            expando.AddProperty(prefix + nameof(obj.DocumentId), obj.DocumentId)
                   .AddProperty(prefix + nameof(obj.Score), obj.Score);

            return expando;
        }

        public static bool IsGenericList(this object o)
        {
            var oType = o.GetType();
            return (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof(List<>)));
        }

        private static readonly Dictionary<Type, Type> AccessTypeMap = new()
        {
            { typeof(ObjectId), typeof(string) },
            // Add more mappings here as needed
        };

        private static Func<T, object> ToAccessTypeConverter<T>(this T type)
        {
            return type!.GetType() switch
            {
                Type t when t.IsArray || t.IsGenericList() => c => JsonSerializer.Serialize(((IEnumerable)c!).Cast<object>().Select(d => d.ToAccessTypeConverter()(d))),
                Type t when AccessTypeMap.TryGetValue(t, out var toType) => c => Convert.ChangeType(c!, toType),
                _ => c => c!
            };
        }

        private static Func<object, object?> FromAccessTypeConverter(this Type type)
        {
            return type switch
            {
                Type t when t == typeof(ObjectId) => c => ObjectId.Parse(c.ToString()),
                Type t when t.IsArray || t.IsGenericList() => c => JsonSerializer.Deserialize(String.IsNullOrEmpty(c.ToString()) ? "[]" : c.ToString()!, type, JsonSerializerOptions),
                Type t when t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>) => c => String.IsNullOrEmpty(c.ToString()) ? null : Convert.ChangeType(c, Nullable.GetUnderlyingType(type)!),
                _ => c => Convert.ChangeType(c, type)
            };
        }

        public static T ConvertDataRowToObj<T>(this DataRow dataRow, T obj) // TODO maybe implement
            where T : new()
        {
            foreach (var propertyInfo in obj.GetType().GetProperties())
            {
                var typeMap = propertyInfo.PropertyType.FromAccessTypeConverter();
                
                var value = dataRow[ propertyInfo.Name ];

                propertyInfo.SetValue(obj, typeMap(value));
            }

            return obj;
        }

        public static DataRow ConvertObjToDataRow(this DataRow dataRow, Dictionary<string, object> dictionary) // TODO Move
        {
            foreach (var item in dictionary)
            {
                var typeMap = item.Value.ToAccessTypeConverter();

                dataRow[ item.Key ] = typeMap(item.Value);
            }
            return dataRow;
        }

        public static DataRow ConvertObjToDataRow(this DataRow dataRow, object obj) // TODO Move
        {
            Dictionary<string, object> dictionary = new();

            if (obj is ExpandoObject)
                dictionary = ((ExpandoObject)obj).ToDictionary();
            else
                dictionary = GlobalHelpers.ObjToDictionary(obj);

            return dataRow.ConvertObjToDataRow(dictionary);
        }

        public static DataTable ConvertObjToNewDataTable(Dictionary<string, object> dictionary, string tableName, string[] primaryKeyNames) // TODO Move
        {
            DataTable newDataTable = new(tableName);
            newDataTable.Columns.AddRange(
                dictionary.Select(c => new DataColumn(c.Key, c.Value.GetType()))
                          .ToArray()
                );

            var primaryKeys = primaryKeyNames.Select(c => newDataTable.Columns[ c ]!)
                                             .Where(c => c != null)
                                             .ToArray();

            if (primaryKeys.Length > 0)
                newDataTable.PrimaryKey = primaryKeys;

            return newDataTable;
        }

        public static DataTable ConvertObjToNewDataTable(object obj, string tableName, string[] primaryKeyNames) // TODO Move
        {
            Dictionary<string, object> dictionary = new();

            if (obj is ExpandoObject)
                dictionary = ((ExpandoObject)obj).ToDictionary();
            else
                dictionary = GlobalHelpers.ObjToDictionary(obj);

            return ConvertObjToNewDataTable(dictionary, tableName, primaryKeyNames);
        }

        public static DataTable ConvertObjToNewDataTable(ExpandoObject obj, string tableName, string[] primaryKeyNames) // TODO Move
        {
            var dictionary = obj.ToDictionary();
            return ConvertObjToNewDataTable(dictionary, tableName, primaryKeyNames);
        }

        public static string BuildCreateTableSql(DataTable table)
        {
            var columns = new List<string>();

            foreach (DataColumn col in table.Columns)
            {
                string type = col.DataType switch
                {
                    Type t when t == typeof(int) => col.AutoIncrement ? "AUTOINCREMENT" : "INTEGER",
                    Type t when t == typeof(string) => "TEXT(255)",
                    Type t when t == typeof(DateTime) => "DATETIME",
                    Type t when t == typeof(bool) => "YESNO",
                    _ => "TEXT(255)"
                };

                string columnSql = $"[{col.ColumnName}] {type}";

                if (col.Unique || table.PrimaryKey.Contains(col))
                {
                    columnSql += " PRIMARY KEY";
                }

                columns.Add(columnSql);
            }

            return $"CREATE TABLE [{table.TableName}] ({string.Join(", ", columns)})";
        }

        public static string BuildDropTableSql(string tableName)
        {
            return $"DROP TABLE [{tableName}]";            
        }
    }
}
