using System.Text.Json;

namespace NodeSharp.Nodes.Common.Configuration;

public sealed class NodeSharpSettings
{
    public GridSettings Grid { get; init; } = new();
    public DirectoriesSettings Directories { get; init; } = new();
    public ExplanationsPopupSettings ExplanationsPopup { get; init; } = new();
    public KeyValueStoreSettings KeyValueStore { get; init; } = new();
    public PersistToDiskSettings PersistToDisk { get; init; } = new();

    public static NodeSharpSettings Load(string fileName)
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(fileName).GetAwaiter().GetResult();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var settings = JsonSerializer.Deserialize<NodeSharpSettings>(json);
            return settings ?? new NodeSharpSettings();
        }
        catch
        {
            return new NodeSharpSettings();
        }
    }
}

public sealed class GridSettings
{
    public double Size { get; init; } = 10.0;
    public string MinorLineColor { get; init; } = "#E3E8EF";
    public string MajorLineColor { get; init; } = "#CBD5E1";
    public string BackgroundColor { get; init; } = "#F8FAFC";
    public int MajorLineEvery { get; init; } = 5;
}

public sealed class DirectoriesSettings
{
}

public sealed class ExplanationsPopupSettings
{
    public bool UseEditableFields { get; init; } = false;
    public string ExplanationFilesDirectory { get; init; } = string.Empty;

    public string GetExpandedExplanationFilesDirectory() =>
        GetExpandedFilesDirectory(ExplanationFilesDirectory);

    public string GetExpandedFilesDirectory(string? baseDirectory = null)
    {
        if (string.IsNullOrEmpty(baseDirectory))
        {
            return string.Empty;
        }

        // Handle environment variable expansion
        var expanded = baseDirectory.Replace("%CommonApplicationData%",
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

        // Handle other common patterns
        expanded = expanded.Replace("%AppData%",
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        expanded = expanded.Replace("%LocalAppData%",
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

        // Also support Environment.ExpandEnvironmentVariables for any other variables
        expanded = Environment.ExpandEnvironmentVariables(expanded);

        // If path is relative, make it absolute relative to AppContext.BaseDirectory
        if (!Path.IsPathRooted(expanded))
        {
            expanded = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expanded));
        }

        return expanded;
    }
}

public sealed class KeyValueStoreSettings
{
    public string StoreName { get; init; } = "NodeSharp.KeyValueStore";
}

public sealed class PersistToDiskSettings
{
    public string FileName { get; init; } = "KeyValueStore.json";
    public TimeSpan FlushInterval { get; init; } = TimeSpan.FromSeconds(0); // 0 means memory store only
}
