namespace DevProject.Tests.Business.Helpers
{
    using DevProject.Business.Helpers;

    public class StringNormalisationTests
    {

        [Theory]
        [InlineData("GivenName", "givenname")]
        [InlineData("Given Name", "givenname")]
        [InlineData("given_name", "givenname")]
        [InlineData("given-name", "givenname")]
        [InlineData("GIVEN NAME", "givenname")]
        [InlineData("given name", "givenname")]
        [InlineData("given__name", "givenname")]
        [InlineData("given--name", "givenname")]
        [InlineData(" given name ", "givenname")]
        public void NormaliseColumnName_CollapsesWhitespaceUnderscoreHyphenAndCase(
            string input, string expected) =>
            Assert.Equal(expected, StringNormalisation.NormaliseColumnName(input));

        [Fact]
        public void NormaliseColumnName_EmptyStringReturnsEmpty() =>
            Assert.Equal(string.Empty, StringNormalisation.NormaliseColumnName(string.Empty));

        [Fact]
        public void NormaliseColumnName_AlreadyNormalisedIsUnchanged() =>
            Assert.Equal("orderid", StringNormalisation.NormaliseColumnName("orderid"));

        [Theory]
        [InlineData("@referredType", "@referredtype")]
        [InlineData("@baseType", "@basetype")]
        public void NormaliseColumnName_PreservesAtPrefix(string input, string expected) =>
            Assert.Equal(expected, StringNormalisation.NormaliseColumnName(input));
        
        [Theory]
        [InlineData("relatedParty", "relatedparty")]
        [InlineData("RelatedParty", "relatedparty")]
        [InlineData("RELATEDPARTY", "relatedparty")]
        [InlineData(" relatedParty", "relatedparty")]
        [InlineData("relatedParty ", "relatedparty")]
        public void NormaliseSheetName_TrimsAndLowercases(string input, string expected) =>
            Assert.Equal(expected, StringNormalisation.NormaliseSheetName(input));

        [Fact]
        public void NormaliseSheetName_PreservesInternalSpaces() =>
            Assert.Equal("spec characteristic", StringNormalisation.NormaliseSheetName("Spec Characteristic"));

        [Fact]
        public void NormaliseSheetName_EmptyStringReturnsEmpty() =>
            Assert.Equal(string.Empty, StringNormalisation.NormaliseSheetName(string.Empty));
        
        [Fact]
        public void NormaliseColumnName_And_NormaliseSheetName_AgreeOnSimpleAlphaStrings()
        {
            const string input = "Overview";
            Assert.Equal(
                StringNormalisation.NormaliseColumnName(input),
                StringNormalisation.NormaliseSheetName(input));
        }
    }
}
