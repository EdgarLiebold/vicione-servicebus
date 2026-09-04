namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a request consumer future endpoint definition implementation.
/// </summary>
/// <typeparam name="TFuture">The t future type.</typeparam>
public class RequestConsumerFutureEndpointDefinition<TFuture> :
    IEndpointDefinition<TFuture>
    where TFuture : class
{
    readonly IConsumerDefinition _consumerDefinition;
    readonly IDefinition _definition;
    string _endpointName = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="consumerDefinition">The consumer definition value.</param>
    public RequestConsumerFutureEndpointDefinition(IDefinition definition, IConsumerDefinition consumerDefinition)
    {
        _definition = definition;
        _consumerDefinition = consumerDefinition;
    }

    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology => true;

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
    }

    /// <summary>
    /// Gets the is temporary value.
    /// </summary>
    public bool IsTemporary => false;

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public int? PrefetchCount => default;

    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit => _definition.ConcurrentMessageLimit;

    /// <summary>
    /// Gets endpoint name.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
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
