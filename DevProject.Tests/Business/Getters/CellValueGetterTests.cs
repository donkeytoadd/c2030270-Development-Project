namespace DevProject.Tests.Business.Getters
{
    using ClosedXML.Excel;
    using DevProject.Business.Getters;
    using DevProject.Data.Enums;

    public class CellValueGetterTests : TestBase<CellValueGetter>
    {
        private IXLCell BuildCell(Action<IXLCell> configure)
        {
            var workbook  = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Sheet1");
            var cell = worksheet.Cell(1, 1);
            
            configure(cell);
            return cell;
        }

        [Fact]
        public void GetValueReturnsCorrectTypeForEachCell()
        {
            var mockNumber = BuildCell(cell => cell.Value = 42.0);
            var mockString = BuildCell(cell => cell.Value = "Hello");
            var mockBool = BuildCell(cell => cell.Value = true);
            var mockDateTime = BuildCell(cell => cell.Value = new DateTime(2026, 2, 4));
            var mockEmpty = BuildCell(cell => { });
            
            var sut = this.CreateTestSubject();

            var numberResult = sut.GetValue(mockNumber);
            var textResult = sut.GetValue(mockString);
            var boolResult = sut.GetValue(mockBool);
            var dateTimeResult = sut.GetValue(mockDateTime);
            var blankResult = sut.GetValue(mockEmpty);

            Assert.Equal(CellValueType.Number, numberResult.Type);
            Assert.Equal(42.0, numberResult.Raw);
            
            Assert.Equal(CellValueType.Text, textResult.Type);
            Assert.Equal("Hello", textResult.Raw);
            
            Assert.Equal(CellValueType.Boolean, boolResult.Type);
            Assert.Equal(true, boolResult.Raw);
            
            Assert.Equal(CellValueType.DateTime, dateTimeResult.Type);
            Assert.Equal(new DateTime(2026, 2, 4), dateTimeResult.Raw);
            
            Assert.Equal(CellValueType.Empty, blankResult.Type);
            Assert.Null(blankResult.Raw);
        }

        [Fact]
        public void IsEmptyReturnsTrueForBlankAndWhitespaceCellsAndFalseForValueCells()
        {
            var sut = this.CreateTestSubject();

            var mockBlank = BuildCell(cell => { });
            var mockWhitespace = BuildCell(cell => cell.Value = "   ");
            var mockString = BuildCell(cell => cell.Value = "Hello");
            var mockNumber = BuildCell(cell => cell.Value = 1.0);

            Assert.True(sut.IsEmpty(mockBlank));
            Assert.True(sut.IsEmpty(mockWhitespace));
            Assert.False(sut.IsEmpty(mockString));
            Assert.False(sut.IsEmpty(mockNumber));
        }
    }
}