using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeSharp.Nodes.Common.Collection;
using NodeSharp.Nodes.Common.Exception;
using NodeSharp.Nodes.Common.Extension;
using NodeSharp.Nodes.Common.Model;
using NodeSharp.Nodes.Common.Services;

namespace NodeSharp.Nodes.Common;

[SuppressMessage("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator",
    "MVVMTK0045:Using [ObservableProperty] on fields is not AOT compatible for WinRT")]
[SuppressMessage("Usage", "CsWinRT1030:Project does not enable unsafe blocks")]
[SuppressMessage("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator",
    "MVVMTK0020:Invalid use of attributes dependent on [ObservableProperty]")]
public abstract partial class BaseNode : ObservableObject
{
    public event EventHandler<(BaseNode baseNode, string level, string message, string entry)>? OnExitNodeMessage;
    public event EventHandler<BaseNode>? OnEnterNode;
    public event EventHandler<(BaseNode, Stopwatch)>? OnLeaveNode;

    protected readonly IPopupService PopupService;

    // [JsonIgnore] [NotifyPropertyChangedFor(nameof(HasOutputMessage))]
    [ObservableProperty][JsonIgnore] private string outputMessage;

    partial void OnOutputMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasOutputMessage));
    }
    // [JsonIgnore]
    // public string OutputMessage
    // {
    //     get => outputMessage;
    //     set
    //     {
    //         if (!string.IsNullOrEmpty(value))
    //         {
    //             SetProperty(ref outputMessage, value.ToPrettyJson());
    //         }
    //     }
    // }

    [JsonIgnore] public bool HasOutputMessage => !string.IsNullOrEmpty(OutputMessage);

    [ObservableProperty]
    [property: JsonIgnore]
    private Rect boxDimension;
    
    [ObservableProperty]
    [property: JsonIgnore]
    private Rect boxBodyDimension;
    
    [ObservableProperty]
    [property: JsonIgnore]
    private BoxNodeStatus boxNodeStatus;

    [ObservableProperty]
    [property: JsonIgnore]
    private INodeInformation typeInformation;

    [ObservableProperty]
    [property: JsonIgnore]
    private ContentView? nodeBodyComponent;

    [ObservableProperty]
    [property: JsonIgnore]
    private ContentView? boxNodeStatusComponent;

    [ObservableProperty]
    [property: JsonIgnore]
    private ExplanationCollection explanations = new(string.Empty);

    [JsonIgnore] protected CancellationTokenSource Cts;

    [JsonIgnore] private BaseNodeList Nodes { get; }

    public string Id { get; }
    public string TypeId { get; }
    public string Name { get; }
    
    public Color BackgroundColor { get; private set; }
    public bool IsEnabled { get; }
    public bool ActivateOnStart { get; }
    public int X { get; set; }
    public int Y { get; set; }
    public IList<Output> Outputs { get; }
    public IList<Input> Inputs { get; }

    protected BaseNode(
        BaseNodeList nodes,
        string id,
        string typeId,
        string name,
        bool isEnabled,
        bool activateOnStart,
        int xPosition,
        int yPosition,
        Storage storage, 
        Color backgroundColor)
    {
        Cts = new CancellationTokenSource();
        BoxNodeStatus = new BoxNodeStatus();
        PopupService = AppService.GetRequiredService<IPopupService>();
        OutputMessage = string.Empty;
        BackgroundColor = backgroundColor ?? Colors.White;
        BoxDimension = new Rect(0, 0, 240, 60);
        BoxBodyDimension = new Rect(0, 0, 140, 48);

        if (storage.GetNodeInformation().TryGetValue(typeId, out var nodeSharp))
        {
            TypeInformation = nodeSharp.NodeInformation;
        }
        else
        {
            throw new InvalidOperationException($"Node type not found: {name}");
        }

        if (storage.GetNodeInformation().TryGetInformation(typeId, out var nodeType))
        {
            Inputs = new List<Input>();
            Outputs = new List<Output>();

            if (Inputs.Count == 0 && nodeType is not null)
            {
                for (var input = 0; input < nodeType.NumberOfInputs; input++)
                {
                    Inputs.Add(new Input(Guid.CreateVersion7(), "Input 1", new ObservableCollection<string>(),
                        new Point(1, 1)));
                }
            }

            if (Outputs.Count == 0 && nodeType is not null)
            {
                for (var output = 0; output < nodeType.NumberOfOutputs; output++)
                {
                    Outputs.Add(new Output(Guid.CreateVersion7(), "Output 1", new ObservableCollection<string>(),
                        new Point(1, 1)));
                }
            }
        }
        else
        {
            throw new InvalidOperationException($"Node type not found: {typeId}");
        }

        Nodes = nodes;
        Id = id;
        TypeId = typeId;
        Name = name;
        IsEnabled = isEnabled;
        ActivateOnStart = activateOnStart;
        X = xPosition;
        Y = yPosition;

        Explanations = new ExplanationCollection(TypeId);
        Explanations.LoadFromFile();
    }


    protected BaseNode(
        BaseNodeList nodes,
        string id,
        string typeId,
        string name,
        bool isEnabled,
        bool activateOnStart,
        int xPosition,
        int yPosition,
        List<Output> outputs,
        List<Input> inputs)
    {
        BoxNodeStatus = new BoxNodeStatus();
        OutputMessage = string.Empty;
        BoxDimension = new Rect(0, 0, 260, 60);
        BoxBodyDimension = new Rect(0, 0, 140, 48);

        var storage = AppService.GetRequiredService<Storage>();
        PopupService = AppService.GetRequiredService<IPopupService>();

        if (storage.GetNodeInformation().TryGetValue(typeId, out var nodeSharp))
        {
            TypeInformation = nodeSharp.NodeInformation;
        }
        else
        {
            throw new InvalidOperationException($"Node type not found: {name}");
        }

        Cts = new CancellationTokenSource();

        Nodes = nodes;
        Id = id;
        TypeId = typeId;
        Name = name;
        IsEnabled = isEnabled;
        ActivateOnStart = activateOnStart;
        X = xPosition;
        Y = yPosition;
        Outputs = outputs;
        Inputs = inputs;
        BackgroundColor = TypeInformation.Background ?? Colors.White;

        Explanations = new ExplanationCollection(TypeId);
        Explanations.LoadFromFile();
    }

    public void Abort()
    {
        Cts?.Cancel();
    }

    public virtual async Task DisplayNodeConfigurationPopup()
    {
        Debug.WriteLine("Node configuration popup not implemented for this node type");
    }

    protected virtual void ExitNodeMessage(BaseNode baseNode, string level, string message, string entry)
    {
        OnExitNodeMessage?.Invoke(this, (baseNode, level, message, entry));
    }

    protected virtual Stopwatch EnterNode(BaseNode node)
    {
        var stopWatch = Stopwatch.StartNew();
        OnEnterNode?.Invoke(this, node);

        return stopWatch;
    }

    protected virtual void LeaveNode(BaseNode node, Stopwatch stopWatch)
    {
        OnLeaveNode?.Invoke(this, (node, stopWatch));
    }

    public virtual Task<string> Run()
    {
        OutputMessage = "";

        if (!ActivateOnStart)
        {
            return Task.FromResult("Not a ActivateOnStart node");
        }

        Cts = new CancellationTokenSource();

        var message = $"BaseNode {FormatNode()} has been activated during start of node";

        Debug.WriteLine(message);

        return Task.FromResult(message);
    }

    // protected virtual Task<JsonNode> RunFromInput(BaseNode parent, JsonNode inputJson)
    // {
    //     Cts = new CancellationTokenSource();
    //
    //     var message = $"Node {FormatNode()} has been activated by parent node {parent.FormatNode()}";
    //
    //     Debug.WriteLine(message);
    //
    //     return Task.FromResult(inputJson);
    // }

    protected virtual Task<JsonNode?> RunFromInput(BaseNode parent, string inputJsonString)
    {
        Cts = new CancellationTokenSource();

        var inputJson = JsonNode.Parse(inputJsonString);


        var message = $"Node {FormatNode()} has been activated by parent node {parent.FormatNode()}";

        Debug.WriteLine(message);

        return Task.FromResult(inputJson);
    }

    protected async Task SendToConnectedChildrenAsync(JsonNode outputNode, Output output)
    {
        var outputJsonString = outputNode.ToJsonString();

        OutputMessage = outputJsonString.ToPrettyJson();

        await SendToConnectedChildrenAsync(outputJsonString, output);
    }

    protected Task SendToConnectedChildrenAsync(string outputJsonString, Output output)
    {
        _ = Task.Run(() =>
        {
            var tasks = new List<Task>();

            foreach (var nodeId in output.ConnectsToNodeId)
            {
                var targetNode = FindNode(nodeId);
                if (targetNode is null)
                {
                    throw new InvalidOperationException($"Node not found: {nodeId}");
                }

                tasks.Add(targetNode.RunFromInput(this, outputJsonString));
            }

            return Task.FromResult(Task.WhenAll(tasks));
        });

        return Task.FromResult(outputJsonString);
    }

    protected Task SendToConnectedChildrenAsync(JsonNode outputNode)
    {
        var outputJsonString = outputNode.ToJsonString();

        OutputMessage = outputJsonString.ToPrettyJson();

        return SendToConnectedChildrenAsync(outputNode.ToJsonString());
    }

    protected Task SendToConnectedChildrenAsync(string parametersJsonString)
    {
        _ = Task.Run(() =>
        {
            var tasks = new List<Task>();

            foreach (var output in Outputs)
            {
                foreach (var nodeId in output.ConnectsToNodeId)
                {
                    var targetNode = FindNode(nodeId);
                    if (targetNode is null)
                    {
                        throw new InvalidOperationException($"Node not found: {nodeId}");
                    }

                    tasks.Add(targetNode.RunFromInput(this, parametersJsonString));
                }
            }

            return Task.FromResult(Task.WhenAll(tasks));
        });

        return Task.FromResult(parametersJsonString);
    }

    private BaseNode FindNode(string nodeId)
    {
        foreach (var node in Nodes)
        {
            foreach (var input in node.Inputs)
            {
                if (input.Id.ToString() == nodeId)
                {
                    return node;
                }
            }
        }

        throw new InvalidOperationException($"Node not found: {nodeId}");
    }

    public virtual void Reset()
    {
        BoxNodeStatus.Reset();
    }

    public void ValidateInputAndOutput()
    {
        Debug.WriteLine($"Validating NodeInject: {Name} (Id: {Id})");
        ValidateNodeId();

        ValidateInputConnections(Nodes);
        ValidateOutputConnections(Nodes);

        Debug.WriteLine($"NodeInject {Name} validation completed successfully");
    }

    private void ValidateNodeId()
    {
        Debug.WriteLine($"Validating NodeId: {Id}");
        if (!Guid.TryParse(Id, out _))
        {
            Debug.WriteLine($"NodeId '{Id}' is not a valid GUID - throwing exception");
            throw new InvalidOperationException($"NodeId '{Id}' is not a valid GUID.");
        }

        Debug.WriteLine("NodeId is valid");
    }

    private void ValidateOutputConnections(BaseNodeList baseNodeList)
    {
        Debug.WriteLine($"Validating {Outputs.Count} output connections");

        // ValidateConnections(
        //     baseNodeList,
        //     connections: Outputs.SelectMany(o =>
        //         o.ConnectsToNodeId.Select(nodeId => (PortName: o.Name, NodeId: nodeId))),
        //     idLabel: "Output ConnectsToNodeId",
        //     missingNodeMessage: (portName, nodeId) =>
        //         $"Output '{portName}' connects to non-existing node '{nodeId}'. ");
    }

    private void ValidateInputConnections(BaseNodeList baseNodeList)
    {
        Debug.WriteLine($"Validating {Inputs.Count} input connections");

        // ValidateConnections(
        //     baseNodeList,
        //     connections: Inputs.SelectMany(i =>
        //         i.ConnectsToParentNodeId.Select(nodeId => (PortName: i.Name, NodeId: nodeId))),
        //     idLabel: "Input ConnectsToParentNodeId",
        //     missingNodeMessage: (portName, nodeId) =>
        //         $"Input '{portName}' connects to non-existing parent node '{nodeId}'. ");
    }

    private void ValidateConnections(
        BaseNodeList baseNodeList,
        IEnumerable<(string PortName, string NodeId)> connections,
        string idLabel,
        Func<string, string, string> missingNodeMessage)
    {
        var errors = new StringBuilder();

        foreach (var (portName, nodeId) in connections)
        {
            Debug.WriteLine($"Checking connection for '{portName}': {nodeId}");

            if (!Guid.TryParse(nodeId, out _))
            {
                Debug.WriteLine($"Invalid GUID: {nodeId}");
                errors.Append($"{idLabel} '{nodeId}' is not a valid GUID. ");
                continue;
            }

            if (baseNodeList.All(n => n.Id != nodeId))
            {
                Debug.WriteLine($"Node not found: {nodeId}");
                errors.Append(missingNodeMessage(portName, nodeId));
                continue;
            }

            Debug.WriteLine($"Node connection valid: {nodeId}");
        }

        if (errors.Length > 0)
        {
            Debug.WriteLine($"Validation errors found: {errors}");
            throw new InvalidOperationException(errors.ToString().Trim());
        }
    }

    protected static JsonElement GetProperty(JsonElement nodeElement, string propertyName)
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

    protected static bool TryGetProperty(JsonElement nodeElement, string propertyName, out JsonElement propertyValue)
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

    private string FormatNode() => $"{Name}:{TypeId}";

    public virtual void RecalculateInputNodes(double height)
    {
    }

    public virtual void RecalculateOutputNodes(double height)
    {
    }
}

