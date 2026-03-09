namespace DevProject.Business.Getters.Interfaces
{
    using ClosedXML.Excel;
    using Data.Entities;

    public interface ICellValueGetter
    {
        CellValue GetValue(IXLCell cell);
        bool IsEmpty(IXLCell cell);
    }
}