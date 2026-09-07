using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Configures routing-slip state after an activity completes successfully.</summary>
public interface CompletedActivityOptions
{
    /// <summary>Sets an optional delay before the next activity executes.</summary>
    TimeSpan? Delay { set; }

    /// <summary>Adds, updates, or removes variables using the readable properties of an object.</summary>
    /// <param name="variables">The object whose properties define the variable updates.</param>
    void SetVariables(object variables);

    /// <summary>Adds, updates, or removes routing-slip variables.</summary>
    /// <param name="variables">The variables; an entry with a null value removes the matching variable.</param>
    void SetVariables(IEnumerable<KeyValuePair<string, object>> variables);

    /// <summary>Adds, updates, or removes one routing-slip variable.</summary>
    /// <param name="key">The variable name.</param>
    /// <param name="value">The new value, or <see langword="null"/> to remove the variable.</param>
    void SetVariable(string key, object? value);

    /// <summary>Sets the typed compensation log.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log output to serialize and store in the routing slip for compensation.</param>
    void SetLog<TLog>(TLog log)
        where TLog : class;

    /// <summary>Sets compensation-log fields using the readable properties of an object.</summary>
    /// <param name="values">The object whose properties define the compensation log.</param>
    void SetLog(object values);

    /// <summary>Sets compensation-log fields from a name/value sequence.</summary>
    /// <param name="values">The compensation-log fields.</param>
    void SetLog(IEnumerable<KeyValuePair<string, object>> values);
}
