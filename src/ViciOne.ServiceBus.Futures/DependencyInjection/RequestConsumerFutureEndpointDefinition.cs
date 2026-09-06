namespace ViciOne.ServiceBus.Futures.DependencyInjection;

/// <summary>Derives a consumer-backed future endpoint from its request consumer definition.</summary>
/// <typeparam name="TFuture">The future state-machine type.</typeparam>
internal sealed class RequestConsumerFutureEndpointDefinition<TFuture> :
    IEndpointDefinition<TFuture>
    where TFuture : class
{
    readonly IConsumerDefinition _consumerDefinition;
    readonly IDefinition _definition;
    string? _endpointName;

    /// <summary>Creates future endpoint metadata derived from a companion request consumer.</summary>
    /// <param name="definition">The future definition that supplies concurrency settings.</param>
    /// <param name="consumerDefinition">The companion consumer definition that supplies the endpoint name.</param>
    public RequestConsumerFutureEndpointDefinition(IDefinition definition, IConsumerDefinition consumerDefinition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(consumerDefinition);
        _definition = definition;
        _consumerDefinition = consumerDefinition;
    }

    /// <summary>Gets whether the endpoint configures consume topology.</summary>
    public bool ConfigureConsumeTopology => true;

    /// <summary>Validates the endpoint configurator; transport settings come from the companion consumer definition.</summary>
    /// <typeparam name="T">The receive endpoint configurator type.</typeparam>
    /// <param name="configurator">The future's receive endpoint.</param>
    /// <param name="context">The optional registration context.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);
    }

    /// <summary>Gets whether the future endpoint is temporary.</summary>
    public bool IsTemporary => false;

    /// <summary>Gets the transport prefetch override, if any.</summary>
    public int? PrefetchCount => default;

    /// <summary>Gets the future definition's concurrent message limit.</summary>
    public int? ConcurrentMessageLimit => _definition.ConcurrentMessageLimit;

    /// <summary>Gets the endpoint name derived from the companion consumer endpoint.</summary>
    /// <param name="formatter">The formatter used to create and sanitize the endpoint name.</param>
    /// <returns>The companion consumer endpoint name with a future suffix.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

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
