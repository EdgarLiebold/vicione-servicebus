using System;
using System.Diagnostics;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines configuration options for test harness.</summary>
public sealed class TestHarnessOptions
{
    /// <summary>Gets or sets the test timeout.</summary>
    public TimeSpan TestTimeout { get; set; } = Debugger.IsAttached ? TimeSpan.FromMinutes(50) : TimeSpan.FromSeconds(30);
    /// <summary>Gets or sets the test inactivity timeout.</summary>
    public TimeSpan TestInactivityTimeout { get; set; } = Debugger.IsAttached ? TimeSpan.FromMinutes(30) : TimeSpan.FromSeconds(1.2);
    /// <summary>Gets or sets the context save mode.</summary>
    public TestContextSaveMode ContextSaveMode { get; set; } = TestContextSaveMode.All;
    /// <summary>Gets or sets the maximum saved contexts.</summary>
    public int MaximumSavedContexts { get; set; } = 4096;
}
