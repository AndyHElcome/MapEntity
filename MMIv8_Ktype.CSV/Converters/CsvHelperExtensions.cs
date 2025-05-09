using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MMIv8_Ktype.CSV.Converters
{
    public static class CsvHelperExtensions
    {
        public static T GetConverted<T>(this IReaderRow row, string columnName, ITypeConverter converter)
        {
            var raw = row.GetField(columnName);
            return (T)converter.ConvertFromString(raw, row, new MemberMapData(null));
        }
    }
}
