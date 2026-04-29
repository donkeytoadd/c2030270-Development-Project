namespace DevProject.Data.Entities
{
    using Enums; public abstract class CellValue
    {
        public abstract CellValueType Type { get; } public abstract object? Raw { get; }

        public bool IsEmpty => Type == CellValueType.Empty;

        public abstract string AsString();

        public static CellValue Empty() => EmptyCellValue.Instance;
        public static CellValue FromText(string v) => new TextCellValue { Value = v };
        public static CellValue FromNumber(double v) => new NumberCellValue { Value = v };
        public static CellValue FromBoolean(bool v) => new BooleanCellValue { Value = v };
        public static CellValue FromDateTime(DateTime v) => new DateTimeCellValue { Value = v };
    }

    public sealed class EmptyCellValue : CellValue
    {
        internal static readonly EmptyCellValue Instance = new();
        public override CellValueType Type => CellValueType.Empty;
        public override object? Raw => null;
        public override string AsString() => string.Empty;
    }

    public sealed class TextCellValue : CellValue
    {
        public required string Value { get; init; }
        public override CellValueType Type => CellValueType.Text;
        public override object? Raw => Value;
        public override string AsString() => Value;
    }

    public sealed class NumberCellValue : CellValue
    {
        public required double Value { get; init; }
        public override CellValueType Type => CellValueType.Number;
        public override object? Raw => Value;
        public override string AsString() => Value.ToString();
    }

    public sealed class BooleanCellValue : CellValue
    {
        public required bool Value { get; init; }
        public override CellValueType Type => CellValueType.Boolean;
        public override object? Raw => Value;
        public override string AsString() => Value.ToString();
    }

    public sealed class DateTimeCellValue : CellValue
    {
        public required DateTime Value { get; init; }
        public override CellValueType Type => CellValueType.DateTime;
        public override object? Raw => Value;
        public override string AsString() => Value.ToString("o");
    }
}
