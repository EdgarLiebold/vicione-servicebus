using System;
using System.Diagnostics;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Configures assertion timeouts, inactivity detection, and observation retention for a test harness.</summary>
public sealed class TestHarnessOptions
{
    /// <summary>Gets or sets the maximum time an assertion waits for a matching observation.</summary>
    public TimeSpan TestTimeout { get; set; } = Debugger.IsAttached ? TimeSpan.FromMinutes(50) : TimeSpan.FromSeconds(30);
    /// <summary>Gets or sets the interval without transport activity that marks the harness inactive.</summary>
    public TimeSpan TestInactivityTimeout { get; set; } = Debugger.IsAttached ? TimeSpan.FromMinutes(30) : TimeSpan.FromSeconds(1.2);
    /// <summary>Gets or sets the policy for retaining observed message contexts.</summary>
    public TestContextSaveMode ContextSaveMode { get; set; } = TestContextSaveMode.All;
    /// <summary>Gets or sets the maximum number of contexts retained per observation list in bounded mode.</summary>
    public int MaximumSavedContexts { get; set; } = 4096;
}
