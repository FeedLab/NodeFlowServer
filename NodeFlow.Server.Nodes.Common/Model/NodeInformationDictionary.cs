namespace NodeFlow.Server.Nodes.Common.Model;

public class NodeInformationDictionary : Dictionary<string, INodeSharp>
{
    // public void Initialize()
    // {
    //     foreach (var dynamicData in Values)
    //     {
    //         Initialize();
    //     }
    // }
    
    public void AddType(INodeSharp nodeSharpInstance)
    {
        Add(nodeSharpInstance.NodeName, nodeSharpInstance);
    }

    public bool TryGetInformation(string key, out INodeInformation nodeInformation)
    {
        if(TryGetValue(key, out var nodeSharpInstance))
        {
            nodeInformation = nodeSharpInstance.NodeInformation;
            return true;
        }
        else
        {
            throw new InvalidOperationException($"Node type not found: {key}");
        }
    }
    
    public bool TryGetNodeType(string typeId, out Type nodeType)
    {
        if(TryGetValue(typeId, out var nodeSharpInstance))
        {
            nodeType = nodeSharpInstance.NodeType;
            return true;
        }

        nodeType = null!;
        return false;
    }
    
    // public NodeInformationDictionary(Storage storage)
    // {
    //     this.storage = storage;
    //     var types = AssemblyHelper.FindImplementations<INodeInformation>(AppContext.BaseDirectory);
    //
    //     foreach (var dynamicData in storage.TypeDictionary.Values)
    //     {
    //         var instance = AssemblyHelper.FindImplementationsAndCreateInstance<INodeInformation>(dynamicData.Type);
    //         // if (type.FullName == "NodeFlow.Server.Nodes.Common.Model.NodeInformation")
    //         // {
    //         //     continue;
    //         // }
    //         
    //         // var instance = (INodeInformation)Activator.CreateInstance(type)!;
    //
    //         Add(instance.TypeId, instance);
    //     }
    //
    //     var informationText =
    //         "Want me to refactor this to use MVVM entirely (no code-behind), with SelectedItem bound to your ViewModel and a CollectionChanged hook to keep highlights in sync";
    //     
    //     // Add("Inject", new NodeInformation("Inject", true, true, informationText,"\ue713", 0, 1));
    //     // Add("Debug", new NodeInformation("Debug", "NodeDebug",false, true, informationText,"\ue714", 1, 0));
    //     // Add("Delay", new NodeInformation("Delay", "NodeDelay",false, true, informationText,"\ue715", 1, 1));
    //   //  Add("RandomNumber", new NodeInformation("RandomNumber", "NodeRandomNumber",false, true, informationText,"\ue716", 1, 1));
    //     // Add("Function", new NodeInformation("Function", "NodeFunction",false, true, informationText,"\ue718", 1, 1));
    // }
}

// public partial class DynamicData : ObservableObject
// {
//     [ObservableProperty] private readonly Assembly assembly;
//     [ObservableProperty] private INodeSharp nodeSharp;
//     [ObservableProperty] private INodeInformation? nodeInformation;
//
//     public DynamicData(INodeSharp nodeSharp, Assembly assembly)
//     {
//         this.assembly = assembly;
//         this.nodeSharp = nodeSharp;
//     }
//
//     public void Initialize()
//     {
//         NodeInformation = AssemblyHelper.FindImplementationsAndCreateInstance<INodeInformation>(Assembly);
//     }
// }