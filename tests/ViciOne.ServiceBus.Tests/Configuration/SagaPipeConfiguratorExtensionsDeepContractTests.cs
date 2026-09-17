using System.Reflection;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaPipeConfiguratorExtensionsDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "exact-use-filter-public-surface")]
    public void UseFilter_ExposesTheExactGenericVoidExtensionSurface()
    {
        Type extensions = typeof(SagaPipeConfiguratorExtensions);

        Assert.True(extensions.IsPublic);
        Assert.True(extensions.IsAbstract);
        Assert.True(extensions.IsSealed);

        MethodInfo method = Assert.Single(
            extensions.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Equal(nameof(SagaPipeConfiguratorExtensions.UseFilter), method.Name);
        Assert.Equal(typeof(void), method.ReturnType);
        Assert.True(method.IsDefined(typeof(ExtensionAttribute), inherit: false));

        Type[] genericArguments = method.GetGenericArguments();
        Assert.Collection(
            genericArguments,
            saga =>
            {
                Assert.Equal("TSaga", saga.Name);
                Assert.Equal(
                    GenericParameterAttributes.ReferenceTypeConstraint,
                    saga.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
                Assert.Equal([typeof(ISaga)], saga.GetGenericParameterConstraints());
            },
            message =>
            {
                Assert.Equal("T", message.Name);
                Assert.Equal(
                    GenericParameterAttributes.ReferenceTypeConstraint,
                    message.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
                Assert.Empty(message.GetGenericParameterConstraints());
            });

        Type sagaContext = typeof(SagaConsumeContext<,>).MakeGenericType(genericArguments);
        Type sagaFilterContext = typeof(SagaConsumeContext<>).MakeGenericType(genericArguments[0]);
        Assert.Collection(
            method.GetParameters(),
            configurator =>
            {
                Assert.Equal("configurator", configurator.Name);
                Assert.Equal(typeof(IPipeConfigurator<>).MakeGenericType(sagaContext), configurator.ParameterType);
            },
            filter =>
            {
                Assert.Equal("filter", filter.Name);
                Assert.Equal(typeof(IFilter<>).MakeGenericType(sagaFilterContext), filter.ParameterType);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "required-guard-order-and-zero-effects")]
    public void UseFilter_RejectsRequiredInputsInSignatureOrderBeforeAnyConfiguratorEffect()
    {
        var configurator = new RecordingConfigurator();

        AssertArgument("configurator", () =>
            SagaPipeConfiguratorExtensions.UseFilter<FilterSaga, FilterMessage>(null!, null!));
        AssertArgument("filter", () =>
            SagaPipeConfiguratorExtensions.UseFilter<FilterSaga, FilterMessage>(configurator, null!));

        Assert.Empty(configurator.Specifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "single-saga-filter-specification-and-filter-identity")]
    public void UseFilter_AddsExactlyOneSagaFilterSpecificationContainingTheSameFilter()
    {
        var configurator = new RecordingConfigurator();
        var filter = new RecordingFilter();

        SagaPipeConfiguratorExtensions.UseFilter<FilterSaga, FilterMessage>(configurator, filter);

        SagaFilterSpecification<FilterSaga, FilterMessage> specification = AssertSingleSpecification(configurator);
        AssertSpecificationFilter(specification, filter);
        Assert.Empty(specification.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "add-failure-identity-and-single-attempt")]
    public void UseFilter_WhenAddFails_PropagatesTheSameFailureAfterOneExactAddAttempt()
    {
        var failure = new InvalidOperationException("add failed");
        var configurator = new RecordingConfigurator(failure);
        var filter = new RecordingFilter();

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() =>
            SagaPipeConfiguratorExtensions.UseFilter<FilterSaga, FilterMessage>(configurator, filter));

        Assert.Same(failure, thrown);
        SagaFilterSpecification<FilterSaga, FilterMessage> specification = AssertSingleSpecification(configurator);
        AssertSpecificationFilter(specification, filter);
    }

    static SagaFilterSpecification<FilterSaga, FilterMessage> AssertSingleSpecification(
        RecordingConfigurator configurator) =>
        Assert.IsType<SagaFilterSpecification<FilterSaga, FilterMessage>>(Assert.Single(configurator.Specifications));

    static void AssertSpecificationFilter(
        SagaFilterSpecification<FilterSaga, FilterMessage> specification,
        IFilter<SagaConsumeContext<FilterSaga>> expected)
    {
        FieldInfo filterField = typeof(SagaFilterSpecification<FilterSaga, FilterMessage>).GetField(
            "_filter",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The saga filter specification does not expose its owned filter field.");

        Assert.Same(expected, filterField.GetValue(specification));
    }

    static void AssertArgument(string parameterName, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    sealed class RecordingConfigurator(Exception? failure = null) :
        IPipeConfigurator<SagaConsumeContext<FilterSaga, FilterMessage>>
    {
        public List<IPipeSpecification<SagaConsumeContext<FilterSaga, FilterMessage>>> Specifications { get; } = [];

        public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<FilterSaga, FilterMessage>> specification)
        {
            Specifications.Add(specification);

            if (failure != null)
                throw failure;
        }
    }

    sealed class RecordingFilter : IFilter<SagaConsumeContext<FilterSaga>>
    {
        public Task SendAsync(
            SagaConsumeContext<FilterSaga> context,
            IPipe<SagaConsumeContext<FilterSaga>> next) =>
            next.SendAsync(context);

        public void Probe(ProbeContext context)
        {
        }
    }

    sealed class FilterSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    sealed record FilterMessage;
}
