using NodeSharp.Nodes.Common.Model;

namespace NodeSharp.Nodes.Common;

public interface INodeSharp
{
    public void Register(IServiceCollection services);
    
    public INodeInformation NodeInformation { get; }
    
    public string NodeName { get; }
    
    public Type NodeType { get; }
}