using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class ActivityDefinitionContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-DEFINITION", "endpoint-names-and-concurrency-boundaries")]
    public void DefinitionOptions_AcceptValidValuesAndRejectInvalidBoundaries()
    {
        var definition = new ValidActivityDefinition();

        Assert.Throws<ArgumentNullException>(() => definition.SetExecuteEndpointName(null));
        Assert.Throws<ArgumentException>(() => definition.SetExecuteEndpointName(" "));
        Assert.Throws<ArgumentNullException>(() => definition.SetCompensateEndpointName(null));
        Assert.Throws<ArgumentException>(() => definition.SetCompensateEndpointName(" "));
        Assert.Throws<ArgumentOutOfRangeException>(() => definition.SetConcurrentMessageLimit(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => definition.SetConcurrentMessageLimit(-1));

        definition.SetExecuteEndpointName("courier-execute");
        definition.SetCompensateEndpointName("courier-compensate");
        definition.SetConcurrentMessageLimit(4);
        var contract = (IActivityDefinition)definition;

        Assert.Equal("courier-execute", contract.GetExecuteEndpointName(DefaultEndpointNameFormatter.Instance));
        Assert.Equal("courier-compensate", contract.GetCompensateEndpointName(DefaultEndpointNameFormatter.Instance));
        Assert.Equal(4, definition.ConcurrentMessageLimit);
        Assert.Equal(typeof(MultiContractActivity), contract.ActivityType);
        Assert.Equal(typeof(CourierArguments), contract.ArgumentType);
        Assert.Equal(typeof(CourierLog), contract.LogType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-DEFINITION", "runtime-definition-must-match-full-generic-signature")]
    public void RuntimeDefinitionRegistration_RejectsMismatchedArgumentsAndLogForTheSameActivityType()
    {
        var services = new ServiceCollection();

        ArgumentException activityException = Assert.Throws<ArgumentException>(() =>
            services.RegisterActivity<MultiContractActivity, CourierArguments, CourierLog>(typeof(MismatchedActivityDefinition)));
        ArgumentException executeException = Assert.Throws<ArgumentException>(() =>
            services.RegisterExecuteActivity<MultiContractActivity, CourierArguments>(typeof(MismatchedExecuteActivityDefinition)));

        Assert.Equal("activityDefinitionType", activityException.ParamName);
        Assert.Contains(nameof(MismatchedActivityDefinition), activityException.Message, StringComparison.Ordinal);
        Assert.Equal("activityDefinitionType", executeException.ParamName);
        Assert.Contains(nameof(MismatchedExecuteActivityDefinition), executeException.Message, StringComparison.Ordinal);
    }

    private sealed class ValidActivityDefinition : ActivityDefinition<MultiContractActivity, CourierArguments, CourierLog>
    {
        public void SetExecuteEndpointName(string? value) => ExecuteEndpointName = value!;

        public void SetCompensateEndpointName(string? value) => CompensateEndpointName = value!;

        public void SetConcurrentMessageLimit(int? value) => ConcurrentMessageLimit = value;
    }

    private sealed class MismatchedActivityDefinition :
        ActivityDefinition<MultiContractActivity, AlternativeArguments, AlternativeLog>;

    private sealed class MismatchedExecuteActivityDefinition :
        ExecuteActivityDefinition<MultiContractActivity, AlternativeArguments>;

    private sealed record AlternativeArguments(string Value);

    private sealed record AlternativeLog(string Value);

    private sealed class MultiContractActivity :
        IActivity<CourierArguments, CourierLog>,
        IActivity<AlternativeArguments, AlternativeLog>
    {
        Task<ExecutionResult> IExecuteActivity<CourierArguments>.ExecuteAsync(ExecuteContext<CourierArguments> context) =>
            Task.FromResult(context.Completed(new CourierLog(context.Arguments.Value)));

        Task<CompensationResult> ICompensateActivity<CourierLog>.CompensateAsync(CompensateContext<CourierLog> context) =>
            Task.FromResult(context.Compensated());

        Task<ExecutionResult> IExecuteActivity<AlternativeArguments>.ExecuteAsync(ExecuteContext<AlternativeArguments> context) =>
            Task.FromResult(context.Completed(new AlternativeLog(context.Arguments.Value)));

        Task<CompensationResult> ICompensateActivity<AlternativeLog>.CompensateAsync(CompensateContext<AlternativeLog> context) =>
            Task.FromResult(context.Compensated());
    }
}
