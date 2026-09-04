using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.TypeConverters;

public sealed class ExceptionTypeConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-EXCEPTION-CONVERSION", "exception-info")]
    public async Task ExceptionInput_ProducesObservableExceptionInformationAsync()
    {
        var exception = new InvalidOperationException("Expected failure");

        InitializeContext<ExceptionMessage> context = await MessageInitializerCache<ExceptionMessage>.InitializeAsync(
            new { Exception = exception },
            TestContext.Current.CancellationToken);

        Assert.NotNull(context.Message.Exception);
        Assert.Equal(TypeCache<InvalidOperationException>.ShortName, context.Message.Exception.ExceptionType);
        Assert.Equal("Expected failure", context.Message.Exception.Message);
        Assert.Null(context.Message.Exception.InnerException);
    }

    public interface ExceptionMessage
    {
        ExceptionInfo Exception { get; }
    }
}
