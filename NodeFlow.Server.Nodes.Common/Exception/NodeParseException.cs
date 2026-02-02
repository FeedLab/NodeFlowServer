namespace NodeSharp.Nodes.Common.Exception;

public class NodeParseException : NodeException
{
    public NodeParseException(BaseNode node, string subNode, System.Exception inner) : base($"Error parsing node: {node.Name}-> {subNode}", inner)
    {
    }
    
    public NodeParseException(string nodeName, System.Exception inner) : base($"Error parsing node: {nodeName}", inner)
    {
    }
    
    public NodeParseException(string nodeName) : base($"Error parsing node: {nodeName}")
    {
    }
    
    public NodeParseException(string nodeName, string message) : base($"Error parsing node: {nodeName}. {message}.")
    {
    }
}

