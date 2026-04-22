namespace DevProject.Business.Processors.Interfaces
{
    using Data.Entities;

    public interface IWorkbookConversionProcessor
    {
        WorkbookConversionResult Convert(ParsedWorkbook workbook, MappingTemplate template);
    }
}
