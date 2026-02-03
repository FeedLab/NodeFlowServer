using Microsoft.Extensions.DependencyInjection;
using NodeFlow.Server.Nodes.Common.Model;

namespace NodeFlow.Server.Nodes.Common;

public interface INodeSharp
{
    public void Register(IServiceCollection services);
    
    public INodeInformation NodeInformation { get; }
    
    public string NodeName { get; }
    
    public Type NodeType { get; }
}