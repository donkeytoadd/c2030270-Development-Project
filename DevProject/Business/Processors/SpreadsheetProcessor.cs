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
        private readonly IColumnGetter columnGetter;
        private readonly IRowProcessor rowProcessor;

        public SpreadsheetProcessor(
            IColumnGetter columnGetter,
            IRowProcessor rowProcessor)
        {
            this.columnGetter = columnGetter;
            this.rowProcessor = rowProcessor;
        }
        
        public ParsedExcelData Process(Stream fileStream, string fileName)
        {
            XLWorkbook workbook;
            
            try
            {
                workbook = new XLWorkbook(fileStream);
            }
            catch (Exception ex)
            {
                throw new ProcessExcelException($"The file '{fileName}' could not be read. Please ensure it is a valid, non-password-protected Excel workbook (.xlsx).", ex);
            }

            using (workbook)
            {
                var worksheet = workbook.Worksheets.FirstOrDefault() ?? throw new ProcessExcelException($"The file {fileName} contains no worksheets.");

                var usedRange = worksheet.RangeUsed();

                if (usedRange == null)
                {
                    return new ParsedExcelData
                    {
                        SpreadsheetName =  worksheet.Name,
                        ColumnNames = new List<string>(),
                        Rows = new List<Dictionary<string, CellValue>>()
                    };
                }

                var usedRows = usedRange.RowsUsed().ToList();
                if (usedRows.Count == 0)
                {
                    return new ParsedExcelData
                    {
                        SpreadsheetName = worksheet.Name,
                        ColumnNames = new List<string>(),
                        Rows = new List<Dictionary<string, CellValue>>()
                    };
                }
                
                var lastColumn = usedRange.LastColumn().ColumnNumber();
                var columns = this.columnGetter.Get(usedRows[0], lastColumn);

                if (columns.Count == 0)
                {
                    return new ParsedExcelData
                    {
                        SpreadsheetName = worksheet.Name,
                        ColumnNames = new List<string>(),
                        Rows = new List<Dictionary<string, CellValue>>()
                    }; 
                }
                
                var rows = this.rowProcessor.Process(usedRows.Skip(1), columns);
                
                return new ParsedExcelData
                {
                    SpreadsheetName = worksheet.Name,
                    ColumnNames   = columns.Select(c => c.Name).ToList(),
                    Rows = rows,
                    TotalRows = rows.Count
                };
            }
        }
    }
}