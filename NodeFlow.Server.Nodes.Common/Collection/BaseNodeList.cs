using System.Collections.ObjectModel;
using NodeFlow.Server.Nodes.Common.Helper;

namespace NodeFlow.Server.Nodes.Common.Collection;

public class BaseNodeList : ObservableCollection<BaseNode>
{
    public BaseNodeList()
    {
        FileLogger.LogNodeSubscription(this);
    }

    public Dictionary<string, BaseNode> ToDictionary()
    {
        return this.ToDictionary(n => n.Id);
    }
    
    public void ValidateInputAndOutputNodes()
    {
        foreach (var node in this)
        {
    //        node.ValidateInputAndOutput();
        }
    }

    public Task Run()
    {
        foreach (var candidate in this)
        {
            candidate.Reset();
        }
        
        foreach (var candidate in this)
        {
            if (!ShouldRunOnStart(candidate))
            {
                continue;
            }

            _ = candidate.Run();
        }

        return Task.CompletedTask;
    }

    private static bool ShouldRunOnStart(BaseNode candidate) =>
        candidate is { ActivateOnStart: true, IsEnabled: true };

   
    public BaseNode? Find(Func<BaseNode, bool> predicate)
    {
        return this.FirstOrDefault(predicate);
    }
}