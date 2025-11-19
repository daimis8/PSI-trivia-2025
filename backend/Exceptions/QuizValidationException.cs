namespace backend.Exceptions;

public class QuizValidationException : Exception
{
    public string ValidationField { get; }
    public object? InvalidValue { get; }

    public QuizValidationException(string message, string validationField, object? invalidValue = null) 
        : base(message)
    {
        ValidationField = validationField;
        InvalidValue = invalidValue;
    }

    public QuizValidationException(string message, string validationField, object? invalidValue, Exception innerException) 
        : base(message, innerException)
    {
        ValidationField = validationField;
        InvalidValue = invalidValue;
    }
}


