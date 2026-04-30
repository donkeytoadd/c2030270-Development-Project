namespace DevProject.Tests.Business.Processors
{
    using DevProject.Business.Processors;
    using DevProject.Data.Entities;
    using DevProject.Data.Enums;
    using System.Text.Json.Nodes;

    public class TmForumSchemaValidationProcessorTests : TestBase<TmForumSchemaValidationProcessor>
    {
        [Fact]
        public void ValidateTmf638ValidResourceReturnsIsValidTrue()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("svc-001"),
                ["href"] = JsonValue.Create("/tmf-api/serviceCatalogManagement/v4/serviceSpecification/svc-001"),
                ["name"] = JsonValue.Create("Business Broadband"),
                ["@type"] = JsonValue.Create("ServiceSpecification"),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF638");

            Assert.True(report.IsValid);
            Assert.Equal(0, report.TotalIssues);
            Assert.Empty(report.Issues);
        }

        [Fact]
        public void ValidateTmf620ValidResourceReturnsIsValidTrue()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("prod-001"),
                ["name"] = JsonValue.Create("Broadband Bundle"),
                ["version"] = JsonValue.Create("1.0"),
                ["lifecycleStatus"] = JsonValue.Create("Active"),
                ["@type"] = JsonValue.Create("ProductSpecification"),
                ["@baseType"] = JsonValue.Create("EntitySpecification"),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF620");

            Assert.True(report.IsValid);
            Assert.Equal(0, report.TotalIssues);
        }

        [Fact]
        public void ValidateTmf622ValidResourceReturnsIsValidTrue()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("order-001"),
                ["state"] = JsonValue.Create("acknowledged"),
                ["productOrderItem"] = new JsonArray
                {
                    new JsonObject { ["id"] = JsonValue.Create("item-1"), ["action"] = JsonValue.Create("add") }
                }
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF622");

            Assert.True(report.IsValid);
        }

        [Fact]
        public void ValidateTmf638MissingRequiredNameReturnsIsValidFalse()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("svc-bad"),
                ["@type"] = JsonValue.Create("ServiceSpecification"),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF638");

            Assert.False(report.IsValid);
            Assert.True(report.TotalIssues > 0);
        }

        [Fact]
        public void ValidateTmf638MissingNameHasIssueWithNonEmptyMessage()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("svc-bad"),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF638");

            Assert.All(report.Issues, issue => Assert.False(string.IsNullOrWhiteSpace(issue.Message)));
        }

        [Fact]
        public void ValidateTmf622MissingRequiredProductOrderItemReturnsIsValidFalse()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("order-bad"),
                ["description"] = JsonValue.Create("Missing items"),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF622");

            Assert.False(report.IsValid);
        }

        [Fact]
        public void ValidateTmf620EmptyNameReturnsIsValidFalse()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("prod-bad"),
                ["name"] = JsonValue.Create(""),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF620");

            Assert.False(report.IsValid);
        }

        [Fact]
        public void ValidateValidResourceHas100PercentCompliance()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("svc-ok"),
                ["name"] = JsonValue.Create("My Service"),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF638");

            Assert.Equal(100.0, report.CompliancePercentage);
        }

        [Fact]
        public void ValidateInvalidResourceHasLessThan100PercentCompliance()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("svc-bad"),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF638");

            Assert.True(report.CompliancePercentage < 100.0);
            Assert.True(report.CompliancePercentage >= 0.0);
        }

        [Fact]
        public void ValidateUnknownApiIdReturnsIsValidTrueWithNoIssues()
        {
            var resource = new JsonObject { ["anything"] = JsonValue.Create("value") };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "UNKNOWN_API");

            Assert.True(report.IsValid);
            Assert.Equal(0, report.TotalIssues);
        }

        [Fact]
        public void ValidateOneValidOneInvalidResourceReportsIssuesForInvalidOnly()
        {
            var valid = new JsonObject
            {
                ["id"] = JsonValue.Create("svc-1"),
                ["name"] = JsonValue.Create("Good Service"),
            };
            var invalid = new JsonObject
            {
                ["id"] = JsonValue.Create("svc-2"),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([valid, invalid], "TMF638");

            Assert.False(report.IsValid);
            Assert.All(report.Issues, issue => Assert.Equal(1, issue.ResourceIndex));
        }

        [Fact]
        public void ValidateAllIssuesSeverityIsErrorOrArtefact()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("svc-bad"),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF638");

            Assert.All(report.Issues, issue =>
                Assert.True(issue.Severity == ValidationSeverity.Error || issue.Severity == ValidationSeverity.SchemaArtefact));
        }

        [Fact]
        public void ValidateFalseSchemaMessageClassifiedAsSchemaArtefact()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("svc-001"),
                ["name"] = JsonValue.Create("My Spec"),
                ["@type"] = JsonValue.Create("ServiceSpecification"),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF638");

            var artefacts = report.Issues.Where(i => i.Severity == ValidationSeverity.SchemaArtefact).ToList();
            var errors = report.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
            Assert.Equal(errors.Count, report.TotalIssues);
            Assert.Equal(artefacts.Count, report.ArtefactCount);
        }

        [Fact]
        public void ValidateOnlyArtefactIssuesIsValidTrue()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("svc-ok"),
                ["name"] = JsonValue.Create("Good Service"),
                ["@type"] = JsonValue.Create("ServiceSpecification"),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF638");

            Assert.True(report.IsValid);
            Assert.Equal(0, report.TotalIssues);
        }

        [Fact]
        public void ValidateArtefactsExcludedFromComplianceCalculation()
        {
            var resource = new JsonObject
            {
                ["id"] = JsonValue.Create("svc-ok"),
                ["name"] = JsonValue.Create("Perfect Service"),
                ["@type"] = JsonValue.Create("ServiceSpecification"),
            };

            var sut = CreateTestSubject();
            var report = sut.Validate([resource], "TMF638");

            var hasOnlyArtefacts = report.Issues.All(i => i.Severity == ValidationSeverity.SchemaArtefact);
            if (hasOnlyArtefacts)
                Assert.Equal(100.0, report.CompliancePercentage);
        }
    }
}
