using System.Text.Json;
using NodeFlow.Server.Nodes.Common.Collection;
using NodeFlow.Server.Nodes.Common.Model;

namespace NodeFlow.Server.Nodes.Common.Services;

public class NodeFactory
{
    private readonly Storage storage;

    public NodeFactory(Storage storage)
    {
        this.storage = storage;
    }

    public BaseNode CreateNode(
        BaseNodeList nodes,
        string id,
        string typeId,
        string name,
        bool isEnabled,
        bool activateOnStart,
        int xPosition,
        int yPosition,
        Storage storageParam)
    {
        var nodeType = GetNodeType(typeId);

        // Find the constructor that takes Storage parameter (for new node creation)
        var constructor = nodeType.GetConstructors()
            .FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length >= 9 &&
                       parameters[0].ParameterType == typeof(BaseNodeList) &&
                       parameters[8].ParameterType == typeof(Storage);
            });

        if (constructor == null)
        {
            throw new InvalidOperationException(
                $"Could not find appropriate constructor for node type: {typeId}");
        }

        var constructorParams = constructor.GetParameters();
        var parameters = new object[constructorParams.Length];

        // Fill in the known parameters
        parameters[0] = nodes;
        parameters[1] = id;
        parameters[2] = typeId;
        parameters[3] = name;
        parameters[4] = isEnabled;
        parameters[5] = activateOnStart;
        parameters[6] = xPosition;
        parameters[7] = yPosition;
        parameters[8] = storageParam;

        // For any additional parameters (like RandomDataPayload, DelayPayload, etc.),
        // create default instances using Activator
        for (int i = 10; i < constructorParams.Length; i++)
        {
            var paramType = constructorParams[i].ParameterType;
            parameters[i] = Activator.CreateInstance(paramType)!;
        }

        var instance = constructor.Invoke(parameters);

        if (instance is not BaseNode node)
        {
            throw new InvalidOperationException($"Failed to create node of type: {typeId}");
        }

        return node;
    }

    public BaseNode CreateNodeFromJson(
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
        JsonElement nodeElement)
    {
        var nodeType = GetNodeType(typeId);

        // Find the constructor that takes JsonElement parameter (for JSON deserialization)
        var constructor = nodeType.GetConstructors()
            .FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == 11 &&
                       parameters[0].ParameterType == typeof(BaseNodeList) &&
                       parameters[8].ParameterType == typeof(List<Output>) &&
                       parameters[9].ParameterType == typeof(List<Input>) &&
                       parameters[10].ParameterType == typeof(JsonElement);
            });

        if (constructor == null)
        {
            throw new InvalidOperationException(
                $"Could not find appropriate JSON constructor for node type: {typeId}");
        }

        var parameters = new object[]
        {
            nodes, id, typeId, name, isEnabled, activateOnStart, xPosition, yPosition, outputs, inputs, nodeElement
        };

        var instance = constructor.Invoke(parameters);

        if (instance is not BaseNode node)
        {
            throw new InvalidOperationException($"Failed to create node of type: {typeId}");
        }

        return node;
    }

    private Type GetNodeType(string typeId)
    {
        if (!storage.GetNodeInformation().TryGetNodeType(typeId, out var nodeType))
        {
            throw new InvalidOperationException($"Unknown node type: {typeId}");
        }

        return nodeType;
    }
}
