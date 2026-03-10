namespace DevProject.Tests.Business.Processors
{
    using ClosedXML.Excel;
    using DevProject.Business.Getters.Interfaces;
    using DevProject.Business.Processors;
    using DevProject.Data.Entities;
    using Moq;

    public class RowProcessorTests : TestBase<RowProcessor>
    {
        private IEnumerable<IXLRangeRow> BuildDataRows(Action<IXLWorksheet> configure)
        {
            var workbook  = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Sheet1");
            configure(worksheet);
            return worksheet.RangeUsed().RowsUsed().Skip(1);
        }

        [Fact]
        public void ProcessReturnsRowDictionariesAndCallsCellValueGetterForEachCell()
        {
            var columns = new List<Column>
            {
                new Column { ColumnIndex = 1, Name = "Id" },
                new Column { ColumnIndex = 2, Name = "Name" }
            };

            var idValue   = CellValue.FromNumber(1);
            var nameValue = CellValue.FromText("Alice");

            this.automocker.GetMock<ICellValueGetter>()
                .SetupSequence(x => x.GetValue(It.IsAny<IXLCell>()))
                .Returns(idValue)
                .Returns(nameValue);

            var dataRows = BuildDataRows(worksheet =>
            {
                worksheet.Cell(1, 1).Value = "Id";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(2, 1).Value = 1;
                worksheet.Cell(2, 2).Value = "Alice";
            });

            var sut = this.CreateTestSubject();

            var result = sut.Process(dataRows, columns);

            Assert.Single(result);
            Assert.Equal(idValue, result[0]["Id"]);
            Assert.Equal(nameValue, result[0]["Name"]);

            this.automocker.GetMock<ICellValueGetter>()
                .Verify(x => x.GetValue(It.IsAny<IXLCell>()), Times.Exactly(2));
        }

        [Fact]
        public void ProcessSkipsEmptyRowsAndRetainsNonEmptyRows()
        {
            var columns = new List<Column>
            {
                new Column { ColumnIndex = 1, Name = "Id" }
            };

            var nonEmptyValue = CellValue.FromNumber(1);

            this.automocker.GetMock<ICellValueGetter>()
                .SetupSequence(x => x.GetValue(It.IsAny<IXLCell>()))
                .Returns(nonEmptyValue)
                .Returns(CellValue.Empty());

            var dataRows = BuildDataRows(ws =>
            {
                ws.Cell(1, 1).Value = "Id";
                ws.Cell(2, 1).Value = 1;
                ws.Cell(3, 1).Value = 2;
            });

            var sut = this.CreateTestSubject();

            var result = sut.Process(dataRows, columns);

            Assert.Single(result);
            Assert.Equal(nonEmptyValue, result[0]["Id"]);
        }

        [Fact]
        public void ProcessReturnsEmptyListWithNoDataRows()
        {
            var columns = new List<Column>
            {
                new Column { ColumnIndex = 1, Name = "Id" }
            };

            var sut = this.CreateTestSubject();

            var result = sut.Process(Enumerable.Empty<IXLRangeRow>(), columns);

            Assert.Empty(result);

            this.automocker.GetMock<ICellValueGetter>()
                .Verify(x => x.GetValue(It.IsAny<IXLCell>()), Times.Never);
        }
    }
}
