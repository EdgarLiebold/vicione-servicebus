using System;
using ViciOne.ServiceBus.Advanced.Serialization;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Reads strongly typed values from the transport-independent activity variable set.</summary>
public static class ActivityContextVariableExtensions
{
    /// <summary>Gets a reference-type activity variable, or the supplied default when the variable is absent.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the variable is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ActivityContext context, string key, T? defaultValue = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.SerializerContext.GetValue(context.Variables, key, defaultValue);
    }

    /// <summary>Gets a value-type activity variable, or the supplied default when the variable is absent.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the variable is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ActivityContext context, string key, T? defaultValue = null)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.SerializerContext.GetValue(context.Variables, key, defaultValue);
    }
}
