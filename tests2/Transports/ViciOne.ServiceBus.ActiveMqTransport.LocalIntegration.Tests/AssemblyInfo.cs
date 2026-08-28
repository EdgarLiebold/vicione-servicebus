using Xunit.Sdk;
using Xunit.v3;

// The assembly shares one run-scoped Classic/Artemis broker pair, and some tests deliberately
// restart or inspect those brokers. Keep the provider module serial while other provider modules
// remain free to run in parallel.
[assembly: Parallelization(Mode = ParallelMode.None)]

namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;
