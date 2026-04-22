namespace DevProject.Business.Getters
{
    using System.Text.RegularExpressions;
    using Data.Entities;
    using Interfaces;    public class TmForumApiDetectorGetter : ITmForumApiDetectorGetter
    {
        private static readonly Regex TmfIdPattern =
            new(@"TMF\d{3}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly string[] KnownApiIds =
        {
            "TMF620", "TMF622", "TMF629", "TMF632", "TMF633",
            "TMF634", "TMF637", "TMF638", "TMF639", "TMF641", "TMF651", "TMF666"
        };

        public string? DetectApiId(ParsedExcelData data)
        {            var match = TmfIdPattern.Match(data.SpreadsheetName);
            if (match.Success)
            {
                var candidate = match.Value.ToUpperInvariant();
                if (KnownApiIds.Contains(candidate))
                    return candidate;
            }
            var cols = data.ColumnNames
                .Select(c => Regex.Replace(c.ToLowerInvariant(), @"[\s_\-]+", ""))
                .ToList();
            if (cols.Any(c => c.Contains("orderid") || c.Contains("orderdate") || c.Contains("ordernumber")))
                return "TMF622";
            if (cols.Any(c => c.Contains("givenname") || c.Contains("firstname"))
                && cols.Any(c => c.Contains("familyname") || c.Contains("lastname")))
                return "TMF632";
            if (cols.Any(c => c.Contains("operationalstate") || c.Contains("administrativestate") || c.Contains("managementstate")))
                return "TMF639";
            if (cols.Any(c => c.Contains("servicestate") || c.Contains("servicechar")))
                return "TMF638";

            return null;
        }
    }
}
