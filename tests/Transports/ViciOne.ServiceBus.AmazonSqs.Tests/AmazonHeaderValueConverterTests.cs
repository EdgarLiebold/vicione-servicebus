using System.Globalization;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonHeaderValueConverterTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-AWS-SNS-HEADERS", "provider-filters-observe-normalized-values-in-both-overloads")]
    public void SnsFilter_ReceivesInvariantScalarAndControlsProviderAttribute(bool typed, bool allow) =>
        VerifyFilter(true, typed, allow);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HEADERS", "provider-filters-observe-normalized-values-in-both-overloads")]
    public void SqsFilter_ReceivesInvariantScalarAndControlsProviderAttribute(bool typed, bool allow) =>
        VerifyFilter(false, typed, allow);

    private static void VerifyFilter(bool sns, bool typed, bool allow)
    {
        var observed = new List<(string Key, string Value)>();
        bool Filter(HeaderValue<string> value)
        {
            observed.Add((value.Key, value.Value));
            return allow;
        }
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            if (sns)
                Verify(new SnsHeaderValueConverter(Filter), value => (value.DataType, value.StringValue));
            else
                Verify(new SqsHeaderValueConverter(Filter), value => (value.DataType, value.StringValue));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        Assert.Equal(new[] { ("Amount", "12.5") }, observed);

        void Verify<T>(IHeaderValueConverter<T> converter, Func<T, (string Type, string Value)> project)
            where T : class
        {
            HeaderValue<T> result;
            bool converted = typed
                ? converter.TryConvert(new HeaderValue<decimal>("Amount", 12.5m), out result)
                : converter.TryConvert(new HeaderValue("Amount", 12.5m), out result);

            Assert.Equal(allow, converted);
            if (allow)
            {
                Assert.Equal("Amount", result.Key);
                Assert.NotNull(result.Value);
                Assert.Equal(("String", "12.5"), project(result.Value));
            }
            else
            {
                Assert.Equal(default(HeaderValue<T>), result);
                Assert.Null(result.Value);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SNS-HEADERS", "unsupported-values-never-invoke-provider-filter")]
    public void SnsUnsupportedValues_DoNotInvokeFilter(bool typed) => VerifyUnsupported(true, typed);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HEADERS", "unsupported-values-never-invoke-provider-filter")]
    public void SqsUnsupportedValues_DoNotInvokeFilter(bool typed) => VerifyUnsupported(false, typed);

    private static void VerifyUnsupported(bool sns, bool typed)
    {
        int calls = 0;
        bool Filter(HeaderValue<string> value)
        {
            calls++;
            return true;
        }
        if (sns)
            Verify(new SnsHeaderValueConverter(Filter));
        else
            Verify(new SqsHeaderValueConverter(Filter));
        Assert.Equal(0, calls);

        void Verify<T>(IHeaderValueConverter<T> converter)
            where T : class
        {
            foreach (object unsupported in new object[] { new object(), new byte[] { 0, 127, 255 } })
            {
                HeaderValue<T> result;
                bool converted = typed
                    ? converter.TryConvert(new HeaderValue<object>("Opaque", unsupported), out result)
                    : converter.TryConvert(new HeaderValue("Opaque", unsupported), out result);

                Assert.False(converted);
                Assert.Equal(default(HeaderValue<T>), result);
                Assert.Null(result.Value);
            }
        }
    }
}
