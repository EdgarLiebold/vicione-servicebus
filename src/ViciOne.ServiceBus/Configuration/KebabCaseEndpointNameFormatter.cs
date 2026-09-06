namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Formats the endpoint names using kebab-case (dashed snake case)
/// SubmitOrderConsumer -> submit-order
/// OrderState -> order-state
/// UpdateCustomerActivity -> update-customer-execute, update-customer-compensate.
/// </summary>
public class KebabCaseEndpointNameFormatter :
    SnakeCaseEndpointNameFormatter
{
    const char KebabCaseSeparator = '-';

    /// <summary>Kebab case endpoint formatter, which uses dashes between words.</summary>
    /// <param name="includeNamespace">If true, the namespace is included in the name.</param>
    public KebabCaseEndpointNameFormatter(bool includeNamespace)
        : base(KebabCaseSeparator, null, includeNamespace)
    {
    }

    /// <summary>Kebab case endpoint formatter, which uses dashes between words.</summary>
    /// <param name="prefix">Prefix to start the name, should match the casing of the formatter (such as Dev or PreProd).</param>
    public KebabCaseEndpointNameFormatter(string prefix)
        : base(KebabCaseSeparator, prefix, false)
    {
    }

    /// <summary>Kebab case endpoint formatter, which uses dashes between words.</summary>
    /// <param name="prefix">Prefix to start the name, should match the casing of the formatter (such as Dev or PreProd).</param>
    /// <param name="includeNamespace">If true, the namespace is included in the name.</param>
    public KebabCaseEndpointNameFormatter(string prefix, bool includeNamespace)
        : base(KebabCaseSeparator, prefix, includeNamespace)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    protected KebabCaseEndpointNameFormatter()
        : base(KebabCaseSeparator, null, false)
    {
    }

    /// <summary>Gets the instance.</summary>
    public new static IEndpointNameFormatter Instance { get; } = new KebabCaseEndpointNameFormatter();

    /// <summary>Sanitizes name.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The string produced by the operation.</returns>
    public override string SanitizeName(string name)
    {
        return base.SanitizeName(name).Replace(SnakeCaseSeparator, KebabCaseSeparator);
    }
}
