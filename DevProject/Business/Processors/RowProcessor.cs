namespace DevProject.Business.Processors
{
    using ClosedXML.Excel;
    using Data.Entities;
    using Data.Enums;
    using Getters.Interfaces;
    using Interfaces;

    public class RowProcessor : IRowProcessor
    {
        private readonly ICellValueGetter cellValueGetter;

        public RowProcessor(ICellValueGetter cellValueGetter)
        {
            this.cellValueGetter = cellValueGetter;
        }
        
        public List<Dictionary<string, CellValue>> Process(IEnumerable<IXLRangeRow> dataRows, List<Column> columnMap)
        {
            var result = new List<Dictionary<string, CellValue>>();

            foreach (var row in dataRows)
            {
                var rowDict = BuildRowDictionary(row, columnMap);

                if (!IsEmptyRow(rowDict))
                {
                    result.Add(rowDict);
                }
            }

            return result;
        }
        
        private Dictionary<string, CellValue> BuildRowDictionary(IXLRangeRow row, List<Column> columnMap)
        {
            var dict = new Dictionary<string, CellValue>();

            foreach (var column in columnMap)
            {
                dict[column.Name] = this.cellValueGetter.GetValue(row.Cell(column.ColumnIndex));
            }

            return dict;
        }

        private bool IsEmptyRow(Dictionary<string, CellValue> row)
        {
            return row.Values.All(v => v.Type == CellValueType.Empty);
        }
    }
}