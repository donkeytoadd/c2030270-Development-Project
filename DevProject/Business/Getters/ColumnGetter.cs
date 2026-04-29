namespace DevProject.Business.Getters
{
    using ClosedXML.Excel;
    using Data.Entities;
    using Interfaces;

    public class ColumnGetter : IColumnGetter
    {
        public List<Column> Get(IXLRangeRow columnRow, int lastColumnIndex)
        {
            var headers = new List<Column>();

            for (int columnIndex = 1; columnIndex <= lastColumnIndex; columnIndex++)
            {
                var value = columnRow.Cell(columnIndex).Value.ToString().Trim();

                if (!string.IsNullOrEmpty(value))
                {
                    headers.Add(new Column
                    {
                        ColumnIndex = columnIndex,
                        Name = value
                    });
                }
            }

            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var result = new List<Column>();

            foreach (var column in headers)
            {
                string uniqueName;

                if (!seen.ContainsKey(column.Name))
                {
                    seen[column.Name] = 1;
                    uniqueName = column.Name;
                }
                else
                {
                    seen[column.Name]++;
                    uniqueName = $"{column.Name}_{seen[column.Name]}";
                }

                result.Add(new Column
                {
                    ColumnIndex = column.ColumnIndex,
                    Name = uniqueName
                });
            }

            return result;
        }
    }
}
