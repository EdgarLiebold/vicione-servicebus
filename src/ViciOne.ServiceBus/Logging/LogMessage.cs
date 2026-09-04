using System;

#nullable enable
namespace ViciOne.ServiceBus.Logging;

/// <summary>
/// Represents the method that handles log message.
/// </summary>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <param name="arg1">The arg1 value.</param>
/// <param name="exception">The exception associated with the operation.</param>
/// <returns>The result of the operation.</returns>
public delegate void LogMessage<in T1>(T1 arg1, Exception? exception = default);


/// <summary>
/// Represents the method that handles log message.
/// </summary>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <typeparam name="T2">The t2 type.</typeparam>
/// <param name="arg1">The arg1 value.</param>
/// <param name="arg2">The arg2 value.</param>
/// <param name="exception">The exception associated with the operation.</param>
/// <returns>The result of the operation.</returns>
public delegate void LogMessage<in T1, in T2>(T1 arg1, T2 arg2, Exception? exception = default);


/// <summary>
/// Represents the method that handles log message.
/// </summary>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <typeparam name="T2">The t2 type.</typeparam>
/// <typeparam name="T3">The t3 type.</typeparam>
/// <param name="arg1">The arg1 value.</param>
/// <param name="arg2">The arg2 value.</param>
/// <param name="arg3">The arg3 value.</param>
/// <param name="exception">The exception associated with the operation.</param>
/// <returns>The result of the operation.</returns>
public delegate void LogMessage<in T1, in T2, in T3>(T1? arg1, T2 arg2, T3? arg3, Exception? exception = default);


/// <summary>
/// Represents the method that handles log message.
/// </summary>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <typeparam name="T2">The t2 type.</typeparam>
/// <typeparam name="T3">The t3 type.</typeparam>
/// <typeparam name="T4">The t4 type.</typeparam>
/// <param name="arg1">The arg1 value.</param>
/// <param name="arg2">The arg2 value.</param>
/// <param name="arg3">The arg3 value.</param>
/// <param name="arg4">The arg4 value.</param>
/// <param name="exception">The exception associated with the operation.</param>
/// <returns>The result of the operation.</returns>
public delegate void LogMessage<in T1, in T2, in T3, in T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, Exception? exception = default);


/// <summary>
/// Represents the method that handles log message.
/// </summary>
/// <typeparam name="T1">The t1 type.</typeparam>
/// <typeparam name="T2">The t2 type.</typeparam>
/// <typeparam name="T3">The t3 type.</typeparam>
/// <typeparam name="T4">The t4 type.</typeparam>
/// <typeparam name="T5">The t5 type.</typeparam>
/// <param name="arg1">The arg1 value.</param>
/// <param name="arg2">The arg2 value.</param>
/// <param name="arg3">The arg3 value.</param>
/// <param name="arg4">The arg4 value.</param>
/// <param name="arg5">The arg5 value.</param>
/// <param name="exception">The exception associated with the operation.</param>
/// <returns>The result of the operation.</returns>
public delegate void LogMessage<in T1, in T2, in T3, in T4, in T5>(T1? arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Exception? exception = default);
