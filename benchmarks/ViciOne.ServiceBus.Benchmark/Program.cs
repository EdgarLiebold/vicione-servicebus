using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NDesk.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Monitoring;
using ViciOneServiceBusBenchmark.BusOutbox;
using ViciOneServiceBusBenchmark.Latency;
using ViciOneServiceBusBenchmark.RequestResponse;

namespace ViciOneServiceBusBenchmark;

class Program
{
    static List<string> _remaining;

    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("ViciOne.ServiceBus Benchmark");
        Console.WriteLine();

        var optionSet = new ProgramOptionSet();

        var disposables = new List<IDisposable>();
        var executedBenchmarks = 0;
        try
        {
            _remaining = optionSet.Parse(args);

            if (optionSet.Help)
            {
                ShowHelp(optionSet);
                return 0;
            }

            if (optionSet.Verbose)
            {
            }

            if (optionSet.EnableMetrics)
            {
                disposables.Add(Sdk.CreateMeterProviderBuilder()
                    .AddMeter(ServiceBusTelemetry.MeterName)
                    .ConfigureResource(r => r.AddService("ViciOne.ServiceBus.Benchmark"))
                    .AddOtlpExporter()
                    .Build());
            }

            if (optionSet.EnableTraces)
            {
                disposables.Add(Sdk.CreateTracerProviderBuilder()
                    .AddSource(DiagnosticHeaders.DefaultListenerName)
                    .ConfigureResource(r => r.AddService("ViciOne.ServiceBus.Benchmark"))
                    .AddOtlpExporter()
                    .Build());
            }

            optionSet.ShowOptions();

            if (optionSet.Threads.HasValue)
            {
                ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
                ThreadPool.SetMinThreads(Math.Max(workerThreads, optionSet.Threads.Value), completionPortThreads);
            }

            if (optionSet.Benchmark.HasFlag(ProgramOptionSet.BenchmarkOptions.Latency))
            {
                await RunLatencyBenchmark(optionSet);
                executedBenchmarks++;
            }

            if (optionSet.Benchmark.HasFlag(ProgramOptionSet.BenchmarkOptions.Rpc))
            {
                RunRequestResponseBenchmark(optionSet);
                executedBenchmarks++;
            }

            if (optionSet.Benchmark.HasFlag(ProgramOptionSet.BenchmarkOptions.BusOutbox))
            {
                await RunBusOutboxBenchmark(optionSet);
                executedBenchmarks++;
            }

            if (executedBenchmarks == 0)
                throw new OptionException("No benchmark was selected.", "run");

            if (Debugger.IsAttached)
            {
                Console.Write("Press any key to continue...");
                Console.ReadKey();
            }

            return 0;
        }
        catch (OptionException ex)
        {
            Console.Write("vicione-servicebus-benchmark: ");
            Console.WriteLine(ex.Message);
            Console.WriteLine("Use 'vicione-servicebus-benchmark --help' for detailed usage information.");
            return 2;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Crashed: {0}", ex.Message);
            return 1;
        }
        finally
        {
            disposables.ForEach(x => x.Dispose());
        }
    }

    static async Task RunLatencyBenchmark(ProgramOptionSet optionSet)
    {
        var messageLatencyOptionSet = new MessageLatencyOptionSet();

        messageLatencyOptionSet.Parse(_remaining);

        IMessageLatencySettings settings = messageLatencyOptionSet;

        IMessageLatencyTransport transport;
        if (optionSet.Transport == ProgramOptionSet.TransportOptions.AzureServiceBus)
        {
            var serviceBusOptionSet = new ServiceBusOptionSet();

            serviceBusOptionSet.Parse(_remaining);

            serviceBusOptionSet.ShowOptions();

            transport = new ServiceBusMessageLatencyTransport(serviceBusOptionSet, settings);
        }
        else if (optionSet.Transport == ProgramOptionSet.TransportOptions.RabbitMq)
        {
            var rabbitMqOptionSet = new RabbitMqOptionSet();
            rabbitMqOptionSet.Parse(_remaining);

            rabbitMqOptionSet.ShowOptions();

            transport = new RabbitMqMessageLatencyTransport(rabbitMqOptionSet, settings);
        }
        else if (optionSet.Transport == ProgramOptionSet.TransportOptions.AmazonSqs)
        {
            var amazonSqsOptionSet = new AmazonSqsOptionSet();
            amazonSqsOptionSet.Parse(_remaining);

            amazonSqsOptionSet.ShowOptions();

            transport = new AmazonSqsMessageLatencyTransport(amazonSqsOptionSet.HostSettings, settings);
        }
        else if (optionSet.Transport == ProgramOptionSet.TransportOptions.ActiveMq)
        {
            var activeMqOptionSet = new ActiveMqOptionSet();
            activeMqOptionSet.Parse(_remaining);

            activeMqOptionSet.ShowOptions();

            transport = new ActiveMqMessageLatencyTransport(activeMqOptionSet, settings);
        }
        else if (optionSet.Transport == ProgramOptionSet.TransportOptions.Sql)
        {
            var options = new SqlOptionSet();
            options.Parse(_remaining);

            options.ShowOptions();

            transport = new SqlMessageLatencyTransport(options, settings);
        }
        else if (optionSet.Transport == ProgramOptionSet.TransportOptions.Mediator)
            transport = new MediatorMessageLatencyTransport(settings);
        else
        {
            var inMemoryOptionSet = new InMemoryOptionSet();
            inMemoryOptionSet.Parse(_remaining);

            inMemoryOptionSet.ShowOptions();

            transport = new InMemoryMessageLatencyTransport(inMemoryOptionSet, settings);
        }

        var benchmark = new MessageLatencyBenchmark(transport, settings);

        await benchmark.Run();
    }

    static void RunRequestResponseBenchmark(ProgramOptionSet optionSet)
    {
        var requestResponseOptionSet = new RequestResponseOptionSet();

        requestResponseOptionSet.Parse(_remaining);

        IRequestResponseSettings settings = requestResponseOptionSet;

        IRequestResponseTransport transport;
        if (optionSet.Transport == ProgramOptionSet.TransportOptions.AzureServiceBus)
        {
            var serviceBusOptionSet = new ServiceBusOptionSet();

            serviceBusOptionSet.Parse(_remaining);

            serviceBusOptionSet.ShowOptions();

            transport = new ServiceBusRequestResponseTransport(serviceBusOptionSet, settings);
        }
        else if (optionSet.Transport == ProgramOptionSet.TransportOptions.RabbitMq)
        {
            var rabbitMqOptionSet = new RabbitMqOptionSet();
            rabbitMqOptionSet.Parse(_remaining);

            rabbitMqOptionSet.ShowOptions();

            transport = new RabbitMqRequestResponseTransport(rabbitMqOptionSet, settings);
        }
        else if (optionSet.Transport == ProgramOptionSet.TransportOptions.Mediator)
            transport = new MediatorRequestResponseTransport(settings);
        else
        {
            var inMemoryOptionSet = new InMemoryOptionSet();
            inMemoryOptionSet.Parse(_remaining);

            inMemoryOptionSet.ShowOptions();

            transport = new InMemoryRequestResponseTransport(inMemoryOptionSet, settings);
        }

        var benchmark = new RequestResponseBenchmark(transport, settings);

        benchmark.Run();
    }

    static async Task RunBusOutboxBenchmark(ProgramOptionSet optionSet)
    {
        var busOutboxBenchmarkOptions = new BusOutboxBenchmarkOptions();

        busOutboxBenchmarkOptions.Parse(_remaining);

        IConfigureBusOutboxTransport transport;
        if (optionSet.Transport == ProgramOptionSet.TransportOptions.RabbitMq)
        {
            var rabbitMqOptionSet = new RabbitMqOptionSet();
            rabbitMqOptionSet.Parse(_remaining);

            rabbitMqOptionSet.ShowOptions();

            transport = new RabbitMqConfigureBusOutboxTransport(rabbitMqOptionSet, busOutboxBenchmarkOptions);
        }
        else if (optionSet.Transport == ProgramOptionSet.TransportOptions.AzureServiceBus)
        {
            var serviceBusOptionSet = new ServiceBusOptionSet();

            serviceBusOptionSet.Parse(_remaining);

            serviceBusOptionSet.ShowOptions();

            transport = new ServiceBusConfigureBusOutboxTransport(serviceBusOptionSet, busOutboxBenchmarkOptions);
        }
        else
        {
            var inMemoryOptionSet = new InMemoryOptionSet();
            inMemoryOptionSet.Parse(_remaining);

            inMemoryOptionSet.ShowOptions();

            transport = new InMemoryConfigureBusOutboxTransport(inMemoryOptionSet, busOutboxBenchmarkOptions);
        }

        var benchmark = new BusOutboxBenchmark(transport, busOutboxBenchmarkOptions);

        await benchmark.Run();
    }

    static void ShowHelp(OptionSet p)
    {
        Console.WriteLine("Usage: vicione-servicebus-benchmark [OPTIONS]+");
        Console.WriteLine("Executes the benchmark using the specified transport with the specified options.");
        Console.WriteLine("If no benchmark is specified, the latency and RPC benchmarks are executed.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        p.WriteOptionDescriptions(Console.Out);

        Console.WriteLine();
        Console.WriteLine("RabbitMQ Options:");
        new RabbitMqOptionSet().WriteOptionDescriptions(Console.Out);

        Console.WriteLine();
        Console.WriteLine("Azure Service Bus Options:");
        new ServiceBusOptionSet().WriteOptionDescriptions(Console.Out);

        Console.WriteLine();
        Console.WriteLine("Amazon SQS Options:");
        new AmazonSqsOptionSet().WriteOptionDescriptions(Console.Out);

        Console.WriteLine();
        Console.WriteLine("ActiveMQ Options:");
        new ActiveMqOptionSet().WriteOptionDescriptions(Console.Out);

        Console.WriteLine();
        Console.WriteLine("PostgreSQL transport Options:");
        new SqlOptionSet().WriteOptionDescriptions(Console.Out);

        Console.WriteLine();
        Console.WriteLine("In-memory Options:");
        new InMemoryOptionSet().WriteOptionDescriptions(Console.Out);

        Console.WriteLine();
        Console.WriteLine("Latency benchmark Options:");
        new MessageLatencyOptionSet().WriteOptionDescriptions(Console.Out);

        Console.WriteLine();
        Console.WriteLine("RPC benchmark Options:");
        new RequestResponseOptionSet().WriteOptionDescriptions(Console.Out);

        Console.WriteLine();
        Console.WriteLine("Bus-outbox benchmark Options:");
        new BusOutboxBenchmarkOptions().WriteOptionDescriptions(Console.Out);
    }
}
