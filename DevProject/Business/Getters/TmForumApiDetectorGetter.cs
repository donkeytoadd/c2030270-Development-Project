namespace DevProject.Business.Getters
{
    using System.Text.RegularExpressions;
    using Business.Helpers;
    using Data.Entities;
    using Interfaces;

    public class TmForumApiDetectorGetter : ITmForumApiDetectorGetter
    {
        private static readonly Regex TmfIdPattern =
            new(@"TMF\d{3}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const double WeightSheet    = 3.0;
        private const double WeightColumn   = 2.0;
        private const double WeightFilename = 1.0;
        private const double MaxRaw         = WeightSheet + WeightColumn + WeightFilename;

        private const int AutoSelectMinConfidence = 70;
        private const int AutoSelectMinGap        = 20;

        private readonly IMappingTemplateGetter mappingTemplateGetter;

        public TmForumApiDetectorGetter(IMappingTemplateGetter mappingTemplateGetter) =>
            this.mappingTemplateGetter = mappingTemplateGetter;

        public ApiDetectionResult DetectApi(ParsedWorkbook workbook)
        {
            var templates = mappingTemplateGetter.GetAll();

            var workbookNameNorm = workbook.WorkbookName.ToUpperInvariant();

            var presentSheets = workbook.Sheets
                .Select(s => StringNormalisation.NormaliseSheetName(s.SpreadsheetName))
                .ToHashSet();

            var presentColumns = workbook.Sheets
                .SelectMany(s => s.ColumnNames)
                .Select(StringNormalisation.NormaliseColumnName)
                .ToHashSet();

            var candidates = new List<ApiDetectionCandidate>(templates.Count);

            foreach (var template in templates)
            {
                var expectedSheets = template.SheetMappings
                    .Select(m => StringNormalisation.NormaliseSheetName(m.SheetName))
                    .ToList();

                var matchedSheets = expectedSheets
                    .Where(presentSheets.Contains)
                    .ToList();

                double sheetScore = expectedSheets.Count > 0
                    ? (double)matchedSheets.Count / expectedSheets.Count
                    : 0;

                var expectedCols = template.SheetMappings
                    .SelectMany(m => m.ColumnMappings)
                    .Select(c => c.SourceColumn)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Select(StringNormalisation.NormaliseColumnName)
                    .Distinct()
                    .ToList();

                var matchedCols = expectedCols
                    .Where(presentColumns.Contains)
                    .ToList();

                double colScore = expectedCols.Count > 0
                    ? (double)matchedCols.Count / expectedCols.Count
                    : 0;

                var fileMatch = TmfIdPattern.Match(workbookNameNorm);
                double filenameScore = fileMatch.Success &&
                    fileMatch.Value.Equals(template.ApiId, StringComparison.OrdinalIgnoreCase)
                    ? 1.0 : 0.0;

                double raw        = sheetScore * WeightSheet + colScore * WeightColumn + filenameScore * WeightFilename;
                int    confidence = (int)Math.Round(raw / MaxRaw * 100);

                var matched = new List<string>();
                var missing = new List<string>();

                foreach (var s in expectedSheets)
                    (presentSheets.Contains(s) ? matched : missing).Add($"sheet:{s}");

                if (filenameScore > 0)
                    matched.Add($"filename:{template.ApiId}");

                candidates.Add(new ApiDetectionCandidate
                {
                    ApiId          = template.ApiId,
                    ApiName        = template.ApiName,
                    Confidence     = confidence,
                    MatchedSignals = matched,
                    MissingSignals = missing,
                });
            }

            candidates.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));

            string? autoSelected = null;
            if (candidates.Count > 0)
            {
                var top    = candidates[0];
                var second = candidates.Count > 1 ? candidates[1] : null;
                int gap    = second is null ? 100 : top.Confidence - second.Confidence;

                if (top.Confidence >= AutoSelectMinConfidence && gap >= AutoSelectMinGap)
                    autoSelected = top.ApiId;
            }

            return new ApiDetectionResult
            {
                Candidates       = candidates,
                AutoSelectedApiId = autoSelected,
            };
        }

        public string? DetectApiId(ParsedExcelData data)
        {
            var workbook = new ParsedWorkbook
            {
                WorkbookName = data.SpreadsheetName,
                Sheets       = [data],
            };
            var result = DetectApi(workbook);
            var top    = result.Candidates.FirstOrDefault(c => c.Confidence > 0);
            return top?.ApiId;
        }
    }
}
