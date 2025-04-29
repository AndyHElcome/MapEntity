using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MMIv8_Ktype.CSV.Converters
{
    public class NAtoDecimalConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (decimal.TryParse(text, out decimal convertedText))
            {
                return convertedText;
            }
            else
            {
                return (decimal)0;
            }
        }
    }
}
