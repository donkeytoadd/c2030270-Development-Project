namespace DevProject.Business.Processors
{
    using ClosedXML.Excel;
    using Data.Entities;
    using Data.Exceptions;
    using Getters.Interfaces;
    using Interfaces;
    using CellValue = Data.Entities.CellValue;

    public class SpreadsheetProcessor : ISpreadsheetProcessor
    {
        private readonly ILogger<SpreadsheetProcessor> logger;
        private readonly IColumnGetter columnGetter;
        private readonly IRowProcessor rowProcessor;

        public SpreadsheetProcessor(
            ILogger<SpreadsheetProcessor> logger,
            IColumnGetter columnGetter,
            IRowProcessor rowProcessor)
        {
            this.logger = logger;
            this.columnGetter = columnGetter;
            this.rowProcessor = rowProcessor;
        }

        public ParsedWorkbook Process(Stream fileStream, string fileName)
        {
            XLWorkbook workbook;

            try
            {
                workbook = new XLWorkbook(fileStream);
            }
            catch (Exception ex)
            {
                this.logger.LogInformation("Error processing workbook with the file stream.");
                throw new ProcessExcelException(
                    $"The file '{fileName}' could not be read. Please ensure it is a valid, non-password-protected Excel workbook (.xlsx).", ex);
            }

            using (workbook)
            {
                if (!workbook.Worksheets.Any())
                    throw new ProcessExcelException($"The file '{fileName}' contains no worksheets.");

                var sheets = workbook.Worksheets
                    .Select(ws => this.ParseSheet(ws))
                    .ToList();

                return new ParsedWorkbook
                {
                    WorkbookName = Path.GetFileNameWithoutExtension(fileName),
                    Sheets       = sheets
                };
            }
        }

        private ParsedExcelData ParseSheet(IXLWorksheet worksheet)
        {
            var usedRange = worksheet.RangeUsed();

            if (usedRange is null)
                return EmptySheet(worksheet.Name);

            var usedRows = usedRange.RowsUsed().ToList();
            if (usedRows.Count == 0)
                return EmptySheet(worksheet.Name);

            var lastColumn = usedRange.LastColumn().ColumnNumber();
            var columns    = this.columnGetter.Get(usedRows[0], lastColumn);

            if (columns.Count == 0)
                return EmptySheet(worksheet.Name);

            var rows = this.rowProcessor.Process(usedRows.Skip(1), columns);

            return new ParsedExcelData
            {
                SpreadsheetName = worksheet.Name,
                ColumnNames     = columns.Select(c => c.Name).ToList(),
                Rows            = rows,
                TotalRows       = rows.Count
            };
        }

        private static ParsedExcelData EmptySheet(string name) =>
            new ParsedExcelData
            {
                SpreadsheetName = name,
                ColumnNames     = new List<string>(),
                Rows            = new List<Dictionary<string, CellValue>>()
            };
    }
}