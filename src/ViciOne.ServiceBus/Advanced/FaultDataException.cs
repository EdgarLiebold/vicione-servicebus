using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Associates application diagnostic values with an exception for inclusion in a published <see cref="Fault{T}" />.
/// </summary>
/// <remarks>
/// Fault creation reports the wrapped exception's type and message while giving these diagnostic values precedence
/// over entries with the same case-insensitive key in <see cref="Exception.Data" />.
/// </remarks>
public sealed class FaultDataException :
    Exception
{
    Dictionary<string, object>? _data;

    /// <summary>Wraps an exception without adding diagnostic values.</summary>
    /// <param name="innerException">The application failure represented by a published fault.</param>
    public FaultDataException(Exception innerException)
        : base(GetExceptionMessage(innerException), innerException)
    {
        ImportExceptionData(innerException);
    }

    /// <summary>Wraps an exception with diagnostic values read from an object's public properties.</summary>
    /// <param name="innerException">The application failure represented by a published fault.</param>
    /// <param name="values">An object whose public readable properties provide diagnostic keys and values.</param>
    public FaultDataException(Exception innerException, object values)
        : base(GetExceptionMessage(innerException), innerException)
    {
        _data = ToCaseInsensitiveDictionary(values);

        ImportExceptionData(innerException);
    }

    /// <summary>Wraps an exception with the supplied diagnostic entries.</summary>
    /// <param name="innerException">The application failure represented by a published fault.</param>
    /// <param name="values">The diagnostic entries to attach to the fault.</param>
    public FaultDataException(Exception innerException, IEnumerable<KeyValuePair<string, object>> values)
        : base(GetExceptionMessage(innerException), innerException)
    {
        ArgumentNullException.ThrowIfNull(values);
        _data = values.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        ImportExceptionData(innerException);
    }

    /// <summary>Wraps an exception with a fault-specific message.</summary>
    /// <param name="message">The message exposed by this wrapper while it is in process.</param>
    /// <param name="innerException">The application failure represented by a published fault.</param>
    public FaultDataException(string message, Exception innerException)
        : base(message ?? throw new ArgumentNullException(nameof(message)),
            innerException ?? throw new ArgumentNullException(nameof(innerException)))
    {
        ImportExceptionData(innerException);
    }

    /// <summary>Wraps an exception with a fault-specific message and values read from an object's public properties.</summary>
    /// <param name="message">The message exposed by this wrapper while it is in process.</param>
    /// <param name="innerException">The application failure represented by a published fault.</param>
    /// <param name="values">An object whose public readable properties provide diagnostic keys and values.</param>
    public FaultDataException(string message, Exception innerException, object values)
        : base(message ?? throw new ArgumentNullException(nameof(message)),
            innerException ?? throw new ArgumentNullException(nameof(innerException)))
    {
        _data = ToCaseInsensitiveDictionary(values);

        ImportExceptionData(innerException);
    }

    /// <summary>Wraps an exception with a fault-specific message and the supplied diagnostic entries.</summary>
    /// <param name="message">The message exposed by this wrapper while it is in process.</param>
    /// <param name="innerException">The application failure represented by a published fault.</param>
    /// <param name="values">The diagnostic entries to attach to the fault.</param>
    public FaultDataException(string message, Exception innerException, IEnumerable<KeyValuePair<string, object>> values)
        : base(message ?? throw new ArgumentNullException(nameof(message)),
            innerException ?? throw new ArgumentNullException(nameof(innerException)))
    {
        ArgumentNullException.ThrowIfNull(values);
        _data = values.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        ImportExceptionData(innerException);
    }

    /// <summary>Gets the case-insensitive diagnostic entries associated with the wrapped exception.</summary>
    public override IDictionary Data => MutableData;

    /// <summary>Gets the writable case-insensitive diagnostic entries included in the published fault.</summary>
    public IDictionary<string, object> ApplicationData => MutableData;

    Dictionary<string, object> MutableData =>
        _data ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    static string GetExceptionMessage(Exception innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        return innerException.Message;
    }

    static Dictionary<string, object> ToCaseInsensitiveDictionary(object values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new Dictionary<string, object>(ConvertObject.ToDictionary(values), StringComparer.OrdinalIgnoreCase);
    }

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
