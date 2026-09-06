using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Provides extension methods for routing slip event.</summary>
public static class RoutingSlipEventExtensions
{
    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlip> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlip> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets result.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The result.</returns>
    public static T? GetResult<T>(this ConsumeContext<RoutingSlipActivityCompensated> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Data, key, defaultValue);
    }

    /// <summary>Gets result.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The result.</returns>
    public static T? GetResult<T>(this ConsumeContext<RoutingSlipActivityCompensated> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Data, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipActivityCompensated> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipActivityCompensated> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets result.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The result.</returns>
    public static T? GetResult<T>(this ConsumeContext<RoutingSlipActivityCompensationFailed> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Data, key, defaultValue);
    }

    /// <summary>Gets result.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The result.</returns>
    public static T? GetResult<T>(this ConsumeContext<RoutingSlipActivityCompensationFailed> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Data, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipActivityCompensationFailed> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipActivityCompensationFailed> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets argument.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The argument.</returns>
    public static T? GetArgument<T>(this ConsumeContext<RoutingSlipActivityCompleted> context, string key, T? defaultValue = null)
        where T : class
    {
        return GetDictionaryValue(context.Advanced().SerializerContext, context.Message.Arguments, context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets argument.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The argument.</returns>
    public static T? GetArgument<T>(this ConsumeContext<RoutingSlipActivityCompleted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return GetDictionaryValue(context.Advanced().SerializerContext, context.Message.Arguments, context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets result.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The result.</returns>
    public static T? GetResult<T>(this ConsumeContext<RoutingSlipActivityCompleted> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Data, key, defaultValue);
    }

    /// <summary>Gets result.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The result.</returns>
    public static T? GetResult<T>(this ConsumeContext<RoutingSlipActivityCompleted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Data, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipActivityCompleted> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipActivityCompleted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets argument.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The argument.</returns>
    public static T? GetArgument<T>(this ConsumeContext<RoutingSlipActivityFaulted> context, string key, T? defaultValue = null)
        where T : class
    {
        return GetDictionaryValue(context.Advanced().SerializerContext, context.Message.Arguments, context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets argument.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The argument.</returns>
    public static T? GetArgument<T>(this ConsumeContext<RoutingSlipActivityFaulted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return GetDictionaryValue(context.Advanced().SerializerContext, context.Message.Arguments, context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipActivityFaulted> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipActivityFaulted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipCompensationFailed> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipCompensationFailed> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipCompleted> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipCompleted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipFaulted> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipFaulted> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipTerminated> context, string key, T? defaultValue = null)
        where T : class
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    /// <summary>Gets variable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The variable.</returns>
    public static T? GetVariable<T>(this ConsumeContext<RoutingSlipTerminated> context, string key, T? defaultValue = null)
        where T : struct
    {
        return context.Advanced().SerializerContext.GetValue(context.Message.Variables, key, defaultValue);
    }

    static T? GetDictionaryValue<T>(IObjectDeserializer context, IDictionary<string, object>? arguments, IDictionary<string, object>? variables,
        string key, T? defaultValue = null)
        where T : class
    {
        IDictionary<string, object>? argumentsDictionary = variables?.Count > 0
            ? arguments?.Count > 0 ? variables.MergeLeft(arguments) : variables
            : arguments;

        if (argumentsDictionary == null)
            return defaultValue;

        return context.GetValue(argumentsDictionary, key, defaultValue);
    }

    static T? GetDictionaryValue<T>(IObjectDeserializer context, IDictionary<string, object>? arguments, IDictionary<string, object>? variables,
        string key, T? defaultValue = null)
        where T : struct
    {
        IDictionary<string, object>? argumentsDictionary = variables?.Count > 0
            ? arguments?.Count > 0 ? variables.MergeLeft(arguments) : variables
            : arguments;

        if (argumentsDictionary == null)
            return defaultValue;

        return context.GetValue(argumentsDictionary, key, defaultValue);
    }
}
