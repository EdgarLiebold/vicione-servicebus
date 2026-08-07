// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.DbTransport.Tests;

using System;
using NUnit.Framework;


static class LicenseConfiguration
{
    const string LicensePathKey = "VICIONE_SERVICEBUS_LICENSE_PATH";

    public static string? LicensePath =>
        TestContext.Parameters.Exists(LicensePathKey)
            ? TestContext.Parameters.Get(LicensePathKey)
            : Environment.GetEnvironmentVariable(LicensePathKey);
}