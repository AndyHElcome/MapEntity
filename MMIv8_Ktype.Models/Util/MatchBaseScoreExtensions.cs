using MongoDB.Driver;

namespace MMIv8_Ktype.Models.Util
{
    internal static class MatchBaseScoreExtensions
    {

        public static decimal CalculateScoreDate(double dateTDCoverage, double dateMMICoverage, double dateInverseIntersection)
        {
            double dateCoverage = 0.600 * (Math.Max(dateTDCoverage, dateMMICoverage) / 100);
            double dateInverse = 0.500 - dateInverseIntersection / 100;

            return ConvertScore(dateCoverage) + ConvertScore(dateInverse);
        }
        public static decimal CalculateScoreDate2(double dateTDCoverage, double dateMMICoverage, double dateInverseIntersection)
        {
            double dateCoverage = Math.Max(dateTDCoverage, dateMMICoverage) / 100;
            double dateInverse = 0.100 * (1 - Math.Abs(dateInverseIntersection) / 120);

            return ConvertScore(dateCoverage) + ConvertScore(dateInverse);
        }

        public static decimal CalculateScoreDate3(double dateTDCoverage, double dateTDSpan, double dateMMICoverage, double dateMMISpan, double dateIntersection, double dateInverseIntersection)
        {
            double minSpan = Math.Min(dateTDSpan, dateMMISpan);
            double intersectionTolerance = minSpan <= 12 ? 3 : minSpan * 0.4;

            if (dateIntersection < intersectionTolerance)
                return 0;

            double dateCoverage = Math.Max(dateTDCoverage, dateMMICoverage) / 100;
            double dateInverse = 0.100 * (1 - Math.Abs(dateInverseIntersection) / 120);

            return ConvertScore(dateCoverage) + ConvertScore(dateInverse);
        }

        public static decimal CalculateScoreEngine(string? TDEngineCode, string? MMIEngineCode)
        {
            if (string.IsNullOrWhiteSpace(TDEngineCode) || string.IsNullOrWhiteSpace(MMIEngineCode))
                return (decimal)1.1;

            string cleanTDEngineCode = TDEngineCode.RemoveSpecialCharacters();
            string cleanMMIEngineCode = MMIEngineCode.RemoveSpecialCharacters();

            if (cleanTDEngineCode == cleanMMIEngineCode)
                return (decimal)1.1;

            char[] splitChars = [ '(', ')', '/' ];

            string[] TDEngineCodeArray = TDEngineCode.RemoveSpecialCharacters(splitChars).Split(splitChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string[] MMIEngineCodeArray = MMIEngineCode.RemoveSpecialCharacters(splitChars).Split(splitChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            int matches = TDEngineCodeArray.Intersect(MMIEngineCodeArray).Count();
            if (matches > 0)
                return (decimal)1.1 - Convert.ToDecimal(Math.Min(TDEngineCodeArray.Length, MMIEngineCodeArray.Length) - matches) / 100;

            string lcs = GlobalHelpers.LongestCommonSubsequence(cleanTDEngineCode, cleanMMIEngineCode, out _, out _);
            if (lcs.Length > 0)
                return ConvertScore(1 * (Convert.ToDouble(lcs.Length) / Convert.ToDouble(Math.Min(cleanTDEngineCode.Length, cleanMMIEngineCode.Length))));

            char[] TDEngineCodeChar = cleanTDEngineCode.ToCharArray();
            char[] MMIEngineCodeChar = cleanMMIEngineCode.ToCharArray();

            matches = TDEngineCodeChar.Intersect(MMIEngineCodeChar).Count();
            if (matches > 0)
                return ConvertScore(0.7 - Convert.ToDouble(TDEngineCodeChar.Length + MMIEngineCodeChar.Length - matches) / 100);

            return 0;
        }

        public static decimal CalculateScoreEngineMultiple(string? TDEngineCode, string? MMIEngineCode)
        {
            if (string.IsNullOrWhiteSpace(TDEngineCode) || string.IsNullOrWhiteSpace(MMIEngineCode))
                return (decimal)1.1;

            char[] splitChars = [ '|' ];
            string[] TDEngineCodeArray = TDEngineCode.Split(splitChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            List<decimal> scores = [ (decimal)0.1 ];
            foreach (var tdEngineCode in TDEngineCodeArray)
            {
                var score = CalculateScoreEngine(tdEngineCode, MMIEngineCode);
                if (score == (decimal)1.1)
                    return score;
                scores.Add(score);
            }

            return scores.Max();
        }

        /// <summary>
        /// Calculates the score between the max difference using the scale
        /// 1.1 * ((<paramref name="difference"/> / <paramref name="max"/>) ^ <paramref name="scale"/>)
        /// </summary>
        /// <param name="difference">The difference to be scored, will be made absolute</param>
        /// <param name="max">The max <paramref name="difference"/> allowed before receving a score of 0</param>
        /// <param name="scale">The gradient of the curve. below 1 initial sharp drop; 1 Linear; above 1 inital little decline</param>
        /// <param name="floor">Minimum score</param>
        public static decimal CalculateScoreGradient(int difference, double max, double scale = 1, double floor = 0) => ConvertScore(1.1 * (1 - Math.Pow(Math.Abs(difference) / max, scale)), floor);

        public static decimal CalculateScoreText(string?[] tecDocPC, string?[] mmiv8, double floor = 0)//TODO Get this right for body and stuff
        {
            var tdValues = tecDocPC.Where(c => !string.IsNullOrEmpty(c)).Select(c => c!.RemoveSpecialCharacters());
            if (!tdValues.Any())
                return ConvertScore(0.8, floor);

            var mmiValues = mmiv8.Where(c => !string.IsNullOrEmpty(c)).Select(c => c!.RemoveSpecialCharacters());
            if (!mmiValues.Any())
                return ConvertScore(0.8, floor);

            int matches = tdValues.Intersect(mmiValues).Count();

            if (matches >= Math.Max(tdValues.Count(), mmiValues.Count()))
                return ConvertScore(1.1, floor);

            if (matches > 0)
                return ConvertScore(1, floor);

            return ConvertScore(floor);
        }

        public static decimal ConvertScore(double score, double floor = 0) => score < floor ? Convert.ToDecimal(floor) : Convert.ToDecimal(score);
    }
}