using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class PublicScopedFilterSelectionContractTests
{
    [Theory]
    [InlineData(Phase.Consume)]
    [InlineData(Phase.Send)]
    [InlineData(Phase.Publish)]
    [InlineData(Phase.Execute)]
    [InlineData(Phase.Compensate)]
    [RequirementCoverage("REQ-VSB-CONTAINER-FILTER", "public-selector-selected-excluded-five-pipelines")]
    public void PublicScopedFilterSelection_AttachesOnlyTheSelectedPipelineContracts(Phase phase)
    {
        var services = new ServiceCollection();
        _ = new ServiceCollectionBusConfigurator(services);
        using ServiceProvider provider = services.BuildServiceProvider();
        IRegistrationContext context = provider.GetRequiredService<IBusRegistrationContext>();
        var capture = new AttachmentCapture();
        IPublicPipeline pipeline = Proxy<IPublicPipeline>(capture.Invoke);
        int selections = 0;
        Attach(phase, pipeline, context, filter =>
        {
            selections++;
            Assert.Equal(0, capture.ConnectionCount);
            Assert.Equal(0, capture.ConfigurationCallbacks);
            filter.Include<Selected>();
            filter.Include<Excluded>();
            filter.Exclude<Excluded>();
        });
        Assert.Equal(1, selections);
        capture.AssertOnlyExpectedPhase(phase);
        Assert.Single(Drive<Selected>(phase, capture));
        Assert.Empty(Drive<Excluded>(phase, capture));
        Assert.Empty(Drive<Unselected>(phase, capture));

        if (phase == Phase.Consume)
        {
            Assert.Same(capture.Consumer, capture.Saga);
            Assert.Single(DriveSaga<Selected>(capture));
            Assert.Empty(DriveSaga<Excluded>(capture));
            Assert.Empty(DriveSaga<Unselected>(capture));
        }
        else if (phase == Phase.Execute)
        {
            // The execution observer must leave the compensation pipeline untouched.
            capture.Activity!.CompensateActivityConfigured<Component, Selected>(
                Proxy<ICompensateActivityPipeConfigurator<Component, Selected>>(Forbidden));
        }
        else if (phase == Phase.Compensate)
        {
            // A compensation observer must not attach an execution filter.
            capture.Activity!.ExecuteActivityConfigured<Component, Selected>(
                Proxy<IExecuteActivityPipeConfigurator<Component, Selected>>(Forbidden));
        }
    }

    [Theory]
    [InlineData(Phase.Consume)]
    [InlineData(Phase.Send)]
    [InlineData(Phase.Publish)]
    [InlineData(Phase.Execute)]
    [InlineData(Phase.Compensate)]
    [RequirementCoverage("REQ-VSB-CONTAINER-FILTER", "selector-failure-before-observer-attachment-five-pipelines")]
    public void PublicScopedFilterSelectionFailure_PreventsEveryObserverAttachment(Phase phase)
    {
        var services = new ServiceCollection();
        _ = new ServiceCollectionBusConfigurator(services);
        using ServiceProvider provider = services.BuildServiceProvider();
        IRegistrationContext context = provider.GetRequiredService<IBusRegistrationContext>();
        var capture = new AttachmentCapture();
        IPublicPipeline pipeline = Proxy<IPublicPipeline>(capture.Invoke);
        var expected = new SelectionFailure();
        int selections = 0;
        Assert.Same(expected, Assert.Throws<SelectionFailure>(() => Attach(phase, pipeline, context, filter =>
        {
            selections++;
            filter.Include<Selected>();
            throw expected;
        })));
        Assert.Equal(1, selections);
        Assert.Equal(0, capture.ConnectionCount);
        Assert.Equal(0, capture.ConfigurationCallbacks);
        Assert.Null(capture.Consumer);
        Assert.Null(capture.Saga);
        Assert.Null(capture.Activity);
        Assert.Null(capture.Send);
        Assert.Null(capture.Publish);
    }

    private static void Attach(Phase phase, IPublicPipeline pipeline, IRegistrationContext context,
        Action<IMessageTypeFilterConfigurator> select)
    {
        switch (phase)
        {
            case Phase.Consume: pipeline.UseConsumeFilter(typeof(ConsumeFilter<>), context, select); break;
            case Phase.Send: pipeline.UseSendFilter(typeof(SendFilter<>), context, select); break;
            case Phase.Publish: pipeline.UsePublishFilter(typeof(PublishFilter<>), context, select); break;
            case Phase.Execute: pipeline.UseExecuteActivityFilter(typeof(ExecuteFilter<>), context, select); break;
            case Phase.Compensate: pipeline.UseCompensateActivityFilter(typeof(CompensateFilter<>), context, select); break;
            default: throw new ArgumentOutOfRangeException(nameof(phase));
        }
    }

    private static List<object> Drive<T>(Phase phase, AttachmentCapture capture) where T : class
    {
        var specifications = new List<object>();
        switch (phase)
        {
            case Phase.Consume:
                capture.Consumer!.ConsumerMessageConfigured<Component, T>(Proxy<ICombinedConsumerConfiguration<T>>(
                    (method, args) => Record<ConsumeContext<T>>(method, args, specifications)));
                break;
            case Phase.Send:
                capture.Send!.MessageSpecificationCreated(Proxy<IMessageSendPipeSpecification<T>>(
                    (method, args) => Record<SendContext<T>>(method, args, specifications)));
                break;
            case Phase.Publish:
                capture.Publish!.MessageSpecificationCreated(Proxy<IMessagePublishPipeSpecification<T>>(
                    (method, args) => Record<PublishContext<T>>(method, args, specifications)));
                break;
            case Phase.Execute:
                capture.Activity!.ExecuteActivityConfigured<Component, T>(Proxy<IExecuteActivityPipeConfigurator<Component, T>>((method, args) =>
                {
                    if (method.Name != "Arguments") return Forbidden(method, args);
                    ((Action<IExecuteArgumentsConfigurator<T>>)args[0]!)(Proxy<IExecuteArgumentsConfigurator<T>>(
                        (leaf, values) => Record<ExecuteContext<T>>(leaf, values, specifications)));
                    return null;
                }));
                break;
            case Phase.Compensate:
                capture.Activity!.CompensateActivityConfigured<Component, T>(Proxy<ICompensateActivityPipeConfigurator<Component, T>>((method, args) =>
                {
                    if (method.Name != "Log") return Forbidden(method, args);
                    ((Action<ICompensateLogConfigurator<T>>)args[0]!)(Proxy<ICompensateLogConfigurator<T>>(
                        (leaf, values) => Record<CompensateContext<T>>(leaf, values, specifications)));
                    return null;
                }));
                break;
            default: throw new ArgumentOutOfRangeException(nameof(phase));
        }
        return specifications;
    }

    private static List<object> DriveSaga<T>(AttachmentCapture capture) where T : class
    {
        var specifications = new List<object>();
        capture.Saga!.SagaMessageConfigured<Saga, T>(Proxy<ICombinedSagaConfiguration<T>>(
            (method, args) => Record<ConsumeContext<T>>(method, args, specifications)));
        return specifications;
    }

    private static object? Record<TContext>(MethodInfo method, object?[] args, List<object> specifications)
        where TContext : class, PipeContext
    {
        if (method.Name != "AddPipeSpecification")
            return Forbidden(method, args);
        specifications.Add(Assert.IsAssignableFrom<IPipeSpecification<TContext>>(Assert.Single(args)));
        return null;
    }

    private static T Proxy<T>(Func<MethodInfo, object?[], object?> invoke) where T : class
    {
        T proxy = DispatchProxy.Create<T, CallbackProxy>();
        ((CallbackProxy)(object)proxy).Handler = invoke;
        return proxy;
    }

    private static object? Forbidden(MethodInfo method, object?[] args) =>
        throw new Xunit.Sdk.XunitException($"Unexpected configuration operation: {method.DeclaringType}.{method.Name}");

    private sealed class AttachmentCapture
    {
        public int ConnectionCount { get; private set; }
        public int ConfigurationCallbacks { get; private set; }
        public IConsumerConfigurationObserver? Consumer { get; private set; }
        public ISagaConfigurationObserver? Saga { get; private set; }
        public IActivityConfigurationObserver? Activity { get; private set; }
        public ISendPipeSpecificationObserver? Send { get; private set; }
        public IPublishPipeSpecificationObserver? Publish { get; private set; }

        public object? Invoke(MethodInfo method, object?[] args)
        {
            switch (method.Name)
            {
                case "ConfigureSend":
                    ConfigurationCallbacks++;
                    ((Action<ISendPipeConfigurator>)args[0]!)(Proxy<ISendPipeConfigurator>(Invoke));
                    return null;
                case "ConfigurePublish":
                    ConfigurationCallbacks++;
                    ((Action<IPublishPipeConfigurator>)args[0]!)(Proxy<IPublishPipeConfigurator>(Invoke));
                    return null;
                case "ConnectConsumerConfigurationObserver":
                    Assert.Null(Consumer);
                    Consumer = Assert.IsAssignableFrom<IConsumerConfigurationObserver>(Assert.Single(args));
                    break;
                case "ConnectSagaConfigurationObserver":
                    Assert.Null(Saga);
                    Saga = Assert.IsAssignableFrom<ISagaConfigurationObserver>(Assert.Single(args));
                    break;
                case "ConnectActivityConfigurationObserver":
                    Assert.Null(Activity);
                    Activity = Assert.IsAssignableFrom<IActivityConfigurationObserver>(Assert.Single(args));
                    break;
                case "ConnectSendPipeSpecificationObserver":
                    Assert.Null(Send);
                    Send = Assert.IsAssignableFrom<ISendPipeSpecificationObserver>(Assert.Single(args));
                    break;
                case "ConnectPublishPipeSpecificationObserver":
                    Assert.Null(Publish);
                    Publish = Assert.IsAssignableFrom<IPublishPipeSpecificationObserver>(Assert.Single(args));
                    break;
                default: return Forbidden(method, args);
            }
            ConnectionCount++;
            return new Handle();
        }

        public void AssertOnlyExpectedPhase(Phase phase)
        {
            Assert.Equal(phase == Phase.Consume ? 2 : 1, ConnectionCount);
            Assert.Equal(phase is Phase.Send or Phase.Publish ? 1 : 0, ConfigurationCallbacks);
            Assert.Equal(phase == Phase.Consume, Consumer != null);
            Assert.Equal(phase == Phase.Consume, Saga != null);
            Assert.Equal(phase is Phase.Execute or Phase.Compensate, Activity != null);
            Assert.Equal(phase == Phase.Send, Send != null);
            Assert.Equal(phase == Phase.Publish, Publish != null);
        }
    }

    private sealed class Handle : ConnectHandle
    {
        public void Disconnect() { }
        public void Dispose() { }
    }

    public class CallbackProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new Xunit.Sdk.XunitException("Missing proxy method."), args ?? []);
    }

    public interface IPublicPipeline : IConsumePipeConfigurator, ISendPipelineConfigurator, IPublishPipelineConfigurator;
    public interface ICombinedConsumerConfiguration<T> : IConsumerMessageConfigurator<Component, T>, IConsumerMessageConfigurator<T> where T : class;
    public interface ICombinedSagaConfiguration<T> : ISagaMessageConfigurator<Saga, T>, ISagaMessageConfigurator<T> where T : class;
    public enum Phase { Consume, Send, Publish, Execute, Compensate }
    public sealed class Component;
    public sealed class Saga { public Guid CorrelationId { get; set; } }
    public sealed record Selected;
    public sealed record Excluded;
    public sealed record Unselected;
    private sealed class SelectionFailure : Exception;

    public sealed class ConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
    {
        public Task SendAsync(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next) => next.SendAsync(context);
        public void Probe(ProbeContext context) { }
    }
    public sealed class SendFilter<T> : IFilter<SendContext<T>> where T : class
    {
        public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next) => next.SendAsync(context);
        public void Probe(ProbeContext context) { }
    }
    public sealed class PublishFilter<T> : IFilter<PublishContext<T>> where T : class
    {
        public Task SendAsync(PublishContext<T> context, IPipe<PublishContext<T>> next) => next.SendAsync(context);
        public void Probe(ProbeContext context) { }
    }
    public sealed class ExecuteFilter<T> : IFilter<ExecuteContext<T>> where T : class
    {
        public Task SendAsync(ExecuteContext<T> context, IPipe<ExecuteContext<T>> next) => next.SendAsync(context);
        public void Probe(ProbeContext context) { }
    }
    public sealed class CompensateFilter<T> : IFilter<CompensateContext<T>> where T : class
    {
        public Task SendAsync(CompensateContext<T> context, IPipe<CompensateContext<T>> next) => next.SendAsync(context);
        public void Probe(ProbeContext context) { }
    }
}
