namespace DevProject.Business.Getters.Interfaces
{
    using Data.Entities;

    public interface ITmForumApiDetectorGetter
    {
        ApiDetectionResult DetectApi(ParsedWorkbook workbook);

        string? DetectApiId(ParsedExcelData data);
    }
}
