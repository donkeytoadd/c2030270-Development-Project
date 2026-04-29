namespace DevProject.Infrastructure
{ public interface IConversionResultStore
    {
        string Store(string filename, string json); bool TryGet(string resultId, out string? filename, out string? json);
    }
}
