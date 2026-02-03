using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodeFlow.Server.Nodes.Common.Configuration;
using NodeFlow.Server.Nodes.Common.Services;

namespace NodeFlow.Server.Nodes.Common;

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
