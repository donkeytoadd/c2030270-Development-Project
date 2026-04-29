namespace DevProject.Infrastructure
{
    using System.Collections.Concurrent;
    public class ConversionResultStore : IConversionResultStore
    {
        private static readonly ConcurrentDictionary<string, (string Filename, string Json)> Cache =
            new(StringComparer.Ordinal);

        public string Store(string filename, string json)
        {
            var id = Guid.NewGuid().ToString();
            Cache[id] = (filename, json);
            return id;
        }

        public bool TryGet(string resultId, out string? filename, out string? json)
        {
            if (Cache.TryGetValue(resultId, out var entry))
            {
                filename = entry.Filename;
                json = entry.Json;
                return true;
            }

            filename = null;
            json = null;
            return false;
        }
    }
}
