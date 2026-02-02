using NodeSharp.Nodes.Common.Configuration;
using NodeSharp.Nodes.Common.Services;

namespace NodeSharp.Nodes.Common;
using Options = Microsoft.Extensions.Options.Options;

public static class Startup
{
    public static void Register(IServiceCollection services, NodeSharpSettings settings)
    {
        services.AddSingleton(settings);
        services.AddSingleton(Options.Create(settings.Grid));
        services.AddSingleton(Options.Create(settings.Directories));
        services.AddSingleton(Options.Create(settings.ExplanationsPopup));
        services.AddSingleton(Options.Create(settings.KeyValueStore));
        services.AddSingleton(Options.Create(settings.PersistToDisk));
        services.AddSingleton<KeyValueStore>();
    }
}
