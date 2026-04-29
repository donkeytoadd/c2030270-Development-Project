namespace DevProject.Business.Getters
{
    using ClosedXML.Excel;
    using Data.Entities;
    using Interfaces;

    public class CellValueGetter : ICellValueGetter
    {
        public CellValue GetValue(IXLCell cell)
        {
            return cell.Value.Type switch
            {
                XLDataType.Number => CellValue.FromNumber(cell.Value.GetNumber()),
                XLDataType.Boolean => CellValue.FromBoolean(cell.Value.GetBoolean()),
                XLDataType.DateTime => CellValue.FromDateTime(cell.Value.GetDateTime()),
                XLDataType.Blank => CellValue.Empty(),
                _ => CellValue.FromText(cell.Value.ToString())
            };
        }

        public bool IsEmpty(IXLCell cell)
        {
            return cell.Value.Type == XLDataType.Blank ||
                   (cell.Value.Type == XLDataType.Text &&
                    string.IsNullOrWhiteSpace(cell.Value.ToString()));
        }
    }
}
