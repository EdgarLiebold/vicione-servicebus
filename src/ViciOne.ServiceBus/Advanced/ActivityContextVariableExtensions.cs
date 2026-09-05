using System;
using ViciOne.ServiceBus.Advanced.Serialization;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Reads strongly typed values from the transport-independent activity variable set.
/// </summary>
public static class ActivityContextVariableExtensions
{
    /// <summary>
    /// Gets a reference-type activity variable, or the supplied default when the variable is absent.
    /// </summary>
    /// <typeparam name="T">The expected variable type.</typeparam>
    /// <param name="context">The activity context.</param>
    /// <param name="key">The variable name.</param>
    /// <param name="defaultValue">The value returned when the variable is absent.</param>
    /// <returns>The converted variable value.</returns>
    public static T? GetVariable<T>(this ActivityContext context, string key, T? defaultValue = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.SerializerContext.GetValue(context.Variables, key, defaultValue);
    }

    /// <summary>
    /// Gets a value-type activity variable, or the supplied default when the variable is absent.
    /// </summary>
    /// <typeparam name="T">The expected variable type.</typeparam>
    /// <param name="context">The activity context.</param>
    /// <param name="key">The variable name.</param>
    /// <param name="defaultValue">The value returned when the variable is absent.</param>
    /// <returns>The converted variable value.</returns>
    public static T? GetVariable<T>(this ActivityContext context, string key, T? defaultValue = null)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.SerializerContext.GetValue(context.Variables, key, defaultValue);
    }
}
