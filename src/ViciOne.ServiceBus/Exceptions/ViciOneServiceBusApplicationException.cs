using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// For use by application developers to include additional data elements along with the exception, which will be
/// transferred to the <see cref="ExceptionInfo" /> of the <see cref="Fault{T}" /> event.
/// </summary>
public class ViciOneServiceBusApplicationException :
    Exception
{
    Dictionary<string, object> _data = null!;

    /// <summary>Initializes a new instance.</summary>
    protected ViciOneServiceBusApplicationException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="innerException">The inner exception.</param>
    public ViciOneServiceBusApplicationException(Exception innerException)
        : base(innerException.Message, innerException)
    {
        ImportExceptionData(innerException);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="values">The values.</param>
    public ViciOneServiceBusApplicationException(Exception innerException, object values)
        : base(innerException.Message, innerException)
    {
        _data = ConvertObject.ToDictionary(values);

        ImportExceptionData(innerException);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="values">The values.</param>
    public ViciOneServiceBusApplicationException(Exception innerException, IEnumerable<KeyValuePair<string, object>> values)
        : base(innerException.Message, innerException)
    {
        _data = values.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        ImportExceptionData(innerException);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public ViciOneServiceBusApplicationException(string message, Exception innerException)
        : base(message, innerException)
    {
        ImportExceptionData(innerException);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="values">The values.</param>
    public ViciOneServiceBusApplicationException(string message, Exception innerException, object values)
        : base(message, innerException)
    {
        _data = ConvertObject.ToDictionary(values);

        ImportExceptionData(innerException);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="values">The values.</param>
    public ViciOneServiceBusApplicationException(string message, Exception innerException, IEnumerable<KeyValuePair<string, object>> values)
        : base(message, innerException)
    {
        _data = values.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        ImportExceptionData(innerException);
    }

    /// <summary>Gets the data.</summary>
    public override IDictionary Data => _data ?? base.Data;

    /// <summary>Gets the application data.</summary>
    public IDictionary<string, object> ApplicationData => _data;

    void ImportExceptionData(Exception exception)
    {
        if (exception.Data == null)
            return;

        var keys = exception.Data.Keys;
        if (keys.Count == 0)
            return;

        foreach (var key in keys)
        {
            if (key is string stringKey && (_data == null || !_data.ContainsKey(stringKey)))
            {
                var value = exception.Data[key];
                if (value != null)
                {
                    _data ??= new Dictionary<string, object>();

                    _data.Add(stringKey, value);
                }
            }
        }
    }
}
