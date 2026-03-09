namespace DevProject.Data.Exceptions
{
    public class ProcessExcelException : Exception
    {
        public ProcessExcelException(string message) : base(message) { }

        public ProcessExcelException(string message, Exception inner) : base(message, inner) { }
    }
}