namespace DevProject.Business.Getters
{
    using Data.Entities;
    using Interfaces;
    using System.Text.Json.Nodes;
    public class TmForumResourceGetter : ITmForumResourceGetter
    {
        private readonly IEnumerable<IResourceMappingGetter> _strategies;

        public TmForumResourceGetter(IEnumerable<IResourceMappingGetter> strategies)
        {
            _strategies = strategies;
        }

        public List<JsonNode> GetAll(ParsedExcelData data, TmForumApiDefinition apiDef, JsonConversionConfig config)
        {
            var strategy = _strategies.FirstOrDefault(s => s.Supports(apiDef.MappingStrategy))
                ?? throw new InvalidOperationException(
                    $"No mapping strategy registered for '{apiDef.MappingStrategy}'. " +
                    $"Ensure a {nameof(IResourceMappingGetter)} that supports this strategy is registered.");

            return strategy.GetAll(data, apiDef, config);
        }
    }
}