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
            var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet(sheetName);
            configure(worksheet);
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        private Stream BuildMultiSheetWorkbook(params (string name, Action<IXLWorksheet> configure)[] sheets)
        {
            var workbook = new XLWorkbook();
            foreach (var (name, configure) in sheets)
            {
                var ws = workbook.AddWorksheet(name);
                configure(ws);
            }
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
                new Column { ColumnIndex = 1, Name = "Id" },
                new Column { ColumnIndex = 2, Name = "Name" }
            };

            var expectedRows = new List<Dictionary<string, CellValue>>
            {
                new Dictionary<string, CellValue>
                {
                    ["Id"] = CellValue.FromNumber(1),
                    ["Name"] = CellValue.FromText("Alice")
                },
                new Dictionary<string, CellValue>
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

            Assert.Equal(1, result.TotalSheets);
            Assert.Equal("products", result.WorkbookName);

            var sheet = result.Sheets[0];
            Assert.Equal("Products", sheet.SpreadsheetName);
            Assert.Equal(new List<string> { "Id", "Name" }, sheet.ColumnNames);
            Assert.Equal(expectedRows, sheet.Rows);

            this.automocker.GetMock<IRowProcessor>()
                .Verify(x => x.Process(It.IsAny<IEnumerable<IXLRangeRow>>(), columns), Times.Once);
        }

        [Fact]
        public void ProcessReturnsEmptyResultWhenWorksheetEmpty()
        {
            var stream = BuildWorkbook(_ => { }, "EmptySheet");

            var sut = this.CreateTestSubject();
            var result = sut.Process(stream, "empty.xlsx");

            Assert.Equal(1, result.TotalSheets);
            var sheet = result.Sheets[0];
            Assert.Equal("EmptySheet", sheet.SpreadsheetName);
            Assert.Empty(sheet.ColumnNames);
            Assert.Empty(sheet.Rows);

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

            Assert.Equal(1, result.TotalSheets);
            Assert.Empty(result.Sheets[0].ColumnNames);
            Assert.Empty(result.Sheets[0].Rows);

            this.automocker.GetMock<IRowProcessor>()
                .Verify(x => x.Process(It.IsAny<IEnumerable<IXLRangeRow>>(), It.IsAny<List<Column>>()), Times.Never);
        }

        [Fact]
        public void ProcessReturnsAllSheetsForMultiSheetWorkbook()
        {
            var columns = new List<Column>
            {
                new Column { ColumnIndex = 1, Name = "Name" }
            };
            var rows = new List<Dictionary<string, CellValue>>
            {
                new Dictionary<string, CellValue> { ["Name"] = CellValue.FromText("Alice") }
            };

            this.automocker.GetMock<IColumnGetter>()
                .Setup(x => x.Get(It.IsAny<IXLRangeRow>(), It.IsAny<int>()))
                .Returns(columns);

            this.automocker.GetMock<IRowProcessor>()
                .Setup(x => x.Process(It.IsAny<IEnumerable<IXLRangeRow>>(), It.IsAny<List<Column>>()))
                .Returns(rows);

            var stream = BuildMultiSheetWorkbook(
                ("Alpha", ws => { ws.Cell(1, 1).Value = "Name"; ws.Cell(2, 1).Value = "Alice"; }),
                ("Beta", ws => { ws.Cell(1, 1).Value = "Name"; ws.Cell(2, 1).Value = "Bob"; }),
                ("Gamma", ws => { ws.Cell(1, 1).Value = "Name"; ws.Cell(2, 1).Value = "Carol"; }));

            var sut = this.CreateTestSubject();
            var result = sut.Process(stream, "multi.xlsx");

            Assert.Equal(3, result.TotalSheets);
        }

        [Fact]
        public void ProcessPreservesSheetOrderForMultiSheetWorkbook()
        {
            var columns = new List<Column> { new Column { ColumnIndex = 1, Name = "Name" } };
            var rows = new List<Dictionary<string, CellValue>>();

            this.automocker.GetMock<IColumnGetter>()
                .Setup(x => x.Get(It.IsAny<IXLRangeRow>(), It.IsAny<int>()))
                .Returns(columns);

            this.automocker.GetMock<IRowProcessor>()
                .Setup(x => x.Process(It.IsAny<IEnumerable<IXLRangeRow>>(), It.IsAny<List<Column>>()))
                .Returns(rows);

            var stream = BuildMultiSheetWorkbook(
                ("First", ws => ws.Cell(1, 1).Value = "Name"),
                ("Second", ws => ws.Cell(1, 1).Value = "Name"),
                ("Third", ws => ws.Cell(1, 1).Value = "Name"));

            var sut = this.CreateTestSubject();
            var result = sut.Process(stream, "order.xlsx");

            Assert.Equal("First", result.Sheets[0].SpreadsheetName);
            Assert.Equal("Second", result.Sheets[1].SpreadsheetName);
            Assert.Equal("Third", result.Sheets[2].SpreadsheetName);
        }

        [Fact]
        public void ProcessIncludesEmptySheetAmongValidSheets()
        {
            var columns = new List<Column> { new Column { ColumnIndex = 1, Name = "Name" } };
            var rows = new List<Dictionary<string, CellValue>>
            {
                new Dictionary<string, CellValue> { ["Name"] = CellValue.FromText("Alice") }
            };

            this.automocker.GetMock<IColumnGetter>()
                .Setup(x => x.Get(It.IsAny<IXLRangeRow>(), It.IsAny<int>()))
                .Returns(columns);

            this.automocker.GetMock<IRowProcessor>()
                .Setup(x => x.Process(It.IsAny<IEnumerable<IXLRangeRow>>(), It.IsAny<List<Column>>()))
                .Returns(rows);

            var stream = BuildMultiSheetWorkbook(
                ("Data", ws => { ws.Cell(1, 1).Value = "Name"; ws.Cell(2, 1).Value = "Alice"; }),
                ("Empty", ws => { }),
                ("More", ws => { ws.Cell(1, 1).Value = "Name"; ws.Cell(2, 1).Value = "Bob"; }));

            var sut = this.CreateTestSubject();
            var result = sut.Process(stream, "test.xlsx");

            Assert.Equal(3, result.TotalSheets);
            Assert.Equal("Empty", result.Sheets[1].SpreadsheetName);
            Assert.Empty(result.Sheets[1].ColumnNames);
            Assert.Empty(result.Sheets[1].Rows);
        }

        [Fact]
        public void ProcessIncludesHeadersOnlySheetWithColumnsAndEmptyRows()
        {
            var columns = new List<Column>
            {
                new Column { ColumnIndex = 1, Name = "Name" },
                new Column { ColumnIndex = 2, Name = "Value" }
            };

            this.automocker.GetMock<IColumnGetter>()
                .Setup(x => x.Get(It.IsAny<IXLRangeRow>(), It.IsAny<int>()))
                .Returns(columns);

            this.automocker.GetMock<IRowProcessor>()
                .Setup(x => x.Process(It.IsAny<IEnumerable<IXLRangeRow>>(), It.IsAny<List<Column>>()))
                .Returns(new List<Dictionary<string, CellValue>>());
            var stream = BuildWorkbook(ws =>
            {
                ws.Cell(1, 1).Value = "Name";
                ws.Cell(1, 2).Value = "Value";
            }, "HeadersOnly");

            var sut = this.CreateTestSubject();
            var result = sut.Process(stream, "headers.xlsx");

            var sheet = result.Sheets[0];
            Assert.Equal("HeadersOnly", sheet.SpreadsheetName);
            Assert.Equal(new List<string> { "Name", "Value" }, sheet.ColumnNames);
            Assert.Empty(sheet.Rows);
        }
    }
}