public partial class Input : ObservableObject
{
    public Input(Guid id, string name, ObservableCollection<string> connectsToParentNodeId, Point startPosition)
    {
        Id = id;
        Name = name;
        ConnectsToParentNodeId = connectsToParentNodeId;
        StartPosition = startPosition;
    }

    [ObservableProperty][JsonIgnore] private Point startPosition;

    [ObservableProperty][JsonIgnore] private Guid id;

    [ObservableProperty][JsonIgnore] private string name;

    [ObservableProperty][JsonIgnore] private ObservableCollection<string> connectsToParentNodeId;
}

public partial class Output : ObservableObject
{
    public Output(Guid id, string name, ObservableCollection<string> connectsToNodeId, Point startPosition)
    {
        Id = id;
        Name = name;
        ConnectsToNodeId = connectsToNodeId;
        StartPosition = startPosition;
    }

    [ObservableProperty][JsonIgnore] private Point startPosition;

    [ObservableProperty][JsonIgnore] private Guid id;

    [ObservableProperty][JsonIgnore] private string name;

    [ObservableProperty][JsonIgnore] private ObservableCollection<string> connectsToNodeId;
}

[SuppressMessage("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator",
    "MVVMTK0045:Using [ObservableProperty] on fields is not AOT compatible for WinRT")]
public partial class BoxNodeStatus : ObservableObject
{
    [JsonIgnore][ObservableProperty] private decimal value;

    [JsonIgnore][ObservableProperty] private string message;

    public BoxNodeStatus()
    {
        Value = 0;
        Message = "";
    }

    public void Reset()
    {
        Value = 0;
        Message = "";
    }
}

public partial class ExplanationItem : ObservableObject
{
    [ObservableProperty]
    private string label = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string code = string.Empty;

    [ObservableProperty]
    [property: JsonIgnore]
    private string fileName = string.Empty;
}
