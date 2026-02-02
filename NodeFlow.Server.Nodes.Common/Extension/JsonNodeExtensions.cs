using System.Text.Json.Nodes;

namespace NodeSharp.Nodes.Common.Extension;

public static class JsonNodeExtensions
{
    public static JsonNode? GetByPath(this JsonNode? node, string path)
    {
        if (node is null || string.IsNullOrWhiteSpace(path))
            return null;

        var current = node;

        foreach (var segment in path.Split('.'))
        {
            if (current is JsonObject obj)
            {
                current = obj[segment];
            }
            else if (current is JsonArray arr &&
                     int.TryParse(segment, out var index) &&
                     index >= 0 && index < arr.Count)
            {
                current = arr[index];
            }
            else
            {
                return null;
            }

            if (current is null)
                return null;
        }

        return current;
    }
}