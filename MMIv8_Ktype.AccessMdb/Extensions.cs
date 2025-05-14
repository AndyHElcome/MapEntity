using MMIv8_Ktype.Models;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.AccessMdb
{
    public static class Extensions
    {

        //private static readonly Dictionary<Type, Type> AccessTypeMap = new()
        //{
        //    { typeof(ObjectId), typeof(string) },
        //    // Add more mappings here as needed
        //};

        //private static Type AccessTypeHandler(this Type type)
        //{
        //    return AccessTypeMap.TryGetValue(type, out var mappedType) ? mappedType : type;
        //}

        public static DataRow ConvertObjToDataRow(this DataRow dataRow, object obj ) // TODO Move
        {
            var dictionary = GlobalHelpers.ObjToDictionary(obj);

            foreach (var item in dictionary)
            {
                //var type = dataRow.Table.Columns[ item.Key ].DataType;
                //dataRow[ item.Key ] = Convert.ChangeType(item.Value, dataRow[ item.Key ].GetType());

                dataRow[ item.Key ] = item.Value;
            }

            return dataRow;
        }

        public static DataTable ConvertObjToNewDataTable(object obj, string tableName, string[] primaryKeyNames) // TODO Move
        {
            var dictionary = GlobalHelpers.ObjToDictionary(obj);

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
