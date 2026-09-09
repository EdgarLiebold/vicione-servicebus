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
    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="options">The application-level publication options.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes after the transport accepts the publication.</returns>
    Task IPublishEndpoint.PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        return PublishAsync(message, new PublishOptionsPipe<T>(options), cancellationToken);
    }

    /// <summary>Publishes a typed message through a typed publish-context pipe.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="publishPipe">The pipeline applied to the typed publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes after the transport accepts the publication.</returns>
    Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Publishes a typed message through an untyped publish-context pipe.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="publishPipe">The pipeline applied to the untyped publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes after the transport accepts the publication.</returns>
    Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Publishes a runtime-typed message.</summary>
    /// <param name="message">The message instance whose runtime type is published.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes after the transport accepts the publication.</returns>
    Task PublishAsync(object message, CancellationToken cancellationToken = default);

    /// <summary>Publishes a runtime-typed message through a publish-context pipe.</summary>
    /// <param name="message">The message instance whose runtime type is published.</param>
    /// <param name="publishPipe">The pipeline applied to the untyped publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes after the transport accepts the publication.</returns>
    Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default);

    /// <summary>Publishes a message as the specified runtime type.</summary>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="messageType">The message contract type to declare to the publish pipeline.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes after the transport accepts the publication.</returns>
    Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default);

    /// <summary>Publishes a message as the specified runtime type through a publish-context pipe.</summary>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="messageType">The message contract type to declare to the publish pipeline.</param>
    /// <param name="publishPipe">The pipeline applied to the untyped publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes after the transport accepts the publication.</returns>
    Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default);

    /// <summary>Initializes and publishes a message from property values.</summary>
    /// <typeparam name="T">The message contract type to initialize.</typeparam>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="cancellationToken">The token that cancels initialization or publication.</param>
    /// <returns>A task that completes after the initialized message is accepted by the transport.</returns>
    Task PublishAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes and publishes a message through a typed publish-context pipe.</summary>
    /// <typeparam name="T">The message contract type to initialize.</typeparam>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="publishPipe">The pipeline applied to the typed publish context.</param>
    /// <param name="cancellationToken">The token that cancels initialization or publication.</param>
    /// <returns>A task that completes after the initialized message is accepted by the transport.</returns>
    Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes and publishes a message through an untyped publish-context pipe.</summary>
    /// <typeparam name="T">The message contract type to initialize.</typeparam>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="publishPipe">The pipeline applied to the untyped publish context.</param>
    /// <param name="cancellationToken">The token that cancels initialization or publication.</param>
    /// <returns>A task that completes after the initialized message is accepted by the transport.</returns>
    Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class;
}
