using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Configures routing-slip state after an activity faults.</summary>
public interface FaultedActivityOptions
{
    /// <summary>Sets an optional delay before compensation starts.</summary>
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
}
