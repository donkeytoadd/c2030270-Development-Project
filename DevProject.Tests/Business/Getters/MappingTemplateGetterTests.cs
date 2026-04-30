namespace DevProject.Tests.Business.Getters
{
    using DevProject.Business.Getters;
    using DevProject.Data.Enums;

    public class MappingTemplateGetterTests : TestBase<MappingTemplateGetter>
    {
        [Theory]
        [InlineData("TMF634")]
        [InlineData("TMF633")]
        [InlineData("TMF620")]
        [InlineData("TMF637")]
        [InlineData("TMF639")]
        [InlineData("TMF629")]
        [InlineData("TMF632")]
        [InlineData("TMF622")]
        [InlineData("TMF641")]
        public void GetByIdReturnsTemplateForEveryBuiltInApiId(string apiId)
        {
            var sut = CreateTestSubject();
            var result = sut.GetById(apiId);

            Assert.NotNull(result);
            Assert.Equal(apiId, result!.ApiId);
        }

        [Fact]
        public void GetByIdReturnsCaseInsensitiveMatch()
        {
            var sut = CreateTestSubject();

            Assert.NotNull(sut.GetById("tmf634"));
            Assert.NotNull(sut.GetById("TMF634"));
            Assert.NotNull(sut.GetById("Tmf634"));
        }

        [Fact]
        public void GetByIdReturnsNullForUnknownApiId()
        {
            var sut = CreateTestSubject();
            var result = sut.GetById("TMF999");

            Assert.Null(result);
        }

        [Fact]
        public void GetAllReturnsAtLeastNineTemplates()
        {
            var sut = CreateTestSubject();
            var result = sut.GetAll();

            Assert.True(result.Count >= 9);
        }

        [Fact]
        public void GetAllReturnsDistinctApiIds()
        {
            var sut = CreateTestSubject();
            var ids = sut.GetAll().Select(t => t.ApiId).ToList();
            var distinct = ids.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            Assert.Equal(ids.Count, distinct.Count);
        }

        [Fact]
        public void EveryTemplateHasAtLeastOneSheetMapping()
        {
            var sut = CreateTestSubject();

            foreach (var template in sut.GetAll())
                Assert.NotEmpty(template.SheetMappings);
        }

        [Fact]
        public void EverySheetMappingHasAtLeastOneColumnMapping()
        {
            var sut = CreateTestSubject();

            foreach (var template in sut.GetAll())
            foreach (var sheet in template.SheetMappings)
                Assert.NotEmpty(sheet.ColumnMappings);
        }

        [Fact]
        public void SpecificationApiTemplatesHaveNestedArrayCharacteristicSheet()
        {
            var sut = CreateTestSubject();

            foreach (var apiId in new[] { "TMF634", "TMF620" })
            {
                var template = sut.GetById(apiId)!;
                var nestedSheets = template.SheetMappings
                    .Where(s => s.Pattern == SheetMappingPattern.NestedArray)
                    .ToList();

                Assert.NotEmpty(nestedSheets);

                foreach (var sheet in nestedSheets)
                {
                    Assert.NotNull(sheet.GroupByColumn);
                    Assert.NotEmpty(sheet.SubArrayDefinitions);
                }
            }
        }

        [Fact]
        public void Tmf633TemplateUsesFlatArrayPattern()
        {
            var sut = CreateTestSubject();
            var template = sut.GetById("TMF633")!;

            Assert.All(template.SheetMappings,
                m => Assert.NotEqual(SheetMappingPattern.NestedArray, m.Pattern));
        }

        [Fact]
        public void Tmf633TemplateHasServiceSheetMarkedAsRequired()
        {
            var sut = CreateTestSubject();
            var template = sut.GetById("TMF633")!;

            var service = template.SheetMappings.First(m => m.SheetName == "Service");
            Assert.True(service.IsRequired);
        }

        [Fact]
        public void Tmf633TemplateDoesNotEmitIdHrefOrLastUpdate()
        {
            var sut = CreateTestSubject();
            var template = sut.GetById("TMF633")!;

            Assert.False(template.EmitId);
            Assert.False(template.EmitHref);
            Assert.False(template.EmitLastUpdate);
            Assert.Null(template.RootBaseType);
        }

        [Fact]
        public void Tmf634TemplateHasCorrectRootTypeAndBaseType()
        {
            var sut = CreateTestSubject();
            var template = sut.GetById("TMF634")!;

            Assert.Equal("ResourceSpecification", template.RootResourceType);
            Assert.Equal("EntitySpecification", template.RootBaseType);
        }

        [Fact]
        public void AllTemplatesHaveNonEmptyApiNameAndPaths()
        {
            var sut = CreateTestSubject();

            foreach (var template in sut.GetAll())
            {
                Assert.False(string.IsNullOrWhiteSpace(template.ApiName));
                Assert.False(string.IsNullOrWhiteSpace(template.BasePath));
                Assert.False(string.IsNullOrWhiteSpace(template.ResourceCollectionPath));
            }
        }

        [Fact]
        public void TopLevelFieldsSheetIsPresentInEveryTemplate()
        { var sut = CreateTestSubject();

            foreach (var template in sut.GetAll())
            {
                var hasTopLevel = template.SheetMappings
                    .Any(s => s.Pattern == SheetMappingPattern.TopLevelFields
                           && s.TargetKey == string.Empty);

                Assert.True(hasTopLevel,
                    $"Template {template.ApiId} has no TopLevelFields sheet with empty TargetKey.");
            }
        }

        [Fact]
        public void TopLevelFieldsSheetAlwaysHasEmptyTargetKey()
        {
            var sut = CreateTestSubject();

            foreach (var template in sut.GetAll())
            {
                var topLevel = template.SheetMappings
                    .First(s => s.Pattern == SheetMappingPattern.TopLevelFields
                             && s.TargetKey == string.Empty);

                Assert.Equal(SheetMappingPattern.TopLevelFields, topLevel.Pattern);
                Assert.Equal(string.Empty, topLevel.TargetKey);
            }
        }
    }
}
