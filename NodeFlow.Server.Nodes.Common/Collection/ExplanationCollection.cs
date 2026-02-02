using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using NodeSharp.Nodes.Common.Services;

namespace NodeSharp.Nodes.Common.Collection;

public sealed class ExplanationCollection(string typeId) : ObservableCollection<ExplanationItem>
{
    private bool suppressSave;

    public void LoadFromFile()
    {
        try
        {
            suppressSave = true;
            var explanationsPath = GetExplanationsPath();
            if (string.IsNullOrEmpty(explanationsPath) || !Directory.Exists(explanationsPath))
            {
                return;
            }

            var fileName = $"{typeId}.json";
            var filePath = Path.Combine(explanationsPath, fileName);
            if (!File.Exists(filePath))
            {
                return;
            }

            var json = File.ReadAllText(filePath);
            var items = JsonSerializer.Deserialize<List<ExplanationItem>>(json);
            if (items == null)
            {
                return;
            }

            Clear();
            foreach (var item in items)
            {
                item.FileName = fileName;
                Add(item);
            }
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"Failed to load explanations for {typeId}: {ex.Message}");
        }
        finally
        {
            suppressSave = false;
        }
    }

    private void SaveToFile()
    {
        if (suppressSave || string.IsNullOrWhiteSpace(typeId))
        {
            return;
        }

        try
        {
            var explanationsPath = GetExplanationsPath();
            if (string.IsNullOrEmpty(explanationsPath))
            {
                return;
            }

            Directory.CreateDirectory(explanationsPath);
            var filePath = Path.Combine(explanationsPath, $"{typeId}.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"Failed to save explanations for {typeId}: {ex.Message}");
        }
    }

    protected override void InsertItem(int index, ExplanationItem item)
    {
        PrepareItem(item);
        base.InsertItem(index, item);
        if (!suppressSave)
        {
            SaveToFile();
        }
    }

    protected override void SetItem(int index, ExplanationItem item)
    {
        var existing = this[index];
        existing.PropertyChanged -= OnItemPropertyChanged;
        PrepareItem(item);
        base.SetItem(index, item);
        if (!suppressSave)
        {
            SaveToFile();
        }
    }

    protected override void RemoveItem(int index)
    {
        var existing = this[index];
        existing.PropertyChanged -= OnItemPropertyChanged;
        base.RemoveItem(index);
        if (!suppressSave)
        {
            SaveToFile();
        }
    }

    protected override void ClearItems()
    {
        foreach (var item in this)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }

        base.ClearItems();
        if (!suppressSave)
        {
            SaveToFile();
        }
    }

    private void PrepareItem(ExplanationItem item)
    {
        if (string.IsNullOrWhiteSpace(item.FileName))
        {
            item.FileName = $"{typeId}.json";
        }

        item.PropertyChanged += OnItemPropertyChanged;
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (suppressSave || e.PropertyName == nameof(ExplanationItem.FileName))
        {
            return;
        }

        SaveToFile();
    }

    private static string? GetExplanationsPath()
    {
        var popupSettings = AppService.GetService<Microsoft.Extensions.Options.IOptions<Configuration.ExplanationsPopupSettings>>()?.Value;
        return popupSettings?.GetExpandedExplanationFilesDirectory();
    }
}
