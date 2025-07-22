using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Application.Exceptions;

public class DataNotFoundException : Exception
{
    public string? FieldName;
    public string? FieldValue;
    public string? ObjectName;

    public DataNotFoundException(string fieldName, string fieldValue, string objectName)
    {
        this.FieldName = fieldName;
        this.FieldValue = fieldValue;
        this.ObjectName = objectName;
    }

    public DataNotFoundException(string? message) : base(message)
    {
    }

    public DataNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected DataNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

