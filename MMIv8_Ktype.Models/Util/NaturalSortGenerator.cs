using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace MMIv8_Ktype.Models.Util
{

    public class NaturalSortParameters
    {
        public bool StripDash { get; set; }
        public bool StripSpace { get; set; }
        public bool IgnoreDecimalPlaces { get; set; }
    }

    //Stolen from XC4 Xc4.Common.Core/NaturalSortGenerator.cs
    public static class NaturalSortGenerator

    {

        [return: NotNullIfNotNull(nameof(source))]

        public static string? Generate(string? source, NaturalSortParameters? naturalSortParameters = null)

        {

            if (source is null)

                return null;



            var result = source;



            // TODO: Optimise: Avoid excessive string allocation    



            // Remove tabs etc, combine space    

            //   NB. This must come before numeric sort handling:    

            //   -> we must preserve double space in: "F3 Saloon" -> "F000003  Saloon"    

            //   -> so it is sorted before        "F3R Hatchback" -> "F000003 R Hatchback"    

            result = Regex.Replace(result, @"[\p{Z}\p{Cc}]+", " ");



            // Natural/numeric sort:    

            // - Find all: "123", "1.23", "1,23", "1.2.3"    

            // - But not: "1.2,3"    

            result = Regex.Replace(result, @"[0-9]+(([\.,])[0-9]+(\2[0-9]+)*)?", match =>

                {

                    var numericParts = match.Value.Split(',', '.');



                    var ignoreDecimalPlaces = naturalSortParameters?.IgnoreDecimalPlaces == true;



                    if (numericParts.Length == 2 && !ignoreDecimalPlaces)

                    {

                        // Treat as decimal separator    

                        return $"{NaturalSortInteger(numericParts[ 0 ])}{numericParts[ 1 ].TrimEnd('0')} ";

                    }

                    else

                    {

                        // Treat as a series of integers    

                        StringBuilder str = new();

                        for (int i = 0; i < numericParts.Length; i++)

                        {

                            if (i > 0)

                                str.Append(match.Groups[ 2 ].Value); // Preserve separator character    

                            str.Append(NaturalSortInteger(numericParts[ i ]));

                            str.Append(' ');

                        }

                        return str.ToString();

                    }

                });




            // Optionally remove space (except after numbers) - needed by engine index which has inconsistent engine codes    

            if (naturalSortParameters?.StripSpace == true)

                result = Regex.Replace(result, @"(?<![0-9])[ ]+", "");




            // Optionally remove dash - needed by engine index which has inconsistent engine codes    

            if (naturalSortParameters?.StripDash == true)

                result = Regex.Replace(result, @"[\-]+", "");




            // Handle "separator" characters    

            // - Put space before so that separators take priority    

            // - Collapse to single character, so all separator characters are equivalent    

            result = Regex.Replace(result, @"(?<![0-9])[ ]*[()\[\]{},|/;]+[ ]*", " ,");




            // Handle "symbol" characters    

            // - Collapse to single character, so all symbols characters are equivalent    

            result = Regex.Replace(result, @"[^\p{N}\p{L}\p{M} ,]+", "#");




            return result;

        }




        static string NaturalSortInteger(ReadOnlySpan<char> source)

        {

            var trimmedSource = source.TrimStart('0');

            if (trimmedSource.Length == 0)

                trimmedSource = "0";




            // Prefix with the number of digits using variable length encoding (to preserve sort order) eg.    

            // 1 digit   = "0"    

            // 2 digits  = "1"    

            // 9 digits  = "8"    

            // 10 digits = "90"    

            // 11 digits = "91"    

            // 18 digits = "98"    

            // 19 digits = "990"    




            int prefix = trimmedSource.Length - 1;

            if (prefix <= 8)

            {

                return $"{prefix}{trimmedSource}";

            }

            else

            {

                var (extraNines, extraPrefix) = Math.DivRem(prefix, 9);

                return $"{new string('9', extraNines)}{extraPrefix}";

            }

        }

    }
}
