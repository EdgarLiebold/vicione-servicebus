using System;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Writes a precompiled structured log message with one template value.</summary>
/// <typeparam name="T1">The first template-value type.</typeparam>
/// <param name="arg1">The first template value.</param>
/// <param name="exception">The exception recorded with the message, if any.</param>
public delegate void LogMessage<in T1>(T1 arg1, Exception? exception = default);

/// <summary>Writes a precompiled structured log message with two template values.</summary>
/// <typeparam name="T1">The first template-value type.</typeparam>
/// <typeparam name="T2">The second template-value type.</typeparam>
/// <param name="arg1">The first template value.</param>
/// <param name="arg2">The second template value.</param>
/// <param name="exception">The exception recorded with the message, if any.</param>
public delegate void LogMessage<in T1, in T2>(T1 arg1, T2 arg2, Exception? exception = default);

/// <summary>Writes a precompiled structured log message with three template values.</summary>
/// <typeparam name="T1">The first template-value type.</typeparam>
/// <typeparam name="T2">The second template-value type.</typeparam>
/// <typeparam name="T3">The third template-value type.</typeparam>
/// <param name="arg1">The first template value.</param>
/// <param name="arg2">The second template value.</param>
/// <param name="arg3">The third template value.</param>
/// <param name="exception">The exception recorded with the message, if any.</param>
public delegate void LogMessage<in T1, in T2, in T3>(T1 arg1, T2 arg2, T3 arg3, Exception? exception = default);

/// <summary>Writes a precompiled structured log message with four template values.</summary>
/// <typeparam name="T1">The first template-value type.</typeparam>
/// <typeparam name="T2">The second template-value type.</typeparam>
/// <typeparam name="T3">The third template-value type.</typeparam>
/// <typeparam name="T4">The fourth template-value type.</typeparam>
/// <param name="arg1">The first template value.</param>
/// <param name="arg2">The second template value.</param>
/// <param name="arg3">The third template value.</param>
/// <param name="arg4">The fourth template value.</param>
/// <param name="exception">The exception recorded with the message, if any.</param>
public delegate void LogMessage<in T1, in T2, in T3, in T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, Exception? exception = default);

/// <summary>Writes a precompiled structured log message with five template values.</summary>
/// <typeparam name="T1">The first template-value type.</typeparam>
/// <typeparam name="T2">The second template-value type.</typeparam>
/// <typeparam name="T3">The third template-value type.</typeparam>
/// <typeparam name="T4">The fourth template-value type.</typeparam>
/// <typeparam name="T5">The fifth template-value type.</typeparam>
/// <param name="arg1">The first template value.</param>
/// <param name="arg2">The second template value.</param>
/// <param name="arg3">The third template value.</param>
/// <param name="arg4">The fourth template value.</param>
/// <param name="arg5">The fifth template value.</param>
/// <param name="exception">The exception recorded with the message, if any.</param>
public delegate void LogMessage<in T1, in T2, in T3, in T4, in T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Exception? exception = default);
