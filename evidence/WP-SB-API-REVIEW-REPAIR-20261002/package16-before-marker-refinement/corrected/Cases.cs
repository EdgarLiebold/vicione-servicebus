using System.Runtime.ExceptionServices;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Logging;
using Xunit;

namespace LoggingLifetime;

public static class Cases
{
    public static readonly string[] Names = [
        "ordinarydisposedpredecessor", "ordinaryalivepredecessorcontrol", "publicresetcontrol", "explicitlivefactorycontrol",
        "automaticcategoryprovenance", "automaticmessagesprovenance", "automaticnestedcategoryprovenance", "sameproviderrootidentity",
        "sameprovidercategoryidentity", "borrowedfactorysharedacrossproviders", "explicitiloggeridentity", "explicitcustomcontextidentity",
        "explicitcategoryidentity", "explicitnullwithoutfactoryidentity", "retireautomaticwhennofactory", "automaticdefaultthenrealfactory",
        "publicboundarycontrols", "hostrestartfreshroot_default", "hostrestartfreshroot_typed_dynamic", "hostrestartfreshroot_typed_explicit",
        "hostrestartfreshroot_default_plus_typed", "concurrentinheritedautomaticcontexts", "concurrentexplicitoverrides", "siblingpreservesinheritedautomaticcategory",
        "disposeawhilebremainsoperational", "factoryresolutionfaultcontrol", "precanceledlifecyclecontrol", "faultymeterfactoryloggingcontrol",
        "actualproviderownedfactorydisposal", "actualcallbackfactoryownership", "livebloggerrouteafteralivea", "realmetricbindingafterautomaticrefresh"
    ];

