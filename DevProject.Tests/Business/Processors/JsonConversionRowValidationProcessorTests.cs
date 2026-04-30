namespace DevProject.Tests.Business.Processors
{
    using DevProject.Business.Processors;
    using DevProject.Data.Entities;

    public class JsonConversionRowValidationProcessorTests : TestBase<JsonConversionRowValidationProcessor>
    {
        [Fact]
        public void ValidateReturnsNoWarningsForValidRows()
        {
            var rows = new List<Dictionary<string, CellValue>>
            {
                new() { ["Name"] = CellValue.FromText("Alpha") }
            };

            var sut = CreateTestSubject();
            var warnings = sut.Validate(rows, new JsonConversionConfig());

            Assert.Empty(warnings);
        }

        [Fact]
        public void ValidateWarnsForCompletelyEmptyRow()
        {
            var rows = new List<Dictionary<string, CellValue>>
            {
                new() { ["Name"] = CellValue.Empty() },
                new() { ["Name"] = CellValue.FromText("Alpha") }
            };

            var sut = CreateTestSubject();
            var warnings = sut.Validate(rows, new JsonConversionConfig());

            Assert.Single(warnings);
            Assert.Contains("Row 1", warnings[0]);
            Assert.Contains("empty", warnings[0]);
        }

        [Fact]
        public void ValidateWarnsForEachEmptyRow()
        {
            var rows = new List<Dictionary<string, CellValue>>
            {
                new() { ["Name"] = CellValue.Empty() },
                new() { ["Name"] = CellValue.Empty() }
            };

            var sut = CreateTestSubject();
            var warnings = sut.Validate(rows, new JsonConversionConfig());

            Assert.Equal(2, warnings.Count);
        }

        [Fact]
        public void ValidateWarnsWhenNameColumnIsEmptyOnDataRow()
        {
            var rows = new List<Dictionary<string, CellValue>>
            {
                new()
                {
                    ["Name"] = CellValue.Empty(),
                    ["Id"] = CellValue.FromNumber(1)
                }
            };
            var config = new JsonConversionConfig { NameColumn = "Name" };

            var sut = CreateTestSubject();
            var warnings = sut.Validate(rows, config);

            Assert.Single(warnings);
            Assert.Contains("name column", warnings[0]);
            Assert.Contains("Name", warnings[0]);
        }

        [Fact]
        public void ValidateDoesNotWarnWhenNameColumnIsPresentAndNonEmpty()
        {
            var rows = new List<Dictionary<string, CellValue>>
            {
                new() { ["Name"] = CellValue.FromText("Alpha"), ["Id"] = CellValue.FromNumber(1) }
            };
            var config = new JsonConversionConfig { NameColumn = "Name" };

            var sut = CreateTestSubject();
            var warnings = sut.Validate(rows, config);

            Assert.Empty(warnings);
        }

        [Fact]
        public void ValidateReturnsEmptyListForEmptyInput()
        {
            var sut = CreateTestSubject();
            var warnings = sut.Validate(new List<Dictionary<string, CellValue>>(), new JsonConversionConfig());

            Assert.Empty(warnings);
        }
    }
}
