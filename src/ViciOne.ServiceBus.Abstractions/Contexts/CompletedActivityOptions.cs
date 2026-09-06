using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines configuration options for completed activity.</summary>
public interface CompletedActivityOptions
{
    /// <summary>When specified, uses the scheduler to delay execution of the next activity by the specified duration.</summary>
    TimeSpan? Delay { set; }

    /// <summary>Add or update the variables on the routing slip with the specified object (properties are mapped to variables).</summary>
    /// <param name="variables">The variables.</param>
    void SetVariables(object variables);

    /// <summary>Add or update the variables on the routing slip with the specified values.</summary>
    /// <param name="variables">The variables.</param>
    void SetVariables(IEnumerable<KeyValuePair<string, object>> variables);

    /// <summary>Add or update the variable on the routing slip with the specified object.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    void SetVariable(string key, object value);

    /// <summary>Set the log data for compensation.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log output to serialize and store in the routing slip for compensation.</param>
    void SetLog<TLog>(TLog log)
        where TLog : class;

    /// <summary>Set the log data for compensation.</summary>
    /// <param name="values">An object to convert to a dictionary for the log data.</param>
    void SetLog(object values);

    /// <summary>Set the log data for compensation using a collection of string/object pairs.</summary>
    /// <param name="values">The values.</param>
    void SetLog(IEnumerable<KeyValuePair<string, object>> values);
}
