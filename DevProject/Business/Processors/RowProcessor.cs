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
                var rowDictionary = BuildRowDictionary(row, columnMap);

                if (!IsEmptyRow(rowDictionary))
                {
                    result.Add(rowDictionary);
                }
            }

            return result;
        }

        private Dictionary<string, CellValue> BuildRowDictionary(IXLRangeRow row, List<Column> columnMap)
        {
            var dictionary = new Dictionary<string, CellValue>();

            foreach (var column in columnMap)
            {
                dictionary[column.Name] = this.cellValueGetter.GetValue(row.Cell(column.ColumnIndex));
            }

            return dictionary;
        }

        private bool IsEmptyRow(Dictionary<string, CellValue> row)
        {
            return row.Values.All(v => v.Type == CellValueType.Empty);
        }
    }
}
