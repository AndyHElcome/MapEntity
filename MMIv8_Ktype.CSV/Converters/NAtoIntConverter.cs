using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MMIv8_Ktype.CSV.Converters
{
    public class NAtoIntConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (int.TryParse(text, out int convertedText))
            {
                return convertedText;
            }
            else
            {
                return 0;
            }
        }
    }
}
