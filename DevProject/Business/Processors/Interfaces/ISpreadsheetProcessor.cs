namespace DevProject.Business.Processors.Interfaces
{
    using Data.Entities;

    public interface ISpreadsheetProcessor
    {
        ParsedWorkbook Process(Stream fileStream, string fileName);
    }
}