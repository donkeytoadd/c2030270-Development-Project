namespace DevProject.Business.Getters.Interfaces
{
    using Data.Entities;

    public interface ITmForumApiDetectorGetter
    {        string? DetectApiId(ParsedExcelData data);
    }
}
