using System.Text.Json;
using System.Text.Json.Nodes;

namespace NodeSharp.Nodes.Common.Extension;

public static class JsonExtensions
{
    public static string ToPrettyJson(this string value)
    {
        var node = JsonNode.Parse(value)!;
        
        return JsonSerializer.Serialize(node, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
    
    public static bool TryGetPropertyIgnoreCase(this JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        foreach (var p in element.EnumerateObject())
        {
            if (string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
