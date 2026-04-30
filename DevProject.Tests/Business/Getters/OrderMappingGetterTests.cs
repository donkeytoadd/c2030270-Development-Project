namespace DevProject.Tests.Business.Getters
{
    using DevProject.Business.Getters;
    using DevProject.Data.Entities;
    using DevProject.Data.Enums;
    using System.Text.Json.Nodes;

    public class OrderMappingGetterTests : TestBase<OrderMappingGetter>
    {
        private static TmForumApiDefinition BuildApiDef() =>
            new TmForumApiDefinition
            {
                ApiId = "TMF622",
                Name = "Product Ordering Management",
                ResourceType = "ProductOrder",
                BasePath = "/productOrderingManagement/v4",
                ResourceCollectionPath = "productOrder",
                Version = "4.0",
                MappingStrategy = MappingStrategy.Order,
                CharacteristicArrayKey = "productOrderItem",
                OrderItemCharacteristicKey = "productCharacteristic",
                OrderItemNestedObjectKey = "product",
                OrderItemNestedObjectType = "ProductRefOrValue"
            };

        private static JsonObject GetItem(JsonNode order, int index = 0) =>
            ((order as JsonObject)!["productOrderItem"] as JsonArray)![index]!.AsObject();

        private static JsonObject GetProduct(JsonNode order, int index = 0) =>
            GetItem(order, index)["product"]!.AsObject();

        private static JsonArray GetChars(JsonNode order, int index = 0) =>
            GetProduct(order, index)["productCharacteristic"]!.AsArray();

        [Fact]
        public void SupportsOrderStrategy()
        {
            var sut = CreateTestSubject();
            Assert.True(sut.Supports(MappingStrategy.Order));
            Assert.False(sut.Supports(MappingStrategy.Specification));
        }

        [Fact]
        public void GetAllWithoutGroupByColumnProducesOneOrderPerRow()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Product" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Product"] = CellValue.FromText("ProductA") },
                    new() { ["Product"] = CellValue.FromText("ProductB") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetAllGroupsRowsByGroupByColumn()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "OrderId", "Product" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["OrderId"] = CellValue.FromText("ORD-001"), ["Product"] = CellValue.FromText("ProductA") },
                    new() { ["OrderId"] = CellValue.FromText("ORD-001"), ["Product"] = CellValue.FromText("ProductB") },
                    new() { ["OrderId"] = CellValue.FromText("ORD-002"), ["Product"] = CellValue.FromText("ProductC") }
                }
            };
            var config = new JsonConversionConfig { GroupByColumn = "OrderId" };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetAllSetsExternalIdFromGroupKey()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "OrderId", "Product" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["OrderId"] = CellValue.FromText("ORD-001"), ["Product"] = CellValue.FromText("ProductA") }
                }
            };
            var config = new JsonConversionConfig { GroupByColumn = "OrderId" };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);

            Assert.Equal("ORD-001", (result[0] as JsonObject)!["externalId"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllBuildsOrderItemsForEachRowInGroup()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "OrderId", "Product" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["OrderId"] = CellValue.FromText("ORD-001"), ["Product"] = CellValue.FromText("Alpha") },
                    new() { ["OrderId"] = CellValue.FromText("ORD-001"), ["Product"] = CellValue.FromText("Beta") }
                }
            };
            var config = new JsonConversionConfig { GroupByColumn = "OrderId" };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);

            Assert.Equal(2, ((result[0] as JsonObject)!["productOrderItem"] as JsonArray)!.Count);
        }

        [Fact]
        public void GetAllDerivesItemTypeFromCharacteristicArrayKey()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Product" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Product"] = CellValue.FromText("Alpha") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            Assert.Equal("ProductOrderItem", GetItem(result[0])["@type"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllNestsCharacteristicsInsideProductObject()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Bandwidth" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Bandwidth"] = CellValue.FromText("100Mbps") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var item = GetItem(result[0]);
            Assert.Null(item["productCharacteristic"]);
            Assert.NotNull(item["product"]);
            Assert.NotNull(GetChars(result[0]));
        }

        [Fact]
        public void GetAllSetsProductObjectType()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Product" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Product"] = CellValue.FromText("Alpha") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var product = GetProduct(result[0]);

            Assert.Equal("ProductRefOrValue", product["@type"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllExcludesGroupByColumnFromCharacteristics()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "OrderId", "Product" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["OrderId"] = CellValue.FromText("ORD-001"), ["Product"] = CellValue.FromText("Alpha") }
                }
            };
            var config = new JsonConversionConfig { GroupByColumn = "OrderId" };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var chars = GetChars(result[0]);

            Assert.DoesNotContain(chars.Cast<JsonObject>(),
                c => c["name"]!.GetValue<string>() == "OrderId");
        }

        [Fact]
        public void FieldMappings_ActionColumnSetsItemAction()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Action", "Product" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Action"] = CellValue.FromText("modify"), ["Product"] = CellValue.FromText("Alpha") }
                }
            };
            var config = new JsonConversionConfig
            {
                FieldMappings = new Dictionary<string, string> { ["Action"] = "item.action" }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var item = GetItem(result[0]);

            Assert.Equal("modify", item["action"]!.GetValue<string>()); var chars = GetChars(result[0]);
            Assert.DoesNotContain(chars.Cast<JsonObject>(),
                c => c["name"]!.GetValue<string>() == "Action");
        }

        [Fact]
        public void FieldMappings_OrderPriorityColumnSetsOrderField()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Priority", "Product" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Priority"] = CellValue.FromText("1"), ["Product"] = CellValue.FromText("Alpha") }
                }
            };
            var config = new JsonConversionConfig
            {
                FieldMappings = new Dictionary<string, string> { ["Priority"] = "order.priority" }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var order = result[0] as JsonObject;

            Assert.Equal("1", order!["priority"]!.GetValue<string>());
        }

        [Fact]
        public void FieldMappings_ProductNameColumnSetsNestedProductName()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Product Name", "Bandwidth" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["Product Name"] = CellValue.FromText("Fibre 500"),
                        ["Bandwidth"] = CellValue.FromText("500Mbps")
                    }
                }
            };
            var config = new JsonConversionConfig
            {
                FieldMappings = new Dictionary<string, string> { ["Product Name"] = "product.name" }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var product = GetProduct(result[0]);

            Assert.Equal("Fibre 500", product["name"]!.GetValue<string>()); var chars = product["productCharacteristic"]!.AsArray();
            Assert.Contains(chars.Cast<JsonObject>(),
                c => c["name"]!.GetValue<string>() == "Bandwidth");
        }
    }
}
