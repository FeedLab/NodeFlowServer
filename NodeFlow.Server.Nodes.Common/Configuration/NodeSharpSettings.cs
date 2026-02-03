using System.Text.Json;

namespace NodeFlow.Server.Nodes.Common.Configuration;

public sealed class NodeSharpSettings
{
    public GridSettings Grid { get; init; } = new();
    public DirectoriesSettings Directories { get; init; } = new();
    public ExplanationsPopupSettings ExplanationsPopup { get; init; } = new();
    public KeyValueStoreSettings KeyValueStore { get; init; } = new();
    public PersistToDiskSettings PersistToDisk { get; init; } = new();

    public static async Task<NodeSharpSettings> LoadAsync(string fileName)
    {
        try
        {
            var json = await ReadFileContentAsync(fileName);
            return JsonSerializer.Deserialize<NodeSharpSettings>(json) ?? new NodeSharpSettings();
        }
        catch
        {
            return new NodeSharpSettings();
        }
    }

    private static Task<string> ReadFileContentAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        return File.ReadAllTextAsync(fileName);
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
}
