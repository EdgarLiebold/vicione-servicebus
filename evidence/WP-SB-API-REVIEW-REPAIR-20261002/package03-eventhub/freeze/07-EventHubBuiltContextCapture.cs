using System.Reflection;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

// Observation adapter for normal production Build. This replaces the old
// CaptureSpecification; it never calls CreateReceiveEndpointContext.
// It decorates the existing public registration factory/specification and
// observes the single context the normal production Build constructs.
public sealed class EventHubBuiltContextCapture : IRegistrationRiderFactory<IEventHubRider>
{
    private readonly EventHubRegistrationRiderFactory _inner;
    private readonly MessageLimits _limits;
    private readonly List<IEventHubReceiveEndpointContext> _contexts = [];

    public EventHubBuiltContextCapture(MessageLimits limits,
        Action<IRiderRegistrationContext, IEventHubFactoryConfigurator> configure)
    {
        _limits = limits;
        _inner = new EventHubRegistrationRiderFactory(configure);
    }

    // Read this only after normal IBusControl resolution/composition validation.
    // The public builder adds its ReceiveSettings payload after the constructor
    // hook returns; the hook itself must not read it prematurely.
    public IEventHubReceiveEndpointContext Context => Assert.Single(_contexts);
    public int ContextCount => _contexts.Count;

    public IBusInstanceSpecification CreateRider(IRiderRegistrationContext context)
        => new ObservingSpecification(_inner.CreateRider(context), this);

    private sealed class ObservingSpecification(IBusInstanceSpecification inner,
        EventHubBuiltContextCapture capture) : IBusInstanceSpecification
    {
        public IEnumerable<ValidationResult> Validate() => inner.Validate();

        public void Configure(IBusInstance actual)
        {
            IHostConfiguration host = DispatchProxy.Create<IHostConfiguration, HostProxy>();
            var hostProxy = (HostProxy)(object)host;
            hostProxy.Actual = actual.HostConfiguration;
            hostProxy.Capture = capture;
            IBusInstance bus = DispatchProxy.Create<IBusInstance, BusProxy>();
            var busProxy = (BusProxy)(object)bus;
            busProxy.Actual = actual;
            busProxy.ObservedHost = host;
            inner.Configure(bus); // Production builds and attaches the actual rider exactly once.
        }
    }

    public class BusProxy : DispatchProxy
    {
        public IBusInstance Actual { get; set; } = null!;
        public IHostConfiguration ObservedHost { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(method);
            return method.Name == "get_HostConfiguration"
                ? ObservedHost
                : ForwardPublicInterface(method, Actual, args);
        }
    }

    public class HostProxy : DispatchProxy
    {
        public IHostConfiguration Actual { get; set; } = null!;
        internal EventHubBuiltContextCapture Capture { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(method);
            if (method.Name == nameof(IHostConfiguration.ConnectReceiveEndpointContext))
            {
                var context = Assert.IsAssignableFrom<ReceiveEndpointContext>(Assert.Single(args!));
                if (context is IEventHubReceiveEndpointContext eventHubContext)
                {
                    // IHostConfiguration's public proxy cannot retain the internal
                    // IMessageLimitsHostConfiguration runtime interface. Preserve
                    // the explicitly selected bus limits using the public payload
                    // SPI before delegating the normal host connector. This is a
                    // stated test-adapter projection, not a product fix or seam.
                    // It never supplies or modifies ReceiveSettings/client factory.
                    Assert.Same(Capture._limits, context.GetOrAddPayload(() => Capture._limits));
                    Capture._contexts.Add(eventHubContext);
                }
            }
            return ForwardPublicInterface(method, Actual, args);
        }
    }

    private static object? ForwardPublicInterface(MethodInfo method, object actual, object?[]? args)
    {
        // DispatchProxy supplies the public interface method. No private member
        // is located or read. Preserve original failure identity/stack instead
        // of leaking MethodInfo.Invoke's TargetInvocationException wrapper.
        try { return method.Invoke(actual, args); }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
