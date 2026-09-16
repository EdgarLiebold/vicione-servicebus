using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class ActivityEndpointConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ENDPOINT-CONFIGURATION", "activity-endpoint-definitions-preserve-settings-and-naming")]
    public void ActivityEndpointDefinitions_PreserveSettingsNamingAndBoundaries()
    {
        AssertParameter("settings", () => new ExecuteActivityEndpointDefinition<TestActivity, Arguments>(null!));
        AssertParameter("settings", () => new CompensateActivityEndpointDefinition<TestActivity, Log>(null!));

        var executeSettings = new EndpointSettings<IEndpointDefinition<IExecuteActivity<Arguments>>>
        {
            Name = "courier-execute",
            InstanceId = "node",
            IsTemporary = true,
            PrefetchCount = 17,
            ConcurrentMessageLimit = 9,
            ConfigureConsumeTopology = false,
        };
        var compensateSettings = new EndpointSettings<IEndpointDefinition<ICompensateActivity<Log>>>();
        var execute = new ExecuteActivityEndpointDefinition<TestActivity, Arguments>(executeSettings);
        var generatedExecute = new ExecuteActivityEndpointDefinition<TestActivity, Arguments>(
            new EndpointSettings<IEndpointDefinition<IExecuteActivity<Arguments>>>());
        var compensate = new CompensateActivityEndpointDefinition<TestActivity, Log>(compensateSettings);
        IEndpointNameFormatter formatter = KebabCaseEndpointNameFormatter.Instance;

        Assert.Equal(formatter.SanitizeName("courier-execute" + formatter.Separator + "node"), execute.GetEndpointName(formatter));
        Assert.True(execute.IsTemporary);
        Assert.Equal(17, execute.PrefetchCount);
        Assert.Equal(9, execute.ConcurrentMessageLimit);
        Assert.False(execute.ConfigureConsumeTopology);
        Assert.Equal(formatter.ExecuteActivity<TestActivity, Arguments>(), generatedExecute.GetEndpointName(formatter));
        Assert.Equal(formatter.CompensateActivity<TestActivity, Log>(), compensate.GetEndpointName(formatter));
        AssertParameter("formatter", () => execute.GetEndpointName(null!));
        AssertParameter("formatter", () => compensate.GetEndpointName(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ENDPOINT-CONFIGURATION", "derived-endpoint-name-is-published-once-under-contention")]
    public async Task DerivedEndpointName_IsPublishedOnceUnderContentionAsync()
    {
        var definition = new BlockingEndpointDefinition(new PermissiveSettings());
        IEndpointNameFormatter formatter = DefaultEndpointNameFormatter.Instance;
        const int workerCount = 16;
        using var ready = new CountdownEvent(workerCount);
        using var start = new ManualResetEventSlim();
        Task<string>[] tasks = Enumerable.Range(0, workerCount).Select(_ => Task.Run(() =>
        {
            ready.Signal();
            start.Wait(TestContext.Current.CancellationToken);
            return definition.GetEndpointName(formatter);
        }, TestContext.Current.CancellationToken)).ToArray();

        Assert.True(ready.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        start.Set();
        Assert.True(definition.FormatEntered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        SpinWait.SpinUntil(() => definition.FormatCalls > 1, TimeSpan.FromMilliseconds(250));
        definition.ReleaseFormat.Set();
        string[] names = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.All(names, name => Assert.Equal("atomic-endpoint", name));
        Assert.Equal(1, definition.FormatCalls);
        Assert.Equal("atomic-endpoint", definition.GetEndpointName(formatter));
        Assert.Equal(1, definition.FormatCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ENDPOINT-CONFIGURATION", "endpoint-configuration-validates-before-forwarding")]
    public void EndpointConfiguration_ValidatesBeforeForwarding()
    {
        var settings = new PermissiveSettings();
        var definition = new BlockingEndpointDefinition(settings);
        IReceiveEndpointConfigurator endpoint = Proxy<IReceiveEndpointConfigurator>();
        IRegistrationContext context = Proxy<IRegistrationContext>();

        AssertParameter("configurator", () => definition.Configure<IReceiveEndpointConfigurator>(null!, context));
        Assert.Equal(0, settings.ConfigureCalls);

        definition.Configure(endpoint, context);

        Assert.Equal(1, settings.ConfigureCalls);
        Assert.Same(endpoint, settings.Configurator);
        Assert.Same(context, settings.Context);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ENDPOINT-CONFIGURATION", "activity-pipeline-adapters-validate-and-forward-specifications")]
    public void ActivityPipelineAdapters_ValidateAndForwardSpecifications()
    {
        IPipeConfigurator<ExecuteActivityContext<TestActivity, Arguments>> executePipe =
            PipeProxy<ExecuteActivityContext<TestActivity, Arguments>>(out PipeRecorder<ExecuteActivityContext<TestActivity, Arguments>> execute);
        IPipeConfigurator<CompensateActivityContext<TestActivity, Log>> compensatePipe =
            PipeProxy<CompensateActivityContext<TestActivity, Log>>(out PipeRecorder<CompensateActivityContext<TestActivity, Log>> compensate);
        var executeAdapter = new ExecuteActivityArgumentsConfigurator<TestActivity, Arguments>(executePipe);
        var compensateAdapter = new CompensateActivityLogConfigurator<TestActivity, Log>(compensatePipe);
        IPipeSpecification<ExecuteActivityContext<Arguments>> executeSpecification =
            Proxy<IPipeSpecification<ExecuteActivityContext<Arguments>>>();
        IPipeSpecification<CompensateActivityContext<Log>> compensateSpecification =
            Proxy<IPipeSpecification<CompensateActivityContext<Log>>>();

        AssertParameter("configurator", () => new ExecuteActivityArgumentsConfigurator<TestActivity, Arguments>(null!));
        AssertParameter("configurator", () => new CompensateActivityLogConfigurator<TestActivity, Log>(null!));
        AssertParameter("specification", () => executeAdapter.AddPipeSpecification(null!));
        AssertParameter("specification", () => compensateAdapter.AddPipeSpecification(null!));

        executeAdapter.AddPipeSpecification(executeSpecification);
        compensateAdapter.AddPipeSpecification(compensateSpecification);

        Assert.IsType<PipeConfigurator<ExecuteActivityContext<TestActivity, Arguments>>
            .SplitFilterPipeSpecification<ExecuteActivityContext<Arguments>>>(Assert.Single(execute.Specifications));
        Assert.IsType<PipeConfigurator<CompensateActivityContext<TestActivity, Log>>
            .SplitFilterPipeSpecification<CompensateActivityContext<Log>>>(Assert.Single(compensate.Specifications));
    }

    private static T Proxy<T>()
        where T : class => DispatchProxy.Create<T, PassiveProxy>();

    private static IPipeConfigurator<T> PipeProxy<T>(out PipeRecorder<T> recorder)
        where T : class, PipeContext
    {
        IPipeConfigurator<T> pipe = DispatchProxy.Create<IPipeConfigurator<T>, PipeRecorder<T>>();
        recorder = (PipeRecorder<T>)(object)pipe;
        return pipe;
    }

    private static void AssertParameter(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    private sealed record Arguments(string Value);

    private sealed record Log(string Value);

    private sealed class Marker;

    private sealed class TestActivity : IActivity<Arguments, Log>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) => throw new NotSupportedException();

        public Task<CompensationResult> CompensateAsync(CompensateContext<Log> context) => throw new NotSupportedException();
    }

    private sealed class BlockingEndpointDefinition(PermissiveSettings settings) : SettingsEndpointDefinition<Marker>(settings)
    {
        int _formatCalls;

        public int FormatCalls => Volatile.Read(ref _formatCalls);

        public ManualResetEventSlim FormatEntered { get; } = new();

        public ManualResetEventSlim ReleaseFormat { get; } = new();

        protected override string FormatEndpointName(IEndpointNameFormatter formatter)
        {
            Interlocked.Increment(ref _formatCalls);
            FormatEntered.Set();
            ReleaseFormat.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            return "atomic-endpoint";
        }
    }

    private sealed class PermissiveSettings : IEndpointSettings<IEndpointDefinition<Marker>>
    {
        public string? Name => null;

        public bool IsTemporary => false;

        public int? PrefetchCount => null;

        public int? ConcurrentMessageLimit => null;

        public bool ConfigureConsumeTopology => true;

        public string? InstanceId => null;

        public int ConfigureCalls { get; private set; }

        public IReceiveEndpointConfigurator? Configurator { get; private set; }

        public IRegistrationContext? Context { get; private set; }

        public void ConfigureEndpoint<TEndpointConfigurator>(TEndpointConfigurator configurator, IRegistrationContext? context)
            where TEndpointConfigurator : IReceiveEndpointConfigurator
        {
            ConfigureCalls++;
            Configurator = configurator;
            Context = context;
        }
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private class PipeRecorder<T> : DispatchProxy
        where T : class, PipeContext
    {
        public List<object> Specifications { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IPipeConfigurator<T>.AddPipeSpecification))
            {
                Specifications.Add(args![0]!);
                return null;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }
}
