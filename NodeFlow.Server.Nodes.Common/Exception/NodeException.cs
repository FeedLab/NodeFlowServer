namespace NodeFlow.Server.Nodes.Common.Exception;

public class NodeException : System.Exception
{
    public NodeException()
    {
    }

    public NodeException(string message) : base(message)
    {
    }

    public NodeException(string message, System.Exception innerException) : base(message, innerException)
    {
    }
}