namespace DevProject.Tests.Business.Getters
{
    using DevProject.Business.Getters;
    using DevProject.Data.Entities;

    public class TmForumApiDetectorGetterTests : TestBase<TmForumApiDetectorGetter>
    {
        private TmForumApiDetectorGetter CreateDetector() =>
            new TmForumApiDetectorGetter(new MappingTemplateGetter());

        private static ParsedWorkbook SingleSheetWorkbook(
            string workbookName,
            string sheetName,
            params string[] columns) =>
            new()
            {
                WorkbookName = workbookName,
                Sheets =
                [
                    new ParsedExcelData
                    {
                        SpreadsheetName = sheetName,
                        ColumnNames = new List<string>(columns),
                        Rows = new List<Dictionary<string, CellValue>>()
                    }
                ]
            };

        private static ParsedWorkbook MultiSheetWorkbook(
            string workbookName,
            params (string sheetName, string[] columns)[] sheets) =>
            new()
            {
                WorkbookName = workbookName,
                Sheets = sheets
                    .Select(s => new ParsedExcelData
                    {
                        SpreadsheetName = s.sheetName,
                        ColumnNames = new List<string>(s.columns),
                        Rows = new List<Dictionary<string, CellValue>>()
                    })
                    .ToList()
            };
        
        [Theory]
        [InlineData("TMF622_Orders.xlsx", "TMF622")]
        [InlineData("My TMF634 Specs", "TMF634")]
        [InlineData("tmf641 service order", "TMF641")]
        public void FilenameContainsTmfIdBoostsCorrectCandidate(
            string workbookName, string expectedTopApiId)
        {
            var workbook = SingleSheetWorkbook(workbookName, "Sheet1");
            var sut = CreateDetector();

            var result = sut.DetectApi(workbook);

            Assert.Equal(expectedTopApiId, result.Candidates[0].ApiId);
            Assert.True(result.Candidates[0].Confidence > 0);
        }
        
        [Fact]
        public void WorkbookWithTmf620SheetNamesScoresTmf620Highest()
        {
            var workbook = MultiSheetWorkbook("Product Spec",
                ("Overview", ["Name", "Description"]),
                ("productSpecCharacteristic", ["Name", "ValueType"]),
                ("relatedParty", ["Id", "Role"]));

            var sut = CreateDetector();
            var result = sut.DetectApi(workbook);

            Assert.Equal("TMF620", result.Candidates[0].ApiId);
        }

        [Fact]
        public void WorkbookWithTmf633SheetNamesScoresTmf633Highest()
        {
            var workbook = MultiSheetWorkbook("TMF633_GAMMA_Extra",
                ("Service", ["name", "description", "version"]),
                ("SpecCharacteristic", ["name", "valueType"]),
                ("RelatedParty", ["id", "role"]));

            var sut = CreateDetector();
            var result = sut.DetectApi(workbook);

            Assert.Equal("TMF633", result.Candidates[0].ApiId);
        }
        
        [Fact]
        public void Tmf632ColumnNamesScoresTmf632Highest()
        {
            var workbook = SingleSheetWorkbook("People", "Overview",
                "GivenName", "FamilyName", "Gender");

            var sut = CreateDetector();
            var result = sut.DetectApi(workbook);

            Assert.Equal("TMF632", result.Candidates[0].ApiId);
        }

        [Fact]
        public void Tmf622ColumnNamesScoresTmf622Highly()
        {
            var workbook = SingleSheetWorkbook("Orders", "Overview",
                "Description", "ExternalId", "Priority", "State",
                "RequestedStartDate", "RequestedCompletionDate");

            var sut = CreateDetector();
            var result = sut.DetectApi(workbook);

            Assert.Equal("TMF622", result.Candidates[0].ApiId);
        }
        
        [Fact]
        public void FullMatchWithFilenameBonusAutoSelectsWhenGapSufficient()
        {
            var workbook = MultiSheetWorkbook("TMF629_Customers",
                ("Overview", ["Name", "Description", "Status", "CustomerRank"]),
                ("characteristic", ["Name", "Value", "ValueType", "UnitOfMeasure"]),
                ("relatedParty", ["Id", "Role", "Name", "ReferredType"]));

            var sut = CreateDetector();
            var result = sut.DetectApi(workbook);

            Assert.Equal("TMF629", result.Candidates[0].ApiId);
            Assert.True(result.Candidates[0].Confidence >= 70);
            Assert.Equal("TMF629", result.AutoSelectedApiId);
        }

        [Fact]
        public void TopCandidateBelow70DoesNotAutoSelect()
        {
            var workbook = SingleSheetWorkbook("Data", "Sheet1", "Name", "Description");

            var sut = CreateDetector();
            var result = sut.DetectApi(workbook);

            Assert.Null(result.AutoSelectedApiId);
        }
        
        [Fact]
        public void ReturnsCandidatesForEveryRegisteredTemplate()
        {
            var workbook = SingleSheetWorkbook("Empty", "Sheet1");
            var sut = CreateDetector();

            var result = sut.DetectApi(workbook);

            var templateCount = new MappingTemplateGetter().GetAll().Count;
            Assert.Equal(templateCount, result.Candidates.Count);
        }

        [Fact]
        public void CandidatesAreSortedByConfidenceDescending()
        {
            var workbook = SingleSheetWorkbook("Empty", "Sheet1");
            var sut = CreateDetector();

            var confidences = sut.DetectApi(workbook).Candidates.Select(c => c.Confidence).ToList();

            for (int i = 0; i < confidences.Count - 1; i++)
                Assert.True(confidences[i] >= confidences[i + 1],
                    $"Candidate at index {i} ({confidences[i]}) is less than candidate at {i + 1} ({confidences[i + 1]})");
        }

        [Fact]
        public void MatchedSignalsContainExpectedSheetNames()
        {
            var workbook = MultiSheetWorkbook("TMF622 Orders",
                ("Overview", ["Description"]),
                ("productOrderItem", ["Id", "Action"]));

            var sut = CreateDetector();
            var result = sut.DetectApi(workbook);

            var tmf622 = result.Candidates.First(c => c.ApiId == "TMF622");
            Assert.Contains(tmf622.MatchedSignals, s => s.StartsWith("sheet:"));
        }
        
        [Theory]
        [InlineData("TMF622 Product Orders", "TMF622")]
        [InlineData("My tmf634 Specs", "TMF634")]
        [InlineData("TMF641 Service Order", "TMF641")]
        public void DetectApiIdFilenameContainsTmfIdReturnsCorrectApiId(
            string sheetName, string expected)
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = sheetName,
                ColumnNames = new List<string>(),
                Rows = new List<Dictionary<string, CellValue>>()
            };

            var sut = CreateDetector();
            var result = sut.DetectApiId(data);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void DetectApiIdNoMatchingSignalsReturnsNull()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Foo", "Bar", "Baz" },
                Rows = new List<Dictionary<string, CellValue>>()
            };

            var sut = CreateDetector();
            var result = sut.DetectApiId(data);

            Assert.Null(result);
        }

        [Fact]
        public void DetectApiIdTmf632ColumnsReturnsTmf632()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "People",
                ColumnNames = new List<string> { "GivenName", "FamilyName", "Gender" },
                Rows = new List<Dictionary<string, CellValue>>()
            };

            var sut = CreateDetector();
            var result = sut.DetectApiId(data);

            Assert.Equal("TMF632", result);
        }
    }
}
