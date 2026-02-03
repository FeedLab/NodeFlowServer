using Microsoft.Extensions.DependencyInjection;
using NodeFlow.Server.Nodes.Common;
using NodeFlow.Server.Nodes.Common.Model;
using NodeFlow.Server.Nodes.Common.Services;

namespace NodeFlow.Server.Nodes.Inject;

public class Startup : INodeSharp
{   
    private IServiceCollection services;

    public void Register(IServiceCollection services)
    {
        this.services = services;
        
        services.AddKeyedSingleton<INodeInformation, NodeInformation>(NodeName);
    }

    public INodeInformation NodeInformation => new NodeInformation(); // services.GetRequiredKeyedService<INodeInformation>(NodeName);

    public string NodeName => "Inject";
    
    public Type NodeType => typeof(NodeInject);
}