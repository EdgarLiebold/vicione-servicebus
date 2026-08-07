// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Courier
{
    using System.Collections.Generic;


    public interface ObjectGraphActivityArguments
    {
        OuterObject Outer { get; }
        string[] Names { get; }
        IDictionary<string, string> ArgumentsDictionary { get; }
    }
}
