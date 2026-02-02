using System.Reflection;

namespace NodeSharp.Nodes.Common.Helper;

public class AssemblyHelper
{
    public static IEnumerable<Type> FindImplementations<TInterface>(string folder)
    {
        var dlls = Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories);
        var interfaceType = typeof(TInterface);

        var names = dlls.Select(Path.GetFileName).Where(filename => filename.StartsWith("NodeSharp.Nodes")).ToList();
        
        foreach (var dll in dlls)
        {
            Assembly asm;
            try
            {
                asm = Assembly.LoadFrom(dll);
            }
            catch
            {
                continue;
            }

            foreach (var type in asm.GetTypes())
            {
                if (interfaceType.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
                    yield return type;
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