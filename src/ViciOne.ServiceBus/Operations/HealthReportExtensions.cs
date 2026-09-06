using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Operations;

/// <summary>Serializes health reports using the service-bus metadata JSON contract.</summary>
public static class HealthReportExtensions
{
    /// <summary>Serializes a health report as indented JSON.</summary>
    /// <param name="result">The health report to serialize.</param>
    /// <returns>The serialized health-report document.</returns>
    public static string ToJsonString(this HealthReport result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var healthResult = new JsonObject
        {
            ["status"] = result.Status.ToString(),
            ["results"] = new JsonObject(result.Entries.Select(entry => new KeyValuePair<string, JsonNode?>(entry.Key,
                new JsonObject
                {
                    ["status"] = entry.Value.Status.ToString(),
                    ["description"] = entry.Value.Description,
                    ["data"] = JsonSerializer.SerializeToNode(entry.Value.Data, ServiceBusMetadataJson.Options)
                })))
        };

        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options)
        {
            WriteIndented = true,
        };

        return healthResult.ToJsonString(options);
    }
}
