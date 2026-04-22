namespace DevProject.Business.Processors.Interfaces
{
    using Data.Entities;

    public interface IJsonConversionRowValidationProcessor
    {
        List<string> Validate(List<Dictionary<string, CellValue>> rows, JsonConversionConfig config);
    }
}
