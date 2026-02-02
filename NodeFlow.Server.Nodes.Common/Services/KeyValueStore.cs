using System.Text.Json;
using Microsoft.Extensions.Options;
using NodeSharp.Nodes.Common.Configuration;

namespace NodeSharp.Nodes.Common.Services;

public class KeyValueStore : IDisposable
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, string> entries = new(StringComparer.Ordinal);
    private readonly string? filePath;
    private readonly Timer? flushTimer;
    private readonly TimeSpan flushInterval1;

    public KeyValueStore(IOptions<PersistToDiskSettings> persistSettings)
    {
        var settings = persistSettings?.Value;
        flushInterval1 = settings?.FlushInterval ?? TimeSpan.Zero;
        filePath = ResolveFilePath(settings?.FileName);

        LoadFromDisk();

        if (flushInterval1 > TimeSpan.Zero && !string.IsNullOrWhiteSpace(filePath))
        {
            flushTimer = new Timer(_ => FlushToDisk(), null, flushInterval1, flushInterval1);
        }
    }

    public void Set(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        var json = JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object));

        lock (gate)
        {
            entries[key] = json;
        }
    }

    public T? Get<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        string? raw;
        lock (gate)
        {
            if (!entries.TryGetValue(key, out raw))
            {
                return default;
            }
        }

        try
        {
            return JsonSerializer.Deserialize<T>(raw);
        }
        catch
        {
            return default;
        }
    }

    public void Dispose()
    {
        flushTimer?.Dispose();
        FlushToDisk();
    }

    private void LoadFromDisk()
    {
        if (flushInterval1 == TimeSpan.Zero || (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data is null)
            {
                return;
            }

            lock (gate)
            {
                foreach (var pair in data)
                {
                    entries[pair.Key] = pair.Value;
                }
            }
        }
        catch
        {
            return;
        }
    }

    private void FlushToDisk()
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        Dictionary<string, string> snapshot;
        lock (gate)
        {
            snapshot = new Dictionary<string, string>(entries, StringComparer.Ordinal);
        }

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(snapshot);
            File.WriteAllText(filePath, json);
        }
        catch
        {
        }
    }

    private static string? ResolveFilePath(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        if (Path.IsPathRooted(fileName))
        {
            return fileName;
        }

        return Path.Combine(FileSystem.AppDataDirectory, fileName);
    }
}
