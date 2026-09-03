namespace ViciOne.ServiceBus.Testing
{
    using System;
    using System.Diagnostics;


    public class TestHarnessOptions
    {
        public TimeSpan TestTimeout { get; set; } = Debugger.IsAttached ? TimeSpan.FromMinutes(50) : TimeSpan.FromSeconds(30);
        public TimeSpan TestInactivityTimeout { get; set; } = Debugger.IsAttached ? TimeSpan.FromMinutes(30) : TimeSpan.FromSeconds(1.2);
        public TestContextSaveMode ContextSaveMode { get; set; } = TestContextSaveMode.All;
        public int MaximumSavedContexts { get; set; } = 4096;
    }
}
