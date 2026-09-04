using System;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides an exception type converter implementation.
/// </summary>
public class ExceptionTypeConverter :
    ITypeConverter<string, Exception>,
    ITypeConverter<ExceptionInfo, Exception>,
    ITypeConverter<ExceptionInfo, object>
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(Exception? input, out ExceptionInfo? result)
    {
        if (input != null)
        {
            result = new FaultExceptionInfo(input);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(object? input, out ExceptionInfo? result)
    {
        switch (input)
        {
            case Exception exception:
                result = new FaultExceptionInfo(exception);
                return true;

            case ExceptionInfo exceptionInfo:
                result = exceptionInfo;
                return true;

            default:
                result = default;
                return false;
        }
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(Exception? input, out string? result)
    {
        if (input != null)
        {
            result = ExceptionUtil.GetMessage(input);
            return true;
        }

        result = default;
        return false;
    }
}
