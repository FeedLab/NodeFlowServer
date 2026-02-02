using NodeSharp.Nodes.Common;
using NodeSharp.Nodes.Common.Model;
using NodeSharp.Nodes.Common.Services;

namespace NodeSharp.Nodes.Inject;

public class Startup : INodeSharp
{
    public void Register(IServiceCollection services)
    {
        services.AddKeyedSingleton<INodeInformation, NodeInformation>(NodeName);
    }

    public INodeInformation NodeInformation => AppService.GetRequiredKeyedService<INodeInformation>(NodeName);

    public string NodeName => "Inject";
    
    public Type NodeType => typeof(NodeInject);
}