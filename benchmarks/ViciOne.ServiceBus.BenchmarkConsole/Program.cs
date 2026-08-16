// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
using System.Reflection;
using BenchmarkDotNet.Running;

var currentAssembly = Assembly.GetExecutingAssembly();
BenchmarkSwitcher
    .FromAssembly(currentAssembly)
    .Run(args);
