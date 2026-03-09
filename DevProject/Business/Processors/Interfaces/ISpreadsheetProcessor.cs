namespace DevProject.Business.Processors.Interfaces
{
    using Data.Entities;

    public interface ISpreadsheetProcessor
    {
        ParsedExcelData Process(Stream fileStream, string fileName);
    }
}