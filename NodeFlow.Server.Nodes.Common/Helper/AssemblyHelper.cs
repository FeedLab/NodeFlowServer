using System.Reflection;

namespace NodeFlow.Server.Nodes.Common.Helper;

public class AssemblyHelper
{
    /// <summary>
    /// Finds all implementations of the specified interface in all assemblies (*.dll)
    /// found in the specified folder and its subdirectories.
    /// </summary>
    /// <typeparam name="TInterface">The interface type to search for</typeparam>
    /// <param name="folder">Root folder to start searching from</param>
    /// <param name="assemblyFilter">Optional filter for assembly names (e.g., "NodeFlow.Server.Nodes")</param>
    /// <returns>All types that implement the interface</returns>
    public static IEnumerable<Type> FindImplementations<TInterface>(string folder, string? assemblyFilter = null)
    {
        var dlls = Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories);
        var interfaceType = typeof(TInterface);

        // Apply filter if specified
        if (!string.IsNullOrEmpty(assemblyFilter))
        {
            dlls = dlls.Where(dll => Path.GetFileName(dll).Contains(assemblyFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        foreach (var dll in dlls)
        {
            Assembly asm;
            try
            {
                asm = Assembly.LoadFrom(dll);
            }
            catch (System.Exception ex)
            {
                // Log or handle assembly load failures
                Console.WriteLine($"[AssemblyHelper] Failed to load assembly {dll}: {ex.Message}");
                continue;
            }

            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Handle partial assembly load
                types = ex.Types.Where(t => t != null).ToArray()!;
            }

            foreach (var type in types)
            {
                if (interfaceType.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
                {
                    yield return type;
                }
            }
        }
    }

    public static IEnumerable<Type> FindImplementations<TInterface>(IEnumerable<Type> types)
    {
        var interfaceType = typeof(TInterface);

        foreach (var type in types)
        {
            if (interfaceType.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
                yield return type;
        }
    }

    public static IEnumerable<TInterface> FindImplementationsAndCreateInstance<TInterface>(IEnumerable<Type> types)
    {
        var interfaceType = typeof(TInterface);

        foreach (var type in types)
        {
            if (interfaceType.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
            {
                var instance = Activator.CreateInstance(type);
                if (instance != null)
                    yield return (TInterface)instance;
            }
        }
    }

    public static TInterface FindImplementationsAndCreateInstance<TInterface>(Assembly assembly)
    {
        var interfaceType = typeof(TInterface);

        var types = assembly.GetTypes();

        foreach (var type in types)
        {
            if (interfaceType.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
            {
                var instance = Activator.CreateInstance(type);
                if (instance != null)
                    return (TInterface)instance;
            }
        }

        throw new ArgumentException("Type does not implement the interface or is not a class");
    }
}