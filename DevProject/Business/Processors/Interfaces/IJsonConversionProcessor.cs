namespace DevProject.Business.Processors.Interfaces
{
    using Data.Entities;

    public interface IJsonConversionProcessor
    {
        JsonConversionResult Convert(ParsedExcelData parsedData, JsonConversionConfig config);
    }
}
