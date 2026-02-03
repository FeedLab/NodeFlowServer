using System.Collections.Specialized;
using System.Text;
using NodeFlow.Server.Nodes.Common.Collection;

namespace NodeFlow.Server.Nodes.Common.Helper;

public static class FileLogger
{
    private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "app_log.txt");

    private static readonly
        Dictionary<BaseNode, EventHandler<(BaseNode baseNode, string level, string message, string entry)>> Handlers =
            new();

    private static readonly StringBuilder MemoryLog = new();

    static FileLogger()
    {
    }

    // Subscription: Event that others can subscribe to
    public static void LogNodeSubscription(BaseNodeList nodes)
    {
        nodes.CollectionChanged += async (s, e) =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (BaseNode addNode in e.NewItems)
                        {
                            EventHandler<(BaseNode baseNode, string level, string message, string entry)> handler =
                                (sender, tuple) =>
                                {
                                    // Fire and forget with error handling to avoid async void crashes
                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await Log($"{tuple.level}: {tuple.baseNode.Name}", tuple.message);
                                        }
                                        catch (System.Exception ex)
                                        {
                                            // Log the exception to a fallback location
                                            System.Diagnostics.Debug.WriteLine($"Logging failed: {ex.Message}");
                                        }
                                    });
                                };
                            Handlers[addNode] = handler;
                            addNode.OnExitNodeMessage += handler;
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (BaseNode deleteNode in e.OldItems)
                        {
                            if (Handlers.Remove(deleteNode, out var handler))
                            {
                                deleteNode.OnExitNodeMessage -= handler;
                            }
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    if (e.OldItems != null)
                    {
                        foreach (BaseNode deleteNode in e.OldItems)
                        {
                            if (Handlers.Remove(deleteNode, out var handler))
                            {
                                deleteNode.OnExitNodeMessage -= handler;
                            }
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };
    }

    public static event EventHandler<(string level, string message, string entry)>? OnLogAdded;

    private static async Task Log(string level, string message)
    {
        string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level.ToUpper()}] {message}{Environment.NewLine}";

        System.Diagnostics.Debug.WriteLine(entry);

        lock (MemoryLog)
        {
            MemoryLog.Append(entry);
        }

        // 3. Notify Subscribers (Real-time updates)
        OnLogAdded?.Invoke(null, (level, message, entry));

        // 4. Save to Disk
        try
        {
            await File.AppendAllTextAsync(LogPath, entry);
        }
        catch
        {
            /* Handle IO errors */
        }
    }

    // Helper to get the full log from memory
    public static string GetMemoryLog()
    {
        lock (MemoryLog) return MemoryLog.ToString();
    }

    public static Task Output(string msg) => Log("Output", msg);
    public static Task Trace(string msg) => Log("TRACE", msg);
    public static Task Debug(string msg) => Log("DEBUG", msg);
    public static Task Information(string msg) => Log("Information", msg);
    public static Task Warning(string msg) => Log("WARN", msg);
    public static Task Error(string msg) => Log("ERROR", msg);
}