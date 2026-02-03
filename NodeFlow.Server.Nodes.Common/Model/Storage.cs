namespace NodeFlow.Server.Nodes.Common.Model;

public class Storage
{
    private readonly NodeInformationDictionary nodeInformationDictionary = new();

    // public IDictionary<Type, DynamicData> TypeDictionary { get; } = new Dictionary<Type, DynamicData>();

    // public IDictionary<string, INodeInformation> GetNodeInformation()
    // {
    //     return TypeDictionary.Select(s => new KeyValuePair<string, INodeInformation>(s.Value?.Information.TypeId, s.Value.Information))
    //         .ToDictionary(s => s.Key, s => s.Value);
    // }

    public NodeInformationDictionary GetNodeInformation()
    {
        return nodeInformationDictionary;
    }
    //
    // public void AddType(Type type, INodeSharp instance)
    // {
    //     var dynamicData = new DynamicData(instance, type);
    //
    //     TypeDictionary.Add(type, dynamicData);
    // }

    public void Initialize()
    {
        // nodeInformationDictionary.Initialize();
    }
}

