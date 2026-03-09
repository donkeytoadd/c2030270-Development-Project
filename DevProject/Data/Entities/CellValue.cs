namespace DevProject.Data.Entities
{
    using Enums;

    public class CellValue
    {
        public CellValueType Type { get; set; }
        public object? Raw { get; set; }
        
        public static CellValue Empty() => new()
        {
            Type = CellValueType.Empty,
            Raw = null
        };
        
        public static CellValue FromNumber(double v) => new()
        {
            Type = CellValueType.Number,
            Raw = v
        };
        
        public static CellValue FromBoolean(bool v) => new()
        {
            Type = CellValueType.Boolean,
            Raw = v
        };
        
        public static CellValue FromDateTime(DateTime v) => new()
        {
            Type = CellValueType.DateTime,
            Raw = v
        };
        
        public static CellValue FromText(string v)  => new()
        {
            Type = CellValueType.Text,
            Raw = v
        };
    }
}