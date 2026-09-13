using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Reads typed arguments, results and variables from routing-slip event contexts.</summary>
public static class RoutingSlipEventExtensions
{
    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received routing slip.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlip> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received routing slip.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlip> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed compensation result.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received activity-compensated event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The result.</returns>
    public static T? GetResult<T>(this ConsumeContext<IRoutingSlipActivityCompensated> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Data, key, defaultValue);
    }

    /// <summary>Gets a typed compensation result.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received activity-compensated event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The result.</returns>
    public static T? GetResult<T>(this ConsumeContext<IRoutingSlipActivityCompensated> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Data, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received activity-compensated event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipActivityCompensated> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received activity-compensated event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipActivityCompensated> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed compensation-failure result.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received activity-compensation-failed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The result.</returns>
    public static T? GetResult<T>(this ConsumeContext<IRoutingSlipActivityCompensationFailed> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Data, key, defaultValue);
    }

    /// <summary>Gets a typed compensation-failure result.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received activity-compensation-failed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The result.</returns>
    public static T? GetResult<T>(this ConsumeContext<IRoutingSlipActivityCompensationFailed> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Data, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received activity-compensation-failed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipActivityCompensationFailed> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received activity-compensation-failed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipActivityCompensationFailed> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed activity argument, with the activity's argument entry taking precedence over a routing-slip variable.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received activity-completed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The argument.</returns>
    public static T? GetArgument<T>(this ConsumeContext<IRoutingSlipActivityCompleted> context, string key, T? defaultValue = null)
        where T : class
    {
        return GetDictionaryValue(context.Advanced().SerializerContext, context.Message.Arguments, context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed activity argument, with the activity's argument entry taking precedence over a routing-slip variable.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received activity-completed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The argument.</returns>
    public static T? GetArgument<T>(this ConsumeContext<IRoutingSlipActivityCompleted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return GetDictionaryValue(context.Advanced().SerializerContext, context.Message.Arguments, context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed activity result.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received activity-completed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The result.</returns>
    public static T? GetResult<T>(this ConsumeContext<IRoutingSlipActivityCompleted> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Data, key, defaultValue);
    }

    /// <summary>Gets a typed activity result.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received activity-completed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The result.</returns>
    public static T? GetResult<T>(this ConsumeContext<IRoutingSlipActivityCompleted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Data, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received activity-completed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipActivityCompleted> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received activity-completed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipActivityCompleted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed faulted-activity argument, with the activity's argument entry taking precedence over a routing-slip variable.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received activity-faulted event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The argument.</returns>
    public static T? GetArgument<T>(this ConsumeContext<IRoutingSlipActivityFaulted> context, string key, T? defaultValue = null)
        where T : class
    {
        return GetDictionaryValue(context.Advanced().SerializerContext, context.Message.Arguments, context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed faulted-activity argument, with the activity's argument entry taking precedence over a routing-slip variable.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received activity-faulted event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The argument.</returns>
    public static T? GetArgument<T>(this ConsumeContext<IRoutingSlipActivityFaulted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return GetDictionaryValue(context.Advanced().SerializerContext, context.Message.Arguments, context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received activity-faulted event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipActivityFaulted> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received activity-faulted event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipActivityFaulted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received routing-slip compensation-failed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipCompensationFailed> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received routing-slip compensation-failed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipCompensationFailed> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received routing-slip completed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipCompleted> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received routing-slip completed event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipCompleted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received routing-slip faulted event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipFaulted> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received routing-slip faulted event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipFaulted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a reference-valued variable from a routing-slip revision event.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received revision event.</param>
    /// <param name="key">The variable key.</param>
    /// <param name="defaultValue">The value returned when the key is absent or cannot produce a value.</param>
    /// <returns>The converted variable or <paramref name="defaultValue" />.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipRevised> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a value-typed variable from a routing-slip revision event.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received revision event.</param>
    /// <param name="key">The variable key.</param>
    /// <param name="defaultValue">The value returned when the key is absent or cannot produce a value.</param>
    /// <returns>The converted variable or <paramref name="defaultValue" />.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipRevised> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="context">The received routing-slip terminated event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipTerminated> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets a typed routing-slip variable.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="context">The received routing-slip terminated event.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<IRoutingSlipTerminated> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    static T? GetDictionaryValue<T>(IObjectDeserializer context, IReadOnlyDictionary<string, object>? arguments, IReadOnlyDictionary<string, object>? variables,
        string key, T? defaultValue = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        IReadOnlyDictionary<string, object>? argumentsDictionary = variables?.Count > 0
            ? arguments?.Count > 0 ? Merge(variables, arguments) : variables
            : arguments;

        if (argumentsDictionary == null)
            return defaultValue;

        return context.GetValue(argumentsDictionary, key, defaultValue);
    }

    static T? GetDictionaryValue<T>(IObjectDeserializer context, IReadOnlyDictionary<string, object>? arguments, IReadOnlyDictionary<string, object>? variables,
        string key, T? defaultValue = null)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        IReadOnlyDictionary<string, object>? argumentsDictionary = variables?.Count > 0
            ? arguments?.Count > 0 ? Merge(variables, arguments) : variables
            : arguments;

        if (argumentsDictionary == null)
            return defaultValue;

        return context.GetValue(argumentsDictionary, key, defaultValue);
    }

    static Dictionary<string, object> Merge(
        IReadOnlyDictionary<string, object> variables,
        IReadOnlyDictionary<string, object> arguments)
    {
        var values = new Dictionary<string, object>(variables, StringComparer.OrdinalIgnoreCase);
        foreach ((string key, object? value) in arguments)
        {
            if (value is not null || !values.ContainsKey(key))
                values[key] = value!;
        }

        return values;
    }
}
