using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Operations;

/// <summary>Provides extension methods for health report.</summary>
public static class HealthReportExtensions
{
    /// <summary>Converts this value to json string.</summary>
    /// <param name="result">The result.</param>
    /// <returns>The converted json string.</returns>
    public static string ToJsonString(this HealthReport result)
    {
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
