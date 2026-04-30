namespace DevProject.Tests.Business.Getters
{
    using ClosedXML.Excel;
    using DevProject.Business.Getters;

    public class ColumnGetterTests : TestBase<ColumnGetter>
    {
        private IXLRangeRow BuildHeaderRow(Action<IXLWorksheet> configure)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Sheet1");
            configure(worksheet);
            return worksheet.RangeUsed()!.RowsUsed().First();
        }

        [Fact]
        public void GetReturnsColumnsWithCorrectNamesAndIndexes()
        {
            var mockRow = BuildHeaderRow(worksheet =>
            {
                worksheet.Cell(1, 1).Value = "Id";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(1, 3).Value = "Age";
            });

            var sut = this.CreateTestSubject();

            var result = sut.Get(mockRow, 3);

            Assert.Equal(3, result.Count);

            Assert.Equal("Id", result[0].Name);
            Assert.Equal("Name", result[1].Name);
            Assert.Equal("Age", result[2].Name);
        }

        [Fact]
        public void GetSkipsEmptyHeaderCells()
        {
            var mockRow = BuildHeaderRow(worksheet =>
            {
                worksheet.Cell(1, 1).Value = "Id";
                worksheet.Cell(1, 3).Value = "Name";
            });

            var sut = this.CreateTestSubject();

            var result = sut.Get(mockRow, 3);

            Assert.Equal(2, result.Count);

            Assert.Equal("Id", result[0].Name);
            Assert.Equal("Name", result[1].Name);
        }

        [Fact]
        public void GetDeduplicatesIdenticalColumnNames()
        {
            var mockRow = BuildHeaderRow(worksheet =>
            {
                worksheet.Cell(1, 1).Value = "Name";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(1, 3).Value = "Name";
            });

            var sut = this.CreateTestSubject();

            var result = sut.Get(mockRow, 3);

            Assert.Equal(3, result.Count);
            Assert.Equal("Name", result[0].Name);
            Assert.Equal("Name_2", result[1].Name);
            Assert.Equal("Name_3", result[2].Name);
        }
    }
}