    public static async Task Run(string mode, TimeSpan timeout)
    {
        int number = Array.IndexOf(Names, mode) + 1;
        if (number == 0) throw new ArgumentException("Unknown exact case", nameof(mode));
        ILogContext? entry = LogContext.Current;
        LogContext.Current = null;
        var life = new OwnedLifetime(timeout);
        Exception? primary = null;
        try
        {
            if (number is 1 or 2 or 3 or 5 or 6 or 7 or 31) await Sequential(life, number);
            else if (number is 4 or 10 or 11 or 12 or 13) await Explicit(life, number);
            else if (number is 8 or 9 or 14 or 15 or 16 or 17) await Identity(life, number);
            else if (number is >= 18 and <= 21) await HostRestart(life, number);
            else if (number is 22 or 23 or 24) await Concurrent(life, number);
            else await Runtime(life, number);
        }
        catch (Exception e) { primary = e; }
        try { await life.DisposeAsync(); }
        catch (Exception cleanup)
        {
            if (primary != null) throw new AggregateException("CASE AND OWNED CLEANUP FAILED", primary, cleanup);
            throw;
        }
        finally { LogContext.Current = entry; }
        if (primary != null) ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static Exception? Capture(Action action) { try { action(); return null; } catch (Exception e) { return e; } }
    static async Task<Exception?> CaptureAsync(Func<Task> action) { try { await action(); return null; } catch (Exception e) { return e; } }

    static void Live(ILoggerFactory factory) => Assert.NotNull(factory.CreateLogger("Public.Validity"));
    static void Disposed(ILoggerFactory factory)
    {
        var exception = Assert.Throws<ObjectDisposedException>(() => factory.CreateLogger("Public.DisposedWitness"));
        Assert.Equal("LoggerFactory", exception.ObjectName);
    }
    static void ResolveClean(RuntimeOwner owner)
    {
        Exception? error = Capture(owner.Resolve);
        if (error != null) Console.WriteLine("ACTUAL_CONSTRUCTION_FAILURE " + error);
        Assert.Null(error);
    }

    static async Task Sequential(OwnedLifetime life, int number)
    {
        var a = new RuntimeOwner(life); a.Resolve();
        var factoryA = a.Factory;
        var contextA = Assert.IsAssignableFrom<ILogContext>(LogContext.Current);
        if (number == 5) LogContext.Current = LogContext.CreateLogContext("Public.AutomaticChild");
        if (number == 6) LogContext.Current = contextA.Messages;
        if (number == 7) LogContext.Current = LogContext.CreateLogContext("Public.Outer").CreateLogContext("Public.Inner");
        if (number is not (2 or 31)) { await life.Await(a.DisposeAsync()); Disposed(factoryA); }
        else Live(factoryA);
        if (number == 3) LogContext.Current = null;
        var b = new RuntimeOwner(life); Live(b.Factory); Assert.NotSame(factoryA, b.Factory);
        ResolveClean(b);
        Assert.NotNull(LogContext.CreateLogContext("Public.SecondRoot"));
        if (number == 31) { Markers.Emit("B"); Markers.Require(b.Recorder, "B"); Live(factoryA); }
        if (number == 2) Live(factoryA);
    }

    static async Task Explicit(OwnedLifetime life, int number)
    {
        var recorder = new RecordingProvider();
        ILoggerFactory caller = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace).AddProvider(recorder));
        life.Own(() => { caller.Dispose(); return Task.CompletedTask; });
        var logger = caller.CreateLogger("Public.Caller");
        LogContext.ConfigureCurrentLogContext(caller);
        if (number == 11) LogContext.ConfigureCurrentLogContext(logger);
        if (number == 12) LogContext.Current = new DelegatingContext(LogContext.Current!);
        if (number == 13) LogContext.Current = LogContext.CreateLogContext("Public.ExplicitChild");
        var callerContext = LogContext.Current;
        if (number == 10) LogContext.Current = null;
        var a = new RuntimeOwner(life, borrowed: number == 10 ? caller : null); a.Resolve();
        var established = Assert.IsAssignableFrom<ILogContext>(LogContext.Current);
        if (number != 10) Assert.Same(callerContext, established);
        var b = new RuntimeOwner(life, borrowed: number == 10 ? caller : null); b.Resolve();
        Assert.Same(established, LogContext.Current);
        if (number == 11)
        {
            Assert.Same(logger, LogContext.CreateLogContext("Public.Any").Logger);
            Assert.Same(logger, established.Messages.Logger);
        }
        Markers.Emit("Caller"); Markers.Require(recorder, "Caller", number == 11 ? "Public.Caller" : "Public.Category");
        await life.Await(a.DisposeAsync()); await life.Await(b.DisposeAsync());
        Live(caller); Assert.Equal(0, recorder.DisposeCount);
        Assert.Same(established, LogContext.Current);
        Markers.Emit("AfterDispose"); Markers.Require(recorder, "AfterDispose", number == 11 ? "Public.Caller" : "Public.Category");
    }

    static async Task Identity(OwnedLifetime life, int number)
    {
        var empty = new ServiceCollection().BuildServiceProvider();
        life.Own(() => empty.DisposeAsync().AsTask());
        if (number == 14)
        {
            LogContext.ConfigureCurrentLogContext((ILoggerFactory?)null);
            var expected = LogContext.Current; LogContext.ConfigureCurrentLogContextIfNull(empty);
            Assert.Same(expected, LogContext.Current); Assert.IsType<NullLogger>(LogContext.Current!.Logger);
            Assert.NotNull(LogContext.CreateLogContext("Public.NullChild")); return;
        }
        if (number == 17)
        {
            Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() => LogContext.ConfigureCurrentLogContextIfNull(null!)).ParamName);
            Assert.Equal("categoryName", Assert.Throws<ArgumentNullException>(() => LogContext.CreateLogContext(null!)).ParamName);
            Assert.Equal("categoryName", Assert.Throws<ArgumentException>(() => LogContext.CreateLogContext(" ")).ParamName);
            LogContext.SetCurrentIfNull(null); Assert.Null(LogContext.Current);
            LogContext.ConfigureCurrentLogContext(); var expected = LogContext.Current;
            LogContext.Current = null; LogContext.SetCurrentIfNull(expected); Assert.Same(expected, LogContext.Current);
            var other = new DelegatingContext(expected!); LogContext.SetCurrentIfNull(other); Assert.Same(expected, LogContext.Current); return;
        }
        if (number == 16) { LogContext.ConfigureCurrentLogContextIfNull(empty); Assert.IsType<NullLogger>(LogContext.Current!.Logger); }
        var a = new RuntimeOwner(life); LogContext.ConfigureCurrentLogContextIfNull(a.Services);
        var factory = a.Factory;
        if (number == 15)
        {
            await life.Await(a.DisposeAsync()); Disposed(factory);
            LogContext.ConfigureCurrentLogContextIfNull(empty);
            Exception? error = Capture(() => LogContext.CreateLogContext("Public.NoFactory"));
            if (error != null) Console.WriteLine("ACTUAL_STALE_FACTORY " + error);
            Assert.Null(error); Assert.IsType<NullLogger>(LogContext.Current!.Logger); return;
        }
        if (number == 9) LogContext.Current = LogContext.CreateLogContext("Public.PreservedCategory");
        var current = LogContext.Current; var messages = current!.Messages;
        LogContext.ConfigureCurrentLogContextIfNull(a.Services);
        Assert.Same(current, LogContext.Current); Assert.Same(messages, LogContext.Current!.Messages);
        LogContext.Current.Logger.LogInformation("SameFactory");
        Assert.Single(a.Recorder.Entries.Where(e => e.Text == "SameFactory"));
        if (number == 9) a.Recorder.Require("Public.PreservedCategory", "SameFactory");
        Live(factory);
    }

    static async Task HostRestart(OwnedLifetime life, int number)
    {
        string kind = number switch { 19 => "dynamic", 20 => "explicit", 21 => "both", _ => "default" };
        var a = new RuntimeOwner(life, kind, host: true); a.Resolve();
        var factoryA = a.Factory;
        await life.Await(a.Start()); await a.Deliver(life, "A"); Live(factoryA);
        await life.Await(a.DisposeAsync()); Disposed(factoryA); Assert.Equal(1, a.Recorder.DisposeCount);
        var b = new RuntimeOwner(life, kind, host: true); ResolveClean(b);
        Assert.NotSame(factoryA, b.Factory); Live(b.Factory);
        await life.Await(b.Start()); await b.Deliver(life, "B"); Live(b.Factory);
        Markers.Emit("RestartB"); Markers.Require(b.Recorder, "RestartB");
    }

    static TaskCompletionSource<bool> Gate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    static async Task Concurrent(OwnedLifetime life, int number)
    {
        var parent = new RuntimeOwner(life); parent.Resolve();
        if (number == 24) LogContext.Current = LogContext.CreateLogContext("Public.ParentCategory");
        if (number == 23)
        {
            ILoggerFactory caller = LoggerFactory.Create(_ => { });
            life.Own(() => { caller.Dispose(); return Task.CompletedTask; });
            LogContext.ConfigureCurrentLogContext(caller);
        }
        var inherited = LogContext.Current!;
        var enteredB = Gate(); var enteredC = Gate(); var release = Gate();
        life.Release(() => release.TrySetResult(true));
        var b = life.Track(Task.Run(() => Child("ChildB", enteredB, false)));
        var c = life.Track(Task.Run(() => Child("ChildC", enteredC, number == 24)));
        await life.Await(enteredB.Task); await life.Await(enteredC.Task);
        Assert.Same(inherited, LogContext.Current);
        release.TrySetResult(true);
        await life.Await(b); await life.Await(c);
        Assert.Same(inherited, LogContext.Current);
        async Task Child(string marker, TaskCompletionSource<bool> entered, bool sibling)
        {
            var own = new OwnedLifetime(life.Timeout);
            Exception? failure = null;
            try
            {
                Assert.Same(inherited, LogContext.Current);
                if (sibling)
                {
                    entered.TrySetResult(true); await release.Task;
                    Assert.Same(inherited, LogContext.Current);
                    LogContext.Current!.Logger.LogInformation(marker);
                    parent.Recorder.Require("Public.ParentCategory", marker);
                }
                else
                {
                    RecordingProvider? callerRecorder = null;
                    if (number == 23)
                    {
                        callerRecorder = new RecordingProvider();
                        ILoggerFactory caller = LoggerFactory.Create(x => x.SetMinimumLevel(LogLevel.Trace).AddProvider(callerRecorder));
                        own.Own(() => { caller.Dispose(); return Task.CompletedTask; });
                        LogContext.ConfigureCurrentLogContext(caller);
                    }
                    var expectedExplicit = LogContext.Current;
                    var root = new RuntimeOwner(own); root.Resolve();
                    var current = LogContext.Current;
                    entered.TrySetResult(true); await release.Task;
                    Assert.Same(current, LogContext.Current);
                    if (number == 23) Assert.Same(expectedExplicit, current); else Assert.NotSame(inherited, current);
                    Markers.Emit(marker); Markers.Require(callerRecorder ?? root.Recorder, marker);
                    await own.Await(root.DisposeAsync());
                    if (number == 23) { Markers.Emit(marker + ".Borrowed"); Markers.Require(callerRecorder!, marker + ".Borrowed"); }
                }
            }
            catch (Exception e) { failure = e; entered.TrySetException(e); }
            try { await own.DisposeAsync(); }
            catch (Exception cleanup) { if (failure != null) throw new AggregateException(failure, cleanup); throw; }
            if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    static async Task Runtime(OwnedLifetime life, int number)
    {
        if (number == 26)
        {
            var a = new RuntimeOwner(life); var expected = new InvalidOperationException("FactoryResolutionSentinel");
            Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => LogContext.ConfigureCurrentLogContextIfNull(new FaultProvider(a.Services, expected))));
            Assert.Null(LogContext.Current); a.Resolve(); Live(a.Factory); return;
        }
        if (number == 25)
        {
            var a = new RuntimeOwner(life); a.Resolve(); var factoryA = a.Factory; await life.Await(a.Start());
            var b = new RuntimeOwner(life); b.Resolve(); await life.Await(b.Start());
            await a.Deliver(life, "ACompleted"); await life.Await(a.DisposeAsync()); Disposed(factoryA);
            await b.Deliver(life, "BAfterADisposed"); Live(b.Factory); Markers.Emit("BStillLive"); Markers.Require(b.Recorder, "BStillLive"); return;
        }
        if (number == 27)
        {
            var recorder = new RecordingProvider(); ILoggerFactory caller = LoggerFactory.Create(x => x.AddProvider(recorder));
            life.Own(() => { caller.Dispose(); return Task.CompletedTask; }); LogContext.ConfigureCurrentLogContext(caller); var expected = LogContext.Current;
            var a = new RuntimeOwner(life); a.Resolve(); using var canceled = new CancellationTokenSource(); canceled.Cancel();
            var error = await CaptureAsync(() => life.Await(a.Start(canceled.Token)));
            Assert.Equal(canceled.Token, Assert.IsAssignableFrom<OperationCanceledException>(error).CancellationToken);
            Assert.Same(expected, LogContext.Current); Live(caller);
            await life.Await(a.Start()); await a.Deliver(life, "AfterPreCanceled"); return;
        }
        if (number == 28)
        {
            var source = new ServiceCollection(); source.AddMetrics(); var provider = source.BuildServiceProvider();
            life.Own(() => provider.DisposeAsync().AsTask());
            var meter = new FaultMeterFactory(provider.GetRequiredService<IMeterFactory>(), new InvalidOperationException("MeterSentinel"));
            var a = new RuntimeOwner(life, faultMeter: meter); a.Resolve(); Assert.True(meter.Attempts > 0);
            Live(a.Factory); Markers.Emit("MeterFaultLogger"); Markers.Require(a.Recorder, "MeterFaultLogger");
            await life.Await(a.Start()); await a.Deliver(life, "MeterFaultDelivery"); return;
        }
        if (number == 32)
        {
            var probe = new MeterProbe(); life.Own(() => { probe.Dispose(); return Task.CompletedTask; });
            var a = new RuntimeOwner(life); a.Resolve();
            var instrumentsA = probe.Published.ToArray(); Assert.NotEmpty(instrumentsA);
            var b = new RuntimeOwner(life); b.Resolve();
            var instrumentsB = probe.Published.Where(i => !instrumentsA.Contains(i)).ToArray(); Assert.NotEmpty(instrumentsB);
            Assert.All(instrumentsB, i => Assert.DoesNotContain(instrumentsA, old => ReferenceEquals(old.Meter, i.Meter)));
            Meter meterB = instrumentsB[0].Meter;
            Meter ownedB = b.Services.GetRequiredService<IMeterFactory>().Create(new MeterOptions(meterB.Name) { Version = meterB.Version });
            Assert.Same(ownedB, meterB);
            await life.Await(b.Start()); await b.Deliver(life, "ActualMeterB");
            Assert.Contains(probe.Measurements, i => instrumentsB.Contains(i) && i.Name.Contains("sent", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(probe.Measurements, i => instrumentsB.Contains(i) && i.Name.Contains("process", StringComparison.OrdinalIgnoreCase)); return;
        }
        var owner = new RuntimeOwner(life, callback: number == 30);
        if (number == 30)
        {
            LogContext.ConfigureCurrentLogContextIfNull(owner.Services);
            Assert.Equal(0, owner.CallbackFactory!.DisposeCount); Live(owner.Factory);
        }
        owner.Resolve(); var factory = owner.Factory;
        await life.Await(owner.Start()); await owner.Deliver(life, "Ownership"); Live(factory);
        if (number == 30) Assert.Equal(0, owner.CallbackFactory!.DisposeCount);
        await life.Await(owner.DisposeAsync()); Disposed(factory);
        if (number == 29) Assert.Equal(1, owner.Recorder.DisposeCount);
        else
        {
            Assert.Equal(1, owner.CallbackFactory!.DisposeCount);
            LogContext.Current = null;
            var fresh = new RuntimeOwner(life); fresh.Resolve(); Live(fresh.Factory); Assert.NotSame(factory, fresh.Factory);
        }
    }
}
