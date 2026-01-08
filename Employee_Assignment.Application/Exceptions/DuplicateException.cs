namespace Employee_Assignment.Application.Exceptions
{
    public class DuplicateException : Exception
    {
        public DuplicateException(string message) : base(message) { }

        public DuplicateException(string entityName, string fieldName, object value)
            : base($"{entityName} with {fieldName} '{value}' already exists.") { }
    }
}