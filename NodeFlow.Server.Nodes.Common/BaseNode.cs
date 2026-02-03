using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using NodeFlow.Server.Nodes.Common.Collection;
using NodeFlow.Server.Nodes.Common.Exception;
using NodeFlow.Server.Nodes.Common.Extension;
using NodeFlow.Server.Nodes.Common.Model;
using NodeFlow.Server.Nodes.Common.Services;

namespace NodeFlow.Server.Nodes.Common;

public abstract partial class BaseNode
{
    public event EventHandler<(BaseNode baseNode, string level, string message, string entry)>? OnExitNodeMessage;
    public event EventHandler<BaseNode>? OnEnterNode;
    public event EventHandler<(BaseNode, Stopwatch)>? OnLeaveNode;

    [field: JsonIgnore] public string OutputMessage { get; set; }


    [JsonIgnore] public bool HasOutputMessage => !string.IsNullOrEmpty(OutputMessage);


    [field: JsonIgnore] public BoxNodeStatus NodeStatus { get; }


    [field: JsonIgnore] public INodeInformation TypeInformation { get; }


    [field: JsonIgnore] public ExplanationCollection Explanations { get; } = new(string.Empty);

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
        Storage storage
        )
    {
        Cts = new CancellationTokenSource();
        BoxNodeStatus = new BoxNodeStatus();
        OutputMessage = string.Empty;

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
                    Inputs.Add(new Input(Guid.CreateVersion7(), "Input 1", new List<string>(),
                        new Point(1, 1)));
                }
            }

            if (Outputs.Count == 0 && nodeType is not null)
            {
                for (var output = 0; output < nodeType.NumberOfOutputs; output++)
                {
                    Outputs.Add(new Output(Guid.CreateVersion7(), "Output 1", new List<string>(),
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

    public BoxNodeStatus BoxNodeStatus { get; set; }


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
        List<Input> inputs,
        Storage storage)
    {
        NodeStatus = new BoxNodeStatus();
        OutputMessage = string.Empty;

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
    }

    private string FormatNode() => $"{Name}:{TypeId}";

}

public partial class Input(
    Guid id,
    string name,
    List<string> connectsToParentNodeId,
    Point startPosition)
{
    [field: JsonIgnore] public Point StartPosition { get; } = startPosition;

    [field: JsonIgnore] public Guid Id { get; } = id;

    [field: JsonIgnore] public string Name { get; } = name;

    [field: JsonIgnore] public List<string> ConnectsToParentNodeId { get; } = connectsToParentNodeId;
}

public class Output(Guid id, string name, List<string> connectsToNodeId, Point startPosition)
{
    [field: JsonIgnore] public Point StartPosition { get; } = startPosition;

    [field: JsonIgnore] public Guid Id { get; } = id;

    [field: JsonIgnore] public string Name { get; } = name;

    [field: JsonIgnore] public List<string> ConnectsToNodeId { get; } = connectsToNodeId;
}

public partial class BoxNodeStatus
{
    [field: JsonIgnore] public decimal Value { get; set; }

    [field: JsonIgnore] public string Message { get; set; }

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

public partial class ExplanationItem 
{
    
    private string label = string.Empty;

    
    private string description = string.Empty;

    
    private string code = string.Empty;

    
    [property: JsonIgnore]
    private string fileName = string.Empty;
}
