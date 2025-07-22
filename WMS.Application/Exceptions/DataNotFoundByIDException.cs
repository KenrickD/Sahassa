using System.Runtime.Serialization;

namespace WMS.Application.Exceptions;

[Serializable]
public class DataNotFoundByIDException : Exception
{
    public int Id;
    public string? ObjectName;

    public DataNotFoundByIDException()
    {
    }

    public DataNotFoundByIDException(int id, string objectName)
    {
        this.Id = id;    
        this.ObjectName = objectName;
    }

    public DataNotFoundByIDException(string? message) : base(message)
    {
    }

    public DataNotFoundByIDException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected DataNotFoundByIDException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
