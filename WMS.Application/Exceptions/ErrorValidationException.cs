using System.Runtime.Serialization;

namespace WMS.Application.Exceptions;

public class ErrorValidationException : Exception
{
    public string? ObjectName;
    public string? ErrorMessage;
    public ErrorValidationException()
    {
    }

    public ErrorValidationException(string? errorMessage, string objectName)
    {
        this.ErrorMessage = errorMessage;
        this.ObjectName = objectName;
    }

    public ErrorValidationException(string? message) : base(message)
    {
    }

    public ErrorValidationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected ErrorValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
