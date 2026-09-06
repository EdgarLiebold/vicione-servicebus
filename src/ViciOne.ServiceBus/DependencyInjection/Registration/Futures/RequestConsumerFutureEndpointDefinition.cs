namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Defines configuration for request consumer future endpoint.</summary>
/// <typeparam name="TFuture">The future type.</typeparam>
public class RequestConsumerFutureEndpointDefinition<TFuture> :
    IEndpointDefinition<TFuture>
    where TFuture : class
{
    readonly IConsumerDefinition _consumerDefinition;
    readonly IDefinition _definition;
    string _endpointName = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="consumerDefinition">The consumer definition.</param>
    public RequestConsumerFutureEndpointDefinition(IDefinition definition, IConsumerDefinition consumerDefinition)
    {
        _definition = definition;
        _consumerDefinition = consumerDefinition;
    }

    /// <summary>Gets the configure consume topology.</summary>
    public bool ConfigureConsumeTopology => true;

    /// <summary>Applies the supplied configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
    }

    /// <summary>Gets a value indicating whether temporary.</summary>
    public bool IsTemporary => false;

    /// <summary>Gets the prefetch count.</summary>
    public int? PrefetchCount => default;

    /// <summary>Gets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit => _definition.ConcurrentMessageLimit;

    /// <summary>Gets endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        string GetSeparator()
        {
            return formatter switch
            {
                SnakeCaseEndpointNameFormatter f => f.Separator,
                _ => ""
            };
        }

        var consumerEndpointName = _consumerDefinition.GetEndpointName(formatter);

        return _endpointName ??= formatter.SanitizeName(consumerEndpointName + GetSeparator() + "Future");
    }
}
