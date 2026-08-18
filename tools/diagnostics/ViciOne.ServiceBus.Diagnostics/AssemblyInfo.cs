using System.Runtime.CompilerServices;

// The ledger and the command line are internal because nothing outside this tool consumes them, and
// their own tests are the one exception. Visibility is not a substitute for testability.
[assembly: InternalsVisibleTo("ViciOne.ServiceBus.Diagnostics.Tests")]
