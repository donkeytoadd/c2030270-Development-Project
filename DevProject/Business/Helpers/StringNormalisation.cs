namespace DevProject.Business.Helpers
{
    using System.Text.RegularExpressions;

    public static class StringNormalisation
    {
        private static readonly Regex ColStripPattern =
            new(@"[\s_\-]+", RegexOptions.Compiled);

        public static string NormaliseColumnName(string value) =>
            ColStripPattern.Replace(value.ToLowerInvariant(), string.Empty);

        public static string NormaliseSheetName(string value) =>
            value.Trim().ToLowerInvariant();
    }
}
