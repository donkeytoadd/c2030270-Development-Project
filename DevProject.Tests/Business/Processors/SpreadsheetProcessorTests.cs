namespace DevProject.Tests.Business.Processors
{
    using ClosedXML.Excel;
    using DevProject.Business.Getters.Interfaces;
    using DevProject.Business.Processors;
    using DevProject.Business.Processors.Interfaces;
    using DevProject.Data.Entities;
    using DevProject.Data.Exceptions;
    using Moq;

    public class SpreadsheetProcessorTests : TestBase<SpreadsheetProcessor>
    {
        private Stream BuildWorkbook(Action<IXLWorksheet> configure, string sheetName = "Sheet1")
        {
            var workbook  = new XLWorkbook();
            var worksheet = workbook.AddWorksheet(sheetName);
            configure(worksheet);
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        [Fact]
        public void ProcessThrowsProcessExcelExceptionWithInvalidStream()
        {
            var invalidStream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02, 0x03 });

            var sut = this.CreateTestSubject();

            Assert.Throws<ProcessExcelException>(() => sut.Process(invalidStream, "bad.xlsx"));
        }

        [Fact]
        public void ProcessReturnsParsedResultWithValidWorkbook()
        {
            var columns = new List<Column>
            {
                new Column
                {
                    ColumnIndex = 1, 
                    Name = "Id"
                },
                new Column 
                { 
                    ColumnIndex = 2, 
                    Name = "Name" 
                }
            };
            
            var expectedRows = new List<Dictionary<string, CellValue>>
            {
                new Dictionary<string, CellValue>()
                {
                    ["Id"] = CellValue.FromNumber(1), 
                    ["Name"] = CellValue.FromText("Alice")
                },
                new Dictionary<string, CellValue>()
                {
                    ["Id"] = CellValue.FromNumber(2), 
                    ["Name"] = CellValue.FromText("Bob")
                }
            };

            this.automocker.GetMock<IColumnGetter>()
                .Setup(x => x.Get(It.IsAny<IXLRangeRow>(), It.IsAny<int>()))
                .Returns(columns);

            this.automocker.GetMock<IRowProcessor>()
                .Setup(x => x.Process(It.IsAny<IEnumerable<IXLRangeRow>>(), It.IsAny<List<Column>>()))
                .Returns(expectedRows);

            var stream = BuildWorkbook(worksheet =>
            {
                worksheet.Cell(1, 1).Value = "Id";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(2, 1).Value = 1;
                worksheet.Cell(2, 2).Value = "Alice";
                worksheet.Cell(3, 1).Value = 2;
                worksheet.Cell(3, 2).Value = "Bob";
            }, "Products");

            var sut = this.CreateTestSubject();

            var result = sut.Process(stream, "products.xlsx");

            var expectedColumns = new List<String>()
            {
                columns[0].Name,
                columns[1].Name,
            };

            Assert.Equal("Products", result.SpreadsheetName);
            Assert.Equal(expectedColumns, result.ColumnNames);
            Assert.Equal(expectedRows, result.Rows);

            this.automocker.GetMock<IRowProcessor>()
                .Verify(x => x.Process(It.IsAny<IEnumerable<IXLRangeRow>>(), columns), Times.Once);
        }

        [Fact]
        public void ProcessReturnsEmptyResultWhenWorksheetEmpty()
        {
            var stream = BuildWorkbook(_ => { }, "EmptySheet");

            var sut = this.CreateTestSubject();

            var result = sut.Process(stream, "empty.xlsx");

            Assert.Equal("EmptySheet", result.SpreadsheetName);
            Assert.Empty(result.ColumnNames);
            Assert.Empty(result.Rows);

            this.automocker.GetMock<IRowProcessor>()
                .Verify(x => x.Process(It.IsAny<IEnumerable<IXLRangeRow>>(), It.IsAny<List<Column>>()), Times.Never);
        }

        [Fact]
        public void ProcessReturnsEmptyResultWithNoColumns()
        {
            this.automocker.GetMock<IColumnGetter>()
                .Setup(x => x.Get(It.IsAny<IXLRangeRow>(), It.IsAny<int>()))
                .Returns(new List<Column>());

            var stream = BuildWorkbook(worksheet =>
            {
                worksheet.Cell(1, 1).Value = "Id";
                worksheet.Cell(2, 1).Value = 1;
            });

            var sut = this.CreateTestSubject();

            var result = sut.Process(stream, "test.xlsx");

            Assert.Empty(result.ColumnNames);
            Assert.Empty(result.Rows);

            this.automocker.GetMock<IRowProcessor>()
                .Verify(x => x.Process(It.IsAny<IEnumerable<IXLRangeRow>>(), It.IsAny<List<Column>>()), Times.Never);
        }
    }
}
