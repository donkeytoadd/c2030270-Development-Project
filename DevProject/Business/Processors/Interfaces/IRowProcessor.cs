namespace DevProject.Business.Processors.Interfaces
{
    using ClosedXML.Excel;
    using Data.Entities;

    public interface IRowProcessor
    {
        List<Dictionary<string, CellValue>> Process(IEnumerable<IXLRangeRow> dataRows, List<Column> columnMap);
    }
}