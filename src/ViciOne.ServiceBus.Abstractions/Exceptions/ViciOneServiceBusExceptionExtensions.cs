using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides extension methods for vici one service bus exception.
/// </summary>
public static class ViciOneServiceBusExceptionExtensions
{
    /// <summary>
    /// Compiles the validation results and throws a <see cref="ConfigurationException" /> if any failures are present.
    /// </summary>
    /// <param name="results"></param>
    /// <param name="prefix">An optional prefix to override the default exception prefix</param>
    /// <exception cref="ConfigurationException"></exception>
    public static IReadOnlyList<ValidationResult> ThrowIfContainsFailure(this IEnumerable<ValidationResult> results, string? prefix = null)
    {
        List<ValidationResult> resultList = results.ToList();

        if (!resultList.ContainsFailure())
            return resultList;

        var message = (prefix ?? "The configuration is invalid:")
            + Environment.NewLine
            + string.Join(Environment.NewLine, resultList.Select(x => x.ToString()).ToArray());

        throw new ConfigurationException(resultList, global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Vici One Service Bus Exception Extensions", "unknown", message, "Correct the named configuration before starting the host"));
    }

    /// <summary>
    /// Performs the contains failure operation.
    /// </summary>
    /// <param name="results">The results value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool ContainsFailure(this IEnumerable<ValidationResult> results)
    {
        return results.Any(x => x.Disposition == ValidationResultDisposition.Failure);
    }
}
