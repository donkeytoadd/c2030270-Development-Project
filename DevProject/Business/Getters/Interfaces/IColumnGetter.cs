namespace DevProject.Business.Getters.Interfaces
{
    using ClosedXML.Excel;
    using Data.Entities;

    public interface IColumnGetter
    {
        List<Column> Get(IXLRangeRow columnRow, int lastColumnIndex);
    }
}