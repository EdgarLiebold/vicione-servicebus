using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Events;

public sealed class FaultExceptionInfoTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "empty-application-data-is-non-null-and-writable")]
    public void ApplicationDiagnosticData_IsAlwaysAvailableAndBacksExceptionData()
    {
        var wrapper = new FaultDataException(new InvalidOperationException("source"));

        Assert.Empty(wrapper.ApplicationData);
        wrapper.ApplicationData.Add("TraceId", "trace-27");

        Assert.Same(wrapper.ApplicationData, wrapper.Data);
        Assert.Equal("trace-27", wrapper.Data["traceid"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "imported-exception-data-is-case-insensitive")]
    public void ImportedExceptionData_RemainsCaseInsensitiveWithoutApplicationOverrides()
    {
        var source = new InvalidOperationException("source");
        source.Data["TraceId"] = "trace-27";

        var wrapper = new FaultDataException(source);

        Assert.Equal("trace-27", wrapper.ApplicationData["traceid"]);
        Assert.Throws<ArgumentException>(() => wrapper.ApplicationData.Add("TRACEID", "duplicate"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "fault-data-required-arguments")]
    public void FaultDataConstruction_RejectsEveryMissingRequiredArgument()
    {
        var failure = new InvalidOperationException("source");
        KeyValuePair<string, object>[] values = [];

        Assert.Equal("innerException", Assert.Throws<ArgumentNullException>(() => new FaultDataException(null!)).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => new FaultDataException(failure, (object)null!)).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() =>
            new FaultDataException(failure, (IEnumerable<KeyValuePair<string, object>>)null!)).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() => new FaultDataException(null!, failure)).ParamName);
        Assert.Equal("innerException", Assert.Throws<ArgumentNullException>(() => new FaultDataException("fault", null!)).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => new FaultDataException("fault", failure, (object)null!)).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() =>
            new FaultDataException("fault", failure, (IEnumerable<KeyValuePair<string, object>>)null!)).ParamName);
        Assert.Equal("innerException", Assert.Throws<ArgumentNullException>(() => new FaultDataException("fault", null!, values)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "all-wrapper-constructors-preserve-message-inner-data-and-precedence")]
    public void FaultDataConstruction_PreservesEveryConstructorContractAndDataPrecedence()
    {
        var source = new InvalidOperationException("source-message");
        source.Data["InnerOnly"] = "inner";
        source.Data["Shared"] = "inner-shared";
        source.Data[17] = "ignored-non-string-key";
        source.Data["IgnoredNull"] = null;
        var objectValues = new { Shared = "object-shared", ObjectOnly = 23 };
        KeyValuePair<string, object>[] pairValues =
        [
            new("shared", "pair-shared"),
            new("PairOnly", true),
        ];

        var basic = new FaultDataException(source);
        var fromObject = new FaultDataException(source, objectValues);
        var fromPairs = new FaultDataException(source, pairValues);
        var messageOnly = new FaultDataException("explicit-message", source);
        var messageObject = new FaultDataException("explicit-object", source, objectValues);
        var messagePairs = new FaultDataException("explicit-pairs", source, pairValues);

        AssertWrapper(basic, "source-message", source, "inner-shared", "InnerOnly");
        AssertWrapper(fromObject, "source-message", source, "object-shared", "InnerOnly", "ObjectOnly");
        AssertWrapper(fromPairs, "source-message", source, "pair-shared", "InnerOnly", "PairOnly");
        AssertWrapper(messageOnly, "explicit-message", source, "inner-shared", "InnerOnly");
        AssertWrapper(messageObject, "explicit-object", source, "object-shared", "InnerOnly", "ObjectOnly");
        AssertWrapper(messagePairs, "explicit-pairs", source, "pair-shared", "InnerOnly", "PairOnly");

        Assert.Equal(23, fromObject.ApplicationData["objectonly"]);
        Assert.True(Assert.IsType<bool>(fromPairs.ApplicationData["paironly"]));
        Assert.DoesNotContain(basic.ApplicationData, entry => entry.Key == "IgnoredNull");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "mutable-type-identifiers-are-snapshotted")]
    public void FaultConstruction_CopiesMutableMessageTypeCollections()
    {
        string[] faultMessageTypes = ["urn:message:original"];
        var fault = new FaultEvent<DiagnosticFailure>(
            new DiagnosticFailure(FailureSource.ExceptionData),
            null,
            HostMetadataCache.Host,
            new InvalidOperationException("source"),
            faultMessageTypes);
        var receiveFault = new ReceiveFaultEvent(
            HostMetadataCache.Host,
            new InvalidOperationException("source"),
            "application/json",
            null,
            faultMessageTypes);

        faultMessageTypes[0] = "urn:message:mutated";

        Assert.Equal(["urn:message:original"], fault.FaultMessageTypes);
        Assert.Equal(["urn:message:original"], receiveFault.FaultMessageTypes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "fault-events-project-identities-payload-content-type-and-clock")]
    public void FaultEvents_ProjectIdentitiesPayloadContentTypeAndClockExactly()
    {
        var timestamp = new DateTimeOffset(2041, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(timestamp);
        var messageId = Guid.Parse("4d3858bd-7839-4d83-a32a-ea64ee747423");
        var message = new DiagnosticFailure(FailureSource.ExceptionData);
        var failure = new InvalidOperationException("source");
        string[] messageTypes = ["urn:message:diagnostic"];

        var fault = new FaultEvent<DiagnosticFailure>(
            message,
            messageId,
            HostMetadataCache.Host,
            failure,
            messageTypes,
            timeProvider);
        var receiveFault = new ReceiveFaultEvent(
            HostMetadataCache.Host,
            failure,
            "application/vnd.vicione.failure+json",
            messageId,
            messageTypes,
            timeProvider);

        Assert.NotEqual(Guid.Empty, fault.FaultId);
        Assert.NotEqual(Guid.Empty, receiveFault.FaultId);
        Assert.NotEqual(fault.FaultId, receiveFault.FaultId);
        Assert.Equal(messageId, fault.FaultedMessageId);
        Assert.Equal(messageId, receiveFault.FaultedMessageId);
        Assert.Equal(timestamp, fault.Timestamp);
        Assert.Equal(timestamp, receiveFault.Timestamp);
        Assert.Same(message, fault.Message);
        Assert.Same(HostMetadataCache.Host, fault.Host);
        Assert.Same(HostMetadataCache.Host, receiveFault.Host);
        Assert.Equal("application/vnd.vicione.failure+json", receiveFault.ContentType);
        Assert.Equal(messageTypes, fault.FaultMessageTypes);
        Assert.Equal(messageTypes, receiveFault.FaultMessageTypes);
        Assert.Equal("source", Assert.Single(fault.Exceptions).Message);
        Assert.Equal("source", Assert.Single(receiveFault.Exceptions).Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "fault-contract-required-values")]
    public void FaultConstruction_RejectsEveryMissingRequiredValue()
    {
        var message = new DiagnosticFailure(FailureSource.ExceptionData);
        var failure = new InvalidOperationException("source");
        ExceptionInfo exceptionInfo = new StubExceptionInfo("Failure", "source", null);

        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
            new FaultEvent<DiagnosticFailure>(null!, null, HostMetadataCache.Host, failure, [])).ParamName);
        Assert.Equal("host", Assert.Throws<ArgumentNullException>(() =>
            new FaultEvent<DiagnosticFailure>(message, null, null!, failure, [])).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() =>
            new FaultEvent<DiagnosticFailure>(message, null, HostMetadataCache.Host, (Exception)null!, [])).ParamName);
        Assert.Equal("exceptions", Assert.Throws<ArgumentNullException>(() =>
            new FaultEvent<DiagnosticFailure>(message, null, HostMetadataCache.Host, (IEnumerable<ExceptionInfo>)null!, [])).ParamName);
        Assert.Equal("faultMessageTypes", Assert.Throws<ArgumentNullException>(() =>
            new FaultEvent<DiagnosticFailure>(message, null, HostMetadataCache.Host, failure, null!)).ParamName);
        Assert.Equal("exceptions", Assert.Throws<ArgumentException>(() =>
            new FaultEvent<DiagnosticFailure>(message, null, HostMetadataCache.Host, new[] { exceptionInfo, null! }, [])).ParamName);
        Assert.Equal("faultMessageTypes", Assert.Throws<ArgumentException>(() =>
            new FaultEvent<DiagnosticFailure>(message, null, HostMetadataCache.Host, failure, ["valid", null!])).ParamName);
        Assert.Equal("host", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveFaultEvent(null!, failure, null, null, null)).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveFaultEvent(HostMetadataCache.Host, null!, null, null, null)).ParamName);
        Assert.Equal("faultMessageTypes", Assert.Throws<ArgumentException>(() =>
            new ReceiveFaultEvent(HostMetadataCache.Host, failure, null, null, ["valid", null!])).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "application-data-transport")]
    public async Task ApplicationDiagnosticData_IsCarriedByExactlyOnePublishedFaultAsync()
    {
        Fault<DiagnosticFailure> fault = await PublishFaultAsync(FailureSource.ApplicationWrapper);
        ExceptionInfo exception = Assert.Single(fault.Exceptions);

        Assert.Equal(TypeCache<DiagnosticFailureException>.ShortName, exception.ExceptionType);
        Assert.Equal("Frank", Assert.IsType<string>(exception.Data!["USERNAME"]));
        Assert.Equal(27L, Assert.IsType<long>(exception.Data["customerid"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "exception-data-transport")]
    public async Task ExceptionDiagnosticData_IsCarriedByExactlyOnePublishedFaultAsync()
    {
        Fault<DiagnosticFailure> fault = await PublishFaultAsync(FailureSource.ExceptionData);
        ExceptionInfo exception = Assert.Single(fault.Exceptions);

        Assert.Equal(TypeCache<DiagnosticFailureException>.ShortName, exception.ExceptionType);
        Assert.Equal("Frank", Assert.IsType<string>(exception.Data!["USERNAME"]));
        Assert.Equal(27L, Assert.IsType<long>(exception.Data["customerid"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "detached-snapshot")]
    public void Construction_DetachesDiagnosticDataFromTheSourceException()
    {
        var source = new InvalidOperationException("source");
        source.Data["State"] = "before";

        var snapshot = new FaultExceptionInfo(source);
        source.Data["State"] = "after";
        source.Data["AddedLater"] = true;

        Assert.Equal("before", snapshot.Data!["state"]);
        Assert.False(snapshot.Data.ContainsKey("AddedLater"));
        Assert.NotSame(source.Data, snapshot.Data);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "wrapper-precedence")]
    public void ApplicationDiagnosticData_WinsOverWrappedExceptionDataCaseInsensitively()
    {
        var source = new InvalidOperationException("source");
        source.Data["TraceId"] = "inner";
        var wrapper = new FaultDataException(
            source,
            new[] { new KeyValuePair<string, object>("traceid", "application") });

        var snapshot = new FaultExceptionInfo(wrapper);

        Assert.Equal(TypeCache<InvalidOperationException>.ShortName, snapshot.ExceptionType);
        Assert.Equal("application", snapshot.Data!["TRACEID"]);
        Assert.Single(snapshot.Data);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "serializable-entry-boundary")]
    public void Construction_IgnoresNonStringKeysAndNullValues()
    {
        var source = new InvalidOperationException("source");
        source.Data[42] = "not a wire key";
        source.Data["NullValue"] = null;
        source.Data["Kept"] = 27;

        var snapshot = new FaultExceptionInfo(source);

        KeyValuePair<string, object> entry = Assert.Single(snapshot.Data!);
        Assert.Equal("Kept", entry.Key);
        Assert.Equal(27, entry.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "remote-type-and-inner-chain")]
    public void RemoteExceptionInfo_PreservesReportedTypesAndTheInnerChain()
    {
        var remote = new StubExceptionInfo(
            "Remote.OuterException",
            "outer",
            new StubExceptionInfo("Remote.InnerException", "inner", null));

        var snapshot = new FaultExceptionInfo(new ExceptionInfoException(remote));

        Assert.Equal("Remote.OuterException", snapshot.ExceptionType);
        Assert.Equal("outer", snapshot.Message);
        Assert.NotNull(snapshot.InnerException);
        Assert.Equal("Remote.InnerException", snapshot.InnerException.ExceptionType);
        Assert.Equal("inner", snapshot.InnerException.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "aggregate-exception-count-boundary")]
    public void AggregateFaults_AreLimitedToTheFirstSixteenExceptions()
    {
        var exceptions = Enumerable.Range(0, 17)
            .Select(index => new InvalidOperationException($"failure-{index}"))
            .ToArray();

        var fault = new FaultEvent<DiagnosticFailure>(
            new DiagnosticFailure(FailureSource.ExceptionData),
            null,
            HostMetadataCache.Host,
            new AggregateException(exceptions),
            []);

        Assert.Equal(16, fault.Exceptions.Length);
        Assert.Collection(
            fault.Exceptions,
            Enumerable.Range(0, 16)
                .Select<int, Action<ExceptionInfo>>(index => exception => Assert.Equal($"failure-{index}", exception.Message))
                .ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "explicit-exception-count-boundary")]
    public void ExplicitFaultExceptionCollections_AreLimitedToTheFirstSixteenExceptions()
    {
        ExceptionInfo[] exceptions = Enumerable.Range(0, 17)
            .Select(index => (ExceptionInfo)new StubExceptionInfo($"Remote.Type{index}", $"failure-{index}", null))
            .ToArray();

        var fault = new FaultEvent<DiagnosticFailure>(
            new DiagnosticFailure(FailureSource.ExceptionData),
            null,
            HostMetadataCache.Host,
            exceptions,
            []);

        Assert.Equal(16, fault.Exceptions.Length);
        Assert.Same(exceptions[0], fault.Exceptions[0]);
        Assert.Same(exceptions[15], fault.Exceptions[15]);
        Assert.DoesNotContain(exceptions[16], fault.Exceptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "explicit-exception-collection-null-guard")]
    public void ExplicitFaultExceptionCollections_RejectNull()
    {
        Assert.Throws<ArgumentNullException>(() => new FaultEvent<DiagnosticFailure>(
            new DiagnosticFailure(FailureSource.ExceptionData),
            null,
            HostMetadataCache.Host,
            (IEnumerable<ExceptionInfo>)null!,
            []));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "receive-exception-count-boundary")]
    public void ReceiveFaults_AreLimitedToTheFirstSixteenExceptions()
    {
        var exceptions = Enumerable.Range(0, 17)
            .Select(index => new InvalidOperationException($"failure-{index}"))
            .ToArray();

        var fault = new ReceiveFaultEvent(HostMetadataCache.Host, new AggregateException(exceptions), "application/json", null, []);

        Assert.Equal(16, fault.Exceptions.Length);
        Assert.Equal("failure-0", fault.Exceptions[0].Message);
        Assert.Equal("failure-15", fault.Exceptions[15].Message);
        Assert.DoesNotContain(fault.Exceptions, exception => exception.Message == "failure-16");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "typed-fault-preserves-empty-aggregate-diagnostic")]
    public void TypedFault_PreservesAnEmptyAggregateAsTheReportedException()
    {
        var aggregate = new AggregateException("empty aggregate");

        var fault = new FaultEvent<DiagnosticFailure>(
            new DiagnosticFailure(FailureSource.ExceptionData),
            null,
            HostMetadataCache.Host,
            aggregate,
            []);

        ExceptionInfo exception = Assert.Single(fault.Exceptions);
        Assert.Equal(TypeCache<AggregateException>.ShortName, exception.ExceptionType);
        Assert.Equal("empty aggregate", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "receive-fault-preserves-empty-aggregate-diagnostic")]
    public void ReceiveFault_PreservesAnEmptyAggregateAsTheReportedException()
    {
        var aggregate = new AggregateException("empty aggregate");

        var fault = new ReceiveFaultEvent(HostMetadataCache.Host, aggregate, null, null, null);

        ExceptionInfo exception = Assert.Single(fault.Exceptions);
        Assert.Equal(TypeCache<AggregateException>.ShortName, exception.ExceptionType);
        Assert.Equal("empty aggregate", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "typed-fault-flattens-nested-aggregate-leaves")]
    public void TypedFault_FlattensNestedAggregatesIntoEveryLeafFailure()
    {
        AggregateException aggregate = NestedAggregate();

        var fault = new FaultEvent<DiagnosticFailure>(
            new DiagnosticFailure(FailureSource.ExceptionData),
            null,
            HostMetadataCache.Host,
            aggregate,
            []);

        AssertLeafFailures(fault.Exceptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "receive-fault-flattens-nested-aggregate-leaves")]
    public void ReceiveFault_FlattensNestedAggregatesIntoEveryLeafFailure()
    {
        AggregateException aggregate = NestedAggregate();

        var fault = new ReceiveFaultEvent(HostMetadataCache.Host, aggregate, null, null, null);

        AssertLeafFailures(fault.Exceptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "inner-exception-depth-boundary")]
    public void Construction_LimitsInnerExceptionChainToSixteenNodes()
    {
        Exception source = new InvalidOperationException("failure-16");
        for (var index = 15; index >= 0; index--)
            source = new InvalidOperationException($"failure-{index}", source);

        var snapshot = new FaultExceptionInfo(source);
        var captured = new List<ExceptionInfo>();
        for (ExceptionInfo? current = snapshot; current is not null; current = current.InnerException)
            captured.Add(current);

        Assert.Equal(16, captured.Count);
        Assert.Equal("failure-0", captured[0].Message);
        Assert.Equal("failure-15", captured[15].Message);
        Assert.Null(captured[15].InnerException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "cardinality-key-and-text-boundaries")]
    public void Construction_BoundsDataCardinalityKeysValuesAndDiagnosticText()
    {
        var source = new InvalidOperationException("source");
        for (var index = 0; index < 31; index++)
            source.Data[$"Key-{index:D2}"] = index;

        string oversizedKey = new('k', 257);
        source.Data[oversizedKey] = new string('v', 2049);
        source.Data["Rejected-33"] = true;

        string oversizedText = new('x', 2049);
        var remote = new StubExceptionInfo(oversizedText, oversizedText, null, oversizedText, oversizedText);

        var dataSnapshot = new FaultExceptionInfo(source);
        var textSnapshot = new FaultExceptionInfo(new ExceptionInfoException(remote));

        Assert.Equal(32, dataSnapshot.Data!.Count);
        for (var index = 0; index < 31; index++)
            Assert.Equal(index, Assert.IsType<int>(dataSnapshot.Data[$"KEY-{index:D2}"]));
        Assert.False(dataSnapshot.Data.ContainsKey("Rejected-33"));
        string boundedKey = Assert.Single(dataSnapshot.Data.Keys, key => key.StartsWith('k'));
        Assert.Equal(256, boundedKey.Length);
        Assert.Equal(new string('v', 2048), Assert.IsType<string>(dataSnapshot.Data[boundedKey]));
        Assert.Equal(2048, textSnapshot.ExceptionType.Length);
        Assert.Equal(2048, textSnapshot.Message.Length);
        Assert.Equal(2048, textSnapshot.StackTrace.Length);
        Assert.Equal(2048, textSnapshot.Source.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "complex-value-safe-rendering")]
    public void Construction_DoesNotInvokeArbitraryDataValueToString()
    {
        var source = new InvalidOperationException("source");
        var value = new HostileDiagnosticValue();
        source.Data["Value"] = value;

        var snapshot = new FaultExceptionInfo(source);

        Assert.Equal(0, value.ToStringCallCount);
        Assert.Equal(typeof(HostileDiagnosticValue).FullName, Assert.IsType<string>(snapshot.Data!["Value"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "wire-safe-scalar-normalization")]
    public void Construction_PreservesWireSafeScalarsAndRendersEnumsAsText()
    {
        object[] scalarValues =
        [
            true,
            byte.MaxValue,
            sbyte.MinValue,
            short.MinValue,
            ushort.MaxValue,
            int.MinValue,
            uint.MaxValue,
            long.MinValue,
            ulong.MaxValue,
            1.25F,
            2.5D,
            3.75M,
            'x',
            Guid.Parse("992821cc-51a7-40fc-8020-0d7cfd6ec891"),
            new DateTime(2044, 5, 6, 7, 8, 9, DateTimeKind.Utc),
            new DateTimeOffset(2045, 6, 7, 8, 9, 10, TimeSpan.Zero),
            TimeSpan.FromMinutes(27),
        ];
        var source = new InvalidOperationException("source");
        for (var index = 0; index < scalarValues.Length; index++)
            source.Data[$"Scalar-{index}"] = scalarValues[index];
        source.Data["Enum"] = FailureSource.ApplicationWrapper;

        var snapshot = new FaultExceptionInfo(source);

        for (var index = 0; index < scalarValues.Length; index++)
        {
            object actual = snapshot.Data![$"Scalar-{index}"];
            Assert.NotNull(actual);
            Assert.Equal(scalarValues[index].GetType(), actual.GetType());
            Assert.Equal(scalarValues[index], actual);
        }
        Assert.Equal(nameof(FailureSource.ApplicationWrapper), Assert.IsType<string>(snapshot.Data!["Enum"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "hostile-getter-isolation")]
    public void HostileDiagnosticGetters_DoNotReplaceTheOriginalFailure()
    {
        var snapshot = new FaultExceptionInfo(new HostileDiagnosticException());

        Assert.Equal(TypeCache<HostileDiagnosticException>.ShortName, snapshot.ExceptionType);
        Assert.Contains("Message property threw", snapshot.Message, StringComparison.Ordinal);
        Assert.Equal(string.Empty, snapshot.StackTrace);
        Assert.Equal(string.Empty, snapshot.Source);
        Assert.Null(snapshot.Data);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "hostile-data-enumerator-isolation")]
    public void HostileDataEnumeration_DoesNotReplaceTheOriginalFailure()
    {
        var source = new HostileDataEnumerationException("original failure");

        var snapshot = new FaultExceptionInfo(source);

        Assert.Equal(TypeCache<HostileDataEnumerationException>.ShortName, snapshot.ExceptionType);
        Assert.Equal("original failure", snapshot.Message);
        Assert.Null(snapshot.Data);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "hostile-remote-type-isolation")]
    public void HostileRemoteExceptionType_DoesNotReplaceTheRemoteFailure()
    {
        var source = new ExceptionInfoException(new HostileExceptionTypeInfo());

        var snapshot = new FaultExceptionInfo(source);

        Assert.Equal(TypeCache<ExceptionInfoException>.ShortName, snapshot.ExceptionType);
        Assert.Equal("remote failure", snapshot.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-DIAGNOSTICS", "missing-remote-type-fallback")]
    public void MissingRemoteExceptionType_FallsBackToTheLocalWrapperType()
    {
        var source = new ExceptionInfoException(new StubExceptionInfo(null!, "remote failure", null));

        var snapshot = new FaultExceptionInfo(source);

        Assert.Equal(TypeCache<ExceptionInfoException>.ShortName, snapshot.ExceptionType);
        Assert.Equal("remote failure", snapshot.Message);
    }

    private static async Task<Fault<DiagnosticFailure>> PublishFaultAsync(FailureSource source)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<DiagnosticFailureConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync().WaitAsync(timeout, cancellationToken);

        try
        {
            Task<IPublishedMessage<Fault<DiagnosticFailure>>> publishedFaultTask = harness.Published
                .SelectAsync<Fault<DiagnosticFailure>>(cancellationToken)
                .FirstObservedAsync();
            await using IPublishMessageObservation<Fault<DiagnosticFailure>> faultObservation =
                await harness.ObservePublishedMessageAsync<Fault<DiagnosticFailure>>(
                    _ => true,
                    cancellationToken);

            await harness.Bus.PublishAsync(new DiagnosticFailure(source), cancellationToken);
            await publishedFaultTask.WaitAsync(timeout, cancellationToken);
            Fault<DiagnosticFailure> fault =
                (await faultObservation.Message.WaitAsync(timeout, cancellationToken)).Message;

            Assert.Single(harness.Published.Snapshot<Fault<DiagnosticFailure>>());
            return fault;
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void AssertWrapper(
        FaultDataException wrapper,
        string message,
        Exception source,
        string sharedValue,
        params string[] expectedKeys)
    {
        Assert.Equal(message, wrapper.Message);
        Assert.Same(source, wrapper.InnerException);
        Assert.Same(wrapper.ApplicationData, wrapper.Data);
        Assert.Equal(sharedValue, wrapper.ApplicationData["SHARED"]);
        Assert.All(expectedKeys, key => Assert.True(wrapper.ApplicationData.ContainsKey(key), key));
    }

    private static AggregateException NestedAggregate() =>
        new(
            "outer aggregate",
            new InvalidOperationException("first failure"),
            new AggregateException(
                "inner aggregate",
                new ArgumentException("second failure"),
                new TimeoutException("third failure")));

    private static void AssertLeafFailures(IReadOnlyList<ExceptionInfo> exceptions)
    {
        Assert.Collection(
            exceptions,
            exception =>
            {
                Assert.Equal(TypeCache<InvalidOperationException>.ShortName, exception.ExceptionType);
                Assert.Equal("first failure", exception.Message);
            },
            exception =>
            {
                Assert.Equal(TypeCache<ArgumentException>.ShortName, exception.ExceptionType);
                Assert.Equal("second failure", exception.Message);
            },
            exception =>
            {
                Assert.Equal(TypeCache<TimeoutException>.ShortName, exception.ExceptionType);
                Assert.Equal("third failure", exception.Message);
            });
    }

    private sealed record DiagnosticFailure(FailureSource Source);

    private enum FailureSource
    {
        ApplicationWrapper,
        ExceptionData,
    }

    private sealed class DiagnosticFailureConsumer : IConsumer<DiagnosticFailure>
    {
        public Task ConsumeAsync(ConsumeContext<DiagnosticFailure> context)
        {
            var failure = new DiagnosticFailureException("intentional diagnostic failure");

            if (context.Message.Source == FailureSource.ApplicationWrapper)
            {
                throw new FaultDataException(failure, new
                {
                    Username = "Frank",
                    CustomerId = 27,
                });
            }

            failure.Data["Username"] = "Frank";
            failure.Data["CustomerId"] = 27;
            throw failure;
        }
    }

    private sealed class DiagnosticFailureException(string message) : Exception(message);

    private sealed class StubExceptionInfo(
        string exceptionType,
        string message,
        ExceptionInfo? innerException,
        string stackTrace = "remote stack",
        string source = "remote source") : ExceptionInfo
    {
        public string ExceptionType { get; } = exceptionType;
        public ExceptionInfo? InnerException { get; } = innerException;
        public string StackTrace { get; } = stackTrace;
        public string Message { get; } = message;
        public string Source { get; } = source;
        public IDictionary<string, object>? Data { get; } = null;
    }

    private sealed class HostileDiagnosticValue
    {
        public int ToStringCallCount { get; private set; }

        public override string ToString()
        {
            ToStringCallCount++;
            throw new InvalidOperationException("Application ToString must not be called for fault diagnostics.");
        }
    }

    private sealed class HostileDiagnosticException : Exception
    {
        public override IDictionary Data => throw new InvalidOperationException("Data getter fault");

        public override string Message => throw new InvalidOperationException("Message getter fault");

        public override string? Source
        {
            get => throw new InvalidOperationException("Source getter fault");
            set => throw new InvalidOperationException("Source setter fault");
        }

        public override string? StackTrace => throw new InvalidOperationException("StackTrace getter fault");
    }

    private sealed class HostileDataEnumerationException(string message) : Exception(message)
    {
        readonly IDictionary _data = new HostileDataDictionary();

        public override IDictionary Data => _data;
    }

    private sealed class HostileDataDictionary : Hashtable
    {
        public override IDictionaryEnumerator GetEnumerator() =>
            throw new InvalidOperationException("Data enumeration fault");
    }

    private sealed class HostileExceptionTypeInfo : ExceptionInfo
    {
        public string ExceptionType => throw new InvalidOperationException("Exception type getter fault");
        public ExceptionInfo? InnerException => null;
        public string StackTrace => "remote stack";
        public string Message => "remote failure";
        public string Source => "remote source";
        public IDictionary<string, object>? Data => null;
    }
}
