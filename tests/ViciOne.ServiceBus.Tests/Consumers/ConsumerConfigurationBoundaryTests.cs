using System.Reflection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumer;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers;

public sealed class ConsumerConfigurationBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONFIGURATION-BOUNDARIES", "registration-extension-inputs")]
    public void RegistrationExtensions_RejectEveryInvalidRequiredInput()
    {
        IReceiveEndpointConfigurator endpoint = CreateProxy<IReceiveEndpointConfigurator>();
        IConsumePipe consumePipe = new ConsumePipeSpecification().BuildConsumePipe();
        var consumer = new BoundaryConsumer();
        var consumerFactory = new InstanceConsumerFactory<BoundaryConsumer>(consumer);
        var batchConsumer = new BoundaryBatchConsumer();
        var batchConfigurator = new BatchConfigurator<BoundaryMessage>(endpoint);
        MessageHandler<BoundaryMessage> handler = _ => Task.CompletedTask;
        var handlerPipeConfigurator = new PipeConfigurator<ConsumeContext<BoundaryMessage>>();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            BatchConsumerExtensions.Batch<BoundaryMessage>(null!, _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            endpoint.Batch<BoundaryMessage>(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            BatchConsumerExtensions.Consumer<BoundaryBatchConsumer, BoundaryMessage>(null!, () => batchConsumer)).ParamName);
        Assert.Equal("consumerFactoryMethod", Assert.Throws<ArgumentNullException>(() =>
            batchConfigurator.Consumer<BoundaryBatchConsumer, BoundaryMessage>((Func<BoundaryBatchConsumer>)null!)).ParamName);
        Assert.Equal("consumerFactory", Assert.Throws<ArgumentNullException>(() =>
            batchConfigurator.Consumer<BoundaryBatchConsumer, BoundaryMessage>((IConsumerFactory<BoundaryBatchConsumer>)null!)).ParamName);

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            ConsumerExtensions.Consumer<BoundaryConsumer>(null!, consumerFactory)).ParamName);
        Assert.Equal("consumerFactory", Assert.Throws<ArgumentNullException>(() =>
            endpoint.Consumer<BoundaryConsumer>((IConsumerFactory<BoundaryConsumer>)null!)).ParamName);
        Assert.Equal("pipeSpecifications", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ConsumerExtensions.ConnectConsumer(consumePipe, consumerFactory,
                (IPipeSpecification<ConsumerConsumeContext<BoundaryConsumer>>[])null!);
        }).ParamName);
        Assert.Equal("pipeSpecification", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = consumePipe.ConnectConsumer(consumerFactory,
                (IPipeSpecification<ConsumerConsumeContext<BoundaryConsumer>>)null!);
        }).ParamName);

        Assert.Equal("consumerType", Assert.Throws<ArgumentNullException>(() =>
            endpoint.Consumer(null!, _ => new object())).ParamName);
        Assert.Equal("consumerFactory", Assert.Throws<ArgumentNullException>(() =>
            endpoint.Consumer(typeof(BoundaryConsumer), null!)).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentException>(() =>
            endpoint.Consumer(typeof(int), _ => new object())).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = consumePipe.ConnectConsumer(null!, _ => new object());
        }).ParamName);
        Assert.Equal("objectFactory", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = consumePipe.ConnectConsumer(typeof(BoundaryConsumer), null!);
        }).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentException>(() =>
        {
            _ = consumePipe.ConnectConsumer(typeof(int), _ => new object());
        }).ParamName);
        Assert.Equal("consumePipe", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ConsumerConnectorCache.Connect(null!, typeof(BoundaryConsumer), _ => new BoundaryConsumer());
        }).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ConsumerConnectorCache.Connect(consumePipe, null!, _ => new BoundaryConsumer());
        }).ParamName);
        Assert.Equal("objectFactory", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ConsumerConnectorCache.Connect(consumePipe, typeof(BoundaryConsumer), null!);
        }).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentException>(() =>
        {
            _ = ConsumerConnectorCache.Connect(consumePipe, typeof(int), _ => new object());
        }).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() =>
            InstanceConnectorCache.GetInstanceConnector(null!)).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentException>(() =>
            InstanceConnectorCache.GetInstanceConnector(typeof(int))).ParamName);

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            HandlerExtensions.Handler<BoundaryMessage>(null!, handler)).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() =>
            endpoint.Handler<BoundaryMessage>(null!)).ParamName);
        Assert.Equal("connector", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = HandlerExtensions.ConnectHandler<BoundaryMessage>(null!, handler);
        }).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = consumePipe.ConnectHandler<BoundaryMessage>(null!);
        }).ParamName);
        Assert.Equal("connector", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = HandlerExtensions.ConnectRequestHandler<BoundaryMessage>(null!, Guid.Empty, handler, handlerPipeConfigurator);
        }).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = consumePipe.ConnectRequestHandler(Guid.Empty, (MessageHandler<BoundaryMessage>)null!, handlerPipeConfigurator);
        }).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = consumePipe.ConnectRequestHandler(Guid.Empty, handler, null!);
        }).ParamName);

        Assert.Equal("convention", Assert.Throws<ArgumentNullException>(() =>
            ConsumerConvention.Register<IConsumerConvention>(null!)).ParamName);
        Assert.Equal("convention", Assert.Throws<ArgumentNullException>(() =>
            ConsumerConventionCache.TryAdd<IConsumerConvention>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONFIGURATION-BOUNDARIES", "constructor-inputs")]
    public void ConsumerConfigurationTypes_RejectEveryInvalidConstructorInput()
    {
        Type messageType = typeof(BoundaryMessage);
        Type consumerType = typeof(BoundaryConsumer);
        var observer = CreateProxy<IConsumerConfigurationObserver>();

        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() =>
            new ConsumerInterfaceType(null!, consumerType)).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentNullException>(() =>
            new ConsumerInterfaceType(messageType, null!)).ParamName);
        Assert.Equal("batchMessageType", Assert.Throws<ArgumentNullException>(() =>
            new BatchConsumerInterfaceType(null!, messageType, consumerType)).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() =>
            new BatchConsumerInterfaceType(typeof(Batch<BoundaryMessage>), null!, consumerType)).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentNullException>(() =>
            new BatchConsumerInterfaceType(typeof(Batch<BoundaryMessage>), messageType, null!)).ParamName);

        Assert.Equal("consumeFilter", Assert.Throws<ArgumentNullException>(() =>
            new ConsumerMessageConnector<BoundaryConsumer, BoundaryMessage>(null!)).ParamName);
        Assert.Equal("consumeFilter", Assert.Throws<ArgumentNullException>(() =>
            new InstanceMessageConnector<BoundaryConsumer, BoundaryMessage>(null!)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            new ConsumerPipeSpecificationProxy<BoundaryConsumer, BoundaryMessage>(
                (IPipeSpecification<ConsumeContext<BoundaryMessage>>)null!)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            new ConsumerSplitFilterSpecification<BoundaryConsumer, BoundaryMessage>(null!)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            new ConsumerMessageSplitFilterSpecification<BoundaryConsumer, BoundaryMessage>(null!)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() =>
            new ObserverPipeSpecification<BoundaryMessage>(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            new ExecuteArgumentsConfigurator<BoundaryMessage>(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            new CompensateLogConfigurator<BoundaryMessage>(null!)).ParamName);
        Assert.Equal("messageSpecifications", Assert.Throws<ArgumentNullException>(() =>
            new ConsumerSpecification<BoundaryConsumer>(null!)).ParamName);
        Assert.Equal("consumerFactory", Assert.Throws<ArgumentNullException>(() =>
            new ConsumerConfigurator<BoundaryConsumer>(null!, observer)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() =>
            new ConsumerConfigurator<BoundaryConsumer>(new InstanceConsumerFactory<BoundaryConsumer>(new BoundaryConsumer()), null!)).ParamName);
        Assert.Equal("consumerFactory", Assert.Throws<ArgumentNullException>(() =>
            new UntypedConsumerConfigurator<BoundaryConsumer>(null!, observer)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() =>
            new UntypedConsumerConfigurator<BoundaryConsumer>(_ => new BoundaryConsumer(), null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONFIGURATION-BOUNDARIES", "pipeline-component-inputs")]
    public void ConsumerPipelines_RejectEveryInvalidRequiredComponent()
    {
        IConsumePipe consumePipe = new ConsumePipeSpecification().BuildConsumePipe();
        var consumerFactory = new InstanceConsumerFactory<BoundaryConsumer>(new BoundaryConsumer());
        var messageSpecification = new ConsumerMessageSpecification<BoundaryConsumer, BoundaryMessage>();
        var consumerSpecification = new ConsumerSpecification<BoundaryConsumer>([messageSpecification]);
        var consumeFilter = new MethodConsumerMessageFilter<BoundaryConsumer, BoundaryMessage>();
        var messageConnector = new ConsumerMessageConnector<BoundaryConsumer, BoundaryMessage>(consumeFilter);

        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            messageSpecification.AddPipeSpecification(
                (IPipeSpecification<ConsumerConsumeContext<BoundaryConsumer, BoundaryMessage>>)null!)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            messageSpecification.AddPipeSpecification((IPipeSpecification<ConsumeContext<BoundaryMessage>>)null!)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            messageSpecification.AddPipeSpecification((IPipeSpecification<ConsumerConsumeContext<BoundaryConsumer>>)null!)).ParamName);
        Assert.Equal("consumeFilter", Assert.Throws<ArgumentNullException>(() => messageSpecification.Build(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => messageSpecification.BuildMessagePipe(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => messageSpecification.Message(null!)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() =>
            messageSpecification.ConnectConsumerConfigurationObserver(null!)).ParamName);

        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            consumerSpecification.AddPipeSpecification(null!)).ParamName);
        Assert.Equal("pipeConfigurator", Assert.Throws<ArgumentNullException>(() =>
            consumerSpecification.ConfigureMessagePipe<BoundaryMessage>(null!)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() =>
            consumerSpecification.ConnectConsumerConfigurationObserver(null!)).ParamName);
        Assert.Equal("consumePipe", Assert.Throws<ArgumentNullException>(() =>
            messageConnector.ConnectConsumer(null!, consumerFactory, consumerSpecification)).ParamName);
        Assert.Equal("consumerFactory", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = messageConnector.ConnectConsumer(consumePipe, null!, consumerSpecification);
        }).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = messageConnector.ConnectConsumer(consumePipe, consumerFactory, null!);
        }).ParamName);

        var observerConnector = new MessageObserverConnector<BoundaryMessage>();
        var observer = new RecordingObserver<BoundaryMessage>();
        Assert.Equal("consumePipe", Assert.Throws<ArgumentNullException>(() =>
            observerConnector.ConnectObserver(null!, observer)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = observerConnector.ConnectObserver(consumePipe, null!);
        }).ParamName);
        Assert.Equal("filters", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = observerConnector.ConnectObserver(consumePipe, observer, null!);
        }).ParamName);
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = observerConnector.ConnectObserver(consumePipe, observer, (IFilter<ConsumeContext<BoundaryMessage>>)null!);
        }).ParamName);

        var handlerConnector = new HandlerConnector<BoundaryMessage>();
        MessageHandler<BoundaryMessage> handler = _ => Task.CompletedTask;
        var handlerPipeConfigurator = new PipeConfigurator<ConsumeContext<BoundaryMessage>>();
        Assert.Equal("consumePipe", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = handlerConnector.ConnectHandler(null!, handler, null);
        }).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = handlerConnector.ConnectHandler(consumePipe, null!, null);
        }).ParamName);
        Assert.Equal("consumePipe", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = handlerConnector.ConnectRequestHandler(null!, Guid.Empty, handler, handlerPipeConfigurator);
        }).ParamName);
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = handlerConnector.ConnectRequestHandler(consumePipe, Guid.Empty, null!, handlerPipeConfigurator);
        }).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = handlerConnector.ConnectRequestHandler(consumePipe, Guid.Empty, handler, null!);
        }).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONFIGURATION-BOUNDARIES", "service-instance-inputs")]
    public void ServiceInstanceConfiguration_RejectsEveryInvalidRequiredInput()
    {
        IReceiveConfigurator<IReceiveEndpointConfigurator> busConfigurator =
            CreateProxy<IReceiveConfigurator<IReceiveEndpointConfigurator>>();
        IReceiveEndpointConfigurator endpointConfigurator = CreateProxy<IReceiveEndpointConfigurator>();
        var options = new ServiceInstanceOptions();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            new ServiceInstanceConfigurator<IReceiveEndpointConfigurator>(null!, options, endpointConfigurator)).ParamName);
        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() =>
            new ServiceInstanceConfigurator<IReceiveEndpointConfigurator>(busConfigurator, null!, endpointConfigurator)).ParamName);
        Assert.Equal("instanceEndpointConfigurator", Assert.Throws<ArgumentNullException>(() =>
            new ServiceInstanceConfigurator<IReceiveEndpointConfigurator>(busConfigurator, options, null!)).ParamName);

        var configurator = new ServiceInstanceConfigurator<IReceiveEndpointConfigurator>(busConfigurator, options, endpointConfigurator);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() => configurator.AddSpecification(null!)).ParamName);
        Assert.Equal("definition", Assert.Throws<ArgumentNullException>(() =>
            configurator.ReceiveEndpoint((IEndpointDefinition)null!, null, null)).ParamName);
        Assert.Equal("queueName", Assert.Throws<ArgumentException>(() => configurator.ReceiveEndpoint(" ", null)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() =>
            configurator.ConnectEndpointConfigurationObserver(null!)).ParamName);

        var definition = new InstanceEndpointDefinition();
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() => definition.GetEndpointName(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            definition.Configure<IReceiveEndpointConfigurator>(null!, null)).ParamName);
    }

    private static TInterface CreateProxy<TInterface>()
        where TInterface : class =>
        DispatchProxy.Create<TInterface, BoundaryDispatchProxy>();

    private class BoundaryDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null || targetMethod.ReturnType == typeof(void))
                return null;

            return targetMethod.ReturnType.IsValueType
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }

    private sealed class RecordingObserver<TMessage> : IObserver<ConsumeContext<TMessage>>
        where TMessage : class
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(ConsumeContext<TMessage> value)
        {
        }
    }

    private sealed class BoundaryConsumer : IConsumer<BoundaryMessage>
    {
        public Task ConsumeAsync(ConsumeContext<BoundaryMessage> context) => Task.CompletedTask;
    }

    private sealed class BoundaryBatchConsumer : IConsumer<Batch<BoundaryMessage>>
    {
        public Task ConsumeAsync(ConsumeContext<Batch<BoundaryMessage>> context) => Task.CompletedTask;
    }

    private sealed record BoundaryMessage;
}
