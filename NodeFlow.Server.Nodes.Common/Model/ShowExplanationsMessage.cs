namespace NodeSharp.Nodes.Common.Model;

public class ShowExplanationsMessage
{
    public BaseNode Node { get; }

    public ShowExplanationsMessage(BaseNode node)
    {
        Node = node;
    }
}
