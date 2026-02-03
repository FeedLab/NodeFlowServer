using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using NodeFlow.Server.Nodes.Common;
using NodeFlow.Server.Nodes.Common.Collection;
using NodeFlow.Server.Nodes.Common.Exception;
using NodeFlow.Server.Nodes.Common.Extension;
using NodeFlow.Server.Nodes.Common.Model;
using NodeFlow.Server.Nodes.Common.Services;

namespace NodeSharp.NodeEngine;

public class NodeIo(Storage storage)
{
    private readonly BaseNodeList nodes = [];
    private readonly NodeFactory nodeFactory = new(storage);
    private string? fileNameSaved;

    public BaseNodeList Nodes => nodes;

    public string? FileNameSaved
    {
        get => fileNameSaved;
        set => fileNameSaved = value;
    }

    public bool IsFlowRunning { get; set; }

    public async Task SaveToFileAsync()
    {
        if (fileNameSaved is null)
        {
            throw new InvalidOperationException("No file name exists. Call LoadFromFileAsync first.");
        }

        if (File.Exists(fileNameSaved))
        {
            File.Delete(fileNameSaved);
        }

        using var stream = File.Create(fileNameSaved);

        await SaveToFileAsync(stream);
    }

    public async Task SaveToFileAsync(Stream stream)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = options.WriteIndented });

        writer.WriteStartObject();
        writer.WritePropertyName("Nodes");

        writer.WriteStartArray();
        foreach (var node in nodes)
        {
            JsonSerializer.Serialize(writer, node, node.GetType(), options);
        }

        writer.WriteEndArray();

        writer.WriteEndObject();

        await writer.FlushAsync();
    }

    public async Task LoadFromFileAsync(StreamReader reader, string filePath)
    {
        fileNameSaved = filePath;

        var nodeDataJson = await reader.ReadToEndAsync();

        if (nodeDataJson is null)
        {
            throw new InvalidOperationException($"Node data JSON is null. File name is: {filePath}");
        }

        Clear();

        var document = JsonDocument.Parse(nodeDataJson);
        var nodesArray = document.RootElement.GetProperty("Nodes");

        try
        {
            ParseNodesFromJson(nodesArray);
        }
        catch (System.Exception e)
        {
            Debug.WriteLine(e.Message);

            throw;
        }

        nodes.ValidateInputAndOutputNodes();

        Console.WriteLine($"Loaded {nodes.Count} nodes:");

        foreach (var node in nodes)
        {
            Console.WriteLine(
                $"  - {node.Name} ({node.TypeId}): {node.Outputs.Count} outputs, {node.Inputs.Count} inputs");
        }
    }

    public async Task Run()
    {
        await Task.Run(() =>
        {
            IsFlowRunning = true;

            _ = nodes.Run();
        });
    }


    // public T? FindNodeFromId<T>(string id) where T : BaseNode => nodes.OfType<T>().SingleOrDefault(x => x.Id == id);

    void ParseNodesFromJson(JsonElement jsonElement)
    {
        foreach (var nodeElement in jsonElement.EnumerateArray())
        {
            var id = GetProperty(nodeElement, "Id").GetString()!;
            var typeId = GetProperty(nodeElement, "TypeId").GetString()!;
            var name = GetProperty(nodeElement, "Name").GetString()!;
            var xPosition = TryGetProperty(nodeElement, "X", out var xProp) &&
                            xProp.ValueKind == JsonValueKind.Number
                ? xProp.GetInt32()
                : 100;

            var yPosition = TryGetProperty(nodeElement, "Y", out var yProp) &&
                            yProp.ValueKind == JsonValueKind.Number
                ? yProp.GetInt32()
                : 100;

            var isEnabled = ReadBool(nodeElement, preferredPropertyName: "IsEnabled",
                fallbackPropertyName: "Enabled");
            var activateOnStart = TryGetProperty(nodeElement, "ActivateOnStart", out var activateOnStartProp) &&
                                  activateOnStartProp.ValueKind == JsonValueKind.True;

            var outputs = ParseOutputs(GetProperty(nodeElement, "Outputs"));
            var inputs = ParseInputs(GetProperty(nodeElement, "Inputs"));

            var node = nodeFactory.CreateNodeFromJson(nodes, id, typeId, name, isEnabled, activateOnStart,
                xPosition, yPosition, outputs, inputs, nodeElement);

            nodes.Add(node);
        }

        static bool ReadBool(JsonElement element, string preferredPropertyName, string fallbackPropertyName)
        {
            if (element.TryGetProperty(preferredPropertyName, out var preferred))
            {
                return preferred.GetBoolean();
            }

            return element.GetProperty(fallbackPropertyName).GetBoolean();
        }
    }

    private static JsonElement GetProperty(JsonElement nodeElement, string propertyName)
    {
        try
        {
            return nodeElement.GetProperty(propertyName);
        }
        catch (System.Exception e)
        {
            throw new NodeParseException(propertyName, e);
        }
    }

    private static bool TryGetProperty(JsonElement nodeElement, string propertyName, out JsonElement propertyValue)
    {
        try
        {
            return nodeElement.TryGetProperty(propertyName, out propertyValue);
        }
        catch (System.Exception e)
        {
            throw new NodeParseException(propertyName, e);
        }
    }

    static Point GetStartPosition(JsonElement element)
    {
        if (element.TryGetPropertyIgnoreCase("StartPosition", out var startPosProp) &&
            startPosProp.ValueKind == JsonValueKind.Object)
        {
            var x = startPosProp.TryGetPropertyIgnoreCase("X", out var xProp) &&
                    xProp.ValueKind == JsonValueKind.Number && xProp.TryGetDouble(out var xi)
                ? xi
                : 0;

            var y = startPosProp.TryGetPropertyIgnoreCase("Y", out var yProp) &&
                    yProp.ValueKind == JsonValueKind.Number && yProp.TryGetDouble(out var yi)
                ? yi
                : 0;

            return new Point((int)x, (int)y);
        }

        // Fallback to reading X and Y directly from the element
        var xDirect = element.TryGetPropertyIgnoreCase("X", out var xDirectProp) &&
                      xDirectProp.ValueKind == JsonValueKind.Number && xDirectProp.TryGetDouble(out var xDirectValue)
            ? xDirectValue
            : 0;

        var yDirect = element.TryGetPropertyIgnoreCase("Y", out var yDirectProp) &&
                      yDirectProp.ValueKind == JsonValueKind.Number && yDirectProp.TryGetDouble(out var yDirectValue)
            ? yDirectValue
            : 0;

        return new Point((int)xDirect, (int)yDirect);
    }

    static List<Output> ParseOutputs(JsonElement outputsElement)
    {
        if (outputsElement.ValueKind != JsonValueKind.Array)
            return new List<Output>();

        var result = new List<Output>();

        foreach (var o in outputsElement.EnumerateArray())
        {
            string name = o.TryGetPropertyIgnoreCase("Name", out var nameProp) &&
                          nameProp.ValueKind == JsonValueKind.String
                ? nameProp.GetString() ?? string.Empty
                : string.Empty;

            var connectsTo = new List<string>();
            if (o.TryGetPropertyIgnoreCase("connectsToNodeId", out var cProp) && cProp.ValueKind == JsonValueKind.Array)
            {
                connectsTo = cProp.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .ToList();
            }


            var startPosition = GetStartPosition(o);

            var id = o.TryGetPropertyIgnoreCase("Id", out var idProp) && idProp.ValueKind == JsonValueKind.String &&
                     idProp.TryGetGuid(out var idi)
                ? idi
                : Guid.CreateVersion7();

            result.Add(new Output(id, name, connectsTo.ToList(), startPosition));
        }

        return result;
    }


    static List<Input> ParseInputs(JsonElement inputsElement)
    {
        if (inputsElement.ValueKind != JsonValueKind.Array)
            return new List<Input>();

        var result = new List<Input>();

        foreach (var o in inputsElement.EnumerateArray())
        {
            var name = o.TryGetPropertyIgnoreCase("Name", out var nameProp) &&
                       nameProp.ValueKind == JsonValueKind.String
                ? nameProp.GetString() ?? string.Empty
                : string.Empty;

            var connectsTo = new List<string>();
            if (o.TryGetPropertyIgnoreCase("ConnectsToParentNodeId", out var cProp) &&
                cProp.ValueKind == JsonValueKind.Array)
            {
                connectsTo = cProp.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .ToList();
            }

            var startPosition = GetStartPosition(o);

            var id = o.TryGetPropertyIgnoreCase("Id", out var idProp) && idProp.ValueKind == JsonValueKind.String &&
                     idProp.TryGetGuid(out var idi)
                ? idi
                : Guid.CreateVersion7();

            result.Add(new Input(id, name, connectsTo.ToList(), startPosition));
        }

        return result;

        //
        // return inputsElement.EnumerateArray()
        //     .Select(i => new Input(
        //         i.GetProperty("Name").GetString()!,
        //         i.GetProperty("ConnectsToParentNodeId").EnumerateArray().Select(x => x.GetString()!).ToObservableCollection()
        //         , new Point(
        //             i.TryGetProperty("X", out var xProp) && xProp.ValueKind == JsonValueKind.Number ? xProp.GetInt32() : 0,
        //             i.TryGetProperty("Y", out var yProp) && yProp.ValueKind == JsonValueKind.Number ? yProp.GetInt32() : 0
        //         ),
        //         i.TryGetProperty("Id", out var idProp) && idProp.ValueKind == JsonValueKind.String && idProp.TryGetGuid(out var idi)
        //             ? idi
        //             : Guid.CreateVersion7()
        //     ))
        //     .ToList();
    }

    public void Clear()
    {
        Nodes.Clear();
        fileNameSaved = null;
    }

    public void Add(INodeInformation nodeInfo, double dropX, double dropY)
    {
        var id = $"{Guid.CreateVersion7()}";
        var typeId = nodeInfo.TypeId;
        var name = $"{nodeInfo.TypeId} {nodes.Count + 1}";
        var xPosition = (int)dropX;
        var yPosition = (int)dropY;
        var isEnabled = nodeInfo.IsEnabled;
        var activateOnStart = nodeInfo.ActivateOnStart;
    //    var backgroundColor = nodeInfo.Background;

        var node = nodeFactory.CreateNode(nodes, id, typeId, name, isEnabled, activateOnStart,
            xPosition, yPosition, storage);

        nodes.Add(node);
    }

    public void Add(INodeInformation nodeInfo, Point position)
    {
        var id = $"{Guid.CreateVersion7()}";
        var typeId = nodeInfo.TypeId;
        var name = $"{nodeInfo.TypeId} {nodes.Count + 1}";
        // var xPosition = (int)dropX;
        // var yPosition = (int)dropY;
        var isEnabled = nodeInfo.IsEnabled;
        var activateOnStart = nodeInfo.ActivateOnStart;
        // var backgroundColor = nodeInfo.Background;

        var node = nodeFactory.CreateNode(nodes, id, typeId, name, isEnabled, activateOnStart,
            0, 0, storage);

        // node.X = (int)(position.X - (node.BoxDimension.Width / 2));
        // node.Y = (int)(position.Y - (node.BoxDimension.Height / 2));

        nodes.Add(node);
    }

    public void Abort()
    {
        foreach (var node in Nodes)
        {
            node.Abort();
        }

        IsFlowRunning = false;
    }
}