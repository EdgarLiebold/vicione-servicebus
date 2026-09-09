using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides validation-result guards used while composing service-bus configuration.</summary>
public static class ViciOneServiceBusExceptionExtensions
{
    /// <summary>Compiles the validation results and throws a <see cref="ConfigurationException" /> if any failures are present.</summary>
    /// <param name="results">The validation results to evaluate and snapshot.</param>
    /// <param name="prefix">The optional heading for the aggregated failure message.</param>
    /// <returns>A stable list containing every supplied validation result.</returns>
    /// <exception cref="ConfigurationException">Thrown when the current service bus configuration is invalid.</exception>
    public static IReadOnlyList<ValidationResult> ThrowIfContainsFailure(this IEnumerable<ValidationResult> results, string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(results);

        List<ValidationResult> resultList = results.ToList();

        if (!resultList.ContainsFailure())
            return resultList;

        var message = (prefix ?? "The configuration is invalid:")
            + Environment.NewLine
            + string.Join(Environment.NewLine, resultList.Select(x => x.ToString()).ToArray());

        throw new ConfigurationException(resultList, global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Vici One Service Bus Exception Extensions", "unknown", message, "Correct the named configuration before starting the host"));
    }

    /// <summary>Determines whether the sequence contains at least one failed validation result.</summary>
    /// <param name="results">The validation results to inspect.</param>
    /// <returns><see langword="true" /> when at least one result is a failure; otherwise, <see langword="false" />.</returns>
    public static bool ContainsFailure(this IEnumerable<ValidationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Any(x => x.Disposition == ValidationResultDisposition.Failure);
    }
}
