using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Metadata;

public sealed class ActivationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-TYPE-ACTIVATION", "all-arities-preserve-type-and-arguments")]
    public void Activate_InvokesEveryStrategyArityWithTheSelectedTypeAndExactArguments()
    {
        Assert.Equal(typeof(ActivationTarget), Activation.Activate(typeof(ActivationTarget), new TypeStrategy()));
        Assert.Equal(
            (typeof(ActivationTarget), "first"),
            Activation.Activate(typeof(ActivationTarget), new OneArgumentStrategy(), "first"));
        Assert.Equal(
            (typeof(ActivationTarget), "first", 42),
            Activation.Activate(typeof(ActivationTarget), new TwoArgumentStrategy(), "first", 42));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-TYPE-ACTIVATION", "required-input-validation")]
    public void Activate_RejectsEveryMissingRequiredInput()
    {
        Action[] missingTypeCalls =
        [
            () => Activation.Activate(null!, new TypeStrategy()),
            () => Activation.Activate(null!, new OneArgumentStrategy(), "first"),
            () => Activation.Activate(null!, new TwoArgumentStrategy(), "first", 42),
        ];
        Action[] missingStrategyCalls =
        [
            () => Activation.Activate<Type>(typeof(ActivationTarget), null!),
            () => Activation.Activate<(Type, string), string>(typeof(ActivationTarget), null!, "first"),
            () => Activation.Activate<(Type, string, int), string, int>(typeof(ActivationTarget), null!, "first", 42),
        ];

        Assert.All(
            missingTypeCalls,
            call => Assert.Equal("type", Assert.Throws<ArgumentNullException>(call).ParamName));
        Assert.All(
            missingStrategyCalls,
            call => Assert.Equal("activationType", Assert.Throws<ArgumentNullException>(call).ParamName));
    }

    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(OpenActivationTarget<>))]
    [RequirementCoverage("REQ-VSB-RUNTIME-TYPE-ACTIVATION", "closed-reference-type-required")]
    public void Activate_RejectsTypesThatCannotSatisfyTheStrategyContract(Type invalidType)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Activation.Activate(invalidType, new TypeStrategy()));

        Assert.Equal("type", exception.ParamName);
    }

    readonly struct TypeStrategy :
        IActivationType<Type>
    {
        public Type ActivateType<T>()
            where T : class
        {
            return typeof(T);
        }
    }

    readonly struct OneArgumentStrategy :
        IActivationType<(Type Type, string Argument), string>
    {
        public (Type Type, string Argument) ActivateType<T>(string arg1)
            where T : class
        {
            return (typeof(T), arg1);
        }
    }

    readonly struct TwoArgumentStrategy :
        IActivationType<(Type Type, string First, int Second), string, int>
    {
        public (Type Type, string First, int Second) ActivateType<T>(string arg1, int arg2)
            where T : class
        {
            return (typeof(T), arg1, arg2);
        }
    }

    sealed class ActivationTarget;

    sealed class OpenActivationTarget<T>;
}
