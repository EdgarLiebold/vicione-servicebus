using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

public sealed class PropertyProviderFactoryObjectGraphTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-OBJECT-GRAPH", "single-nested-and-array-contracts")]
    public async Task AnonymousSources_InitializeSingleNestedAndArrayContracts()
    {
        var singleInput = new { Message = new { Text = "Hello" } };
        var nestedInput = new
        {
            Message = new
            {
                Text = "Hello",
                Headers = new object[]
                {
                    new { Key = "Format", Value = "CSV" },
                    new { Key = "Length", Value = 2457 },
                },
            },
        };
        var arrayInput = new { Messages = new[] { new { Text = "Hello" }, new { Text = "World" } } };

        SimpleMessageContract single = await PropertyProviderTestContext.For(singleInput)
            .ReadAsync<SimpleMessageContract>(nameof(singleInput.Message));
        NestedMessageContract nested = await PropertyProviderTestContext.For(nestedInput)
            .ReadAsync<NestedMessageContract>(nameof(nestedInput.Message));
        SimpleMessageContract[] messages = await PropertyProviderTestContext.For(arrayInput)
            .ReadAsync<SimpleMessageContract[]>(nameof(arrayInput.Messages));

        Assert.Equal("Hello", single.Text);
        Assert.Equal("Hello", nested.Text);
        Assert.Equal(2, nested.Headers.Length);
        Assert.Equal("Format", nested.Headers[0].Key);
        Assert.Equal("CSV", nested.Headers[0].Value);
        Assert.Equal("Length", nested.Headers[1].Key);
        Assert.Equal("2457", nested.Headers[1].Value);
        Assert.Equal(2, messages.Length);
        Assert.Equal("Hello", messages[0].Text);
        Assert.Equal("World", messages[1].Text);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-EXCEPTION", "exception-info")]
    public async Task ExceptionSource_ProducesObservableExceptionInformation()
    {
        var input = new ExceptionInput(new InvalidOperationException("Expected failure"));

        ExceptionInfo information = await PropertyProviderTestContext.For(input)
            .ReadAsync<ExceptionInfo>(nameof(ExceptionInput.Value));

        Assert.Equal(TypeCache<InvalidOperationException>.ShortName, information.ExceptionType);
        Assert.Equal("Expected failure", information.Message);
        Assert.Null(information.InnerException);
    }

    public interface SimpleMessageContract
    {
        string Text { get; }
    }

    public interface NestedMessageContract
    {
        string Text { get; }

        MessageHeader[] Headers { get; }
    }

    public interface MessageHeader
    {
        string Key { get; }

        string Value { get; }
    }

    private sealed record ExceptionInput(Exception Value);
}
