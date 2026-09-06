using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Exposes low-level publish forms for middleware, serializers, initializers, and transport integrations.
/// Application code should prefer <see cref="IPublishEndpoint"/>.
/// </summary>
public interface IAdvancedPublishEndpoint :
    IPublishEndpoint
{
    Task IPublishEndpoint.PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        return PublishAsync(message, new PublishOptionsPipe<T>(options), cancellationToken);
    }

    /// <summary>Publishes a typed message through a typed publish-context pipe.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="publishPipe">The publish pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Publishes a typed message through an untyped publish-context pipe.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="publishPipe">The publish pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Publishes a runtime-typed message.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync(object message, CancellationToken cancellationToken = default);

    /// <summary>Publishes a runtime-typed message through a publish-context pipe.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="publishPipe">The publish pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default);

    /// <summary>Publishes a message as the specified runtime type.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default);

    /// <summary>Publishes a message as the specified runtime type through a publish-context pipe.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="publishPipe">The publish pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default);

    /// <summary>Initializes and publishes a message from property values.</summary>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes and publishes a message through a typed publish-context pipe.</summary>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="publishPipe">The publish pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes and publishes a message through an untyped publish-context pipe.</summary>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="publishPipe">The publish pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class;
}
