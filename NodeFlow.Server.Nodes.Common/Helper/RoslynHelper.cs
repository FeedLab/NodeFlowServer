using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using NodeSharp.Nodes.Common.Services;

namespace NodeSharp.Nodes.Common.Helper;

public class RoslynHelper(string codeTemplate, string sourceCode)
{
    private CSharpCompilation? compilation;
    private string? code;

    public CSharpCompilation CompileScript()
    {
        code = codeTemplate.Replace("##@@##", sourceCode);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        // Collect core references
        var references = new[]
        {
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.CoreLib.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "netstandard.dll")),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Dynamic.ExpandoObject).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JsonConvert).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(KeyValueStore).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Options.IOptions<>).Assembly.Location)
        };

        compilation = CSharpCompilation.Create(
            "RunnerAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        return compilation;
    }

    public List<string> GetDiagnostics()
    {
        if (compilation is null)
        {
            throw new InvalidOperationException("Roslyn compilation is not initialized.");
        }

        HasCompilerError = false;
        HasCompilerWarning = false;

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        var diagnostics = new List<string>();
        if (!result.Success)
        {
            // Diagnostics contain errors and warnings
            foreach (var diagnostic in result.Diagnostics
                         .Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                diagnostics.Add($"Error: {diagnostic.Id} - {diagnostic.GetMessage()}");
                HasCompilerError = true;
            }

            foreach (var diagnostic in result.Diagnostics
                         .Where(d => d.Severity == DiagnosticSeverity.Warning))
            {
                diagnostics.Add($"Warning: {diagnostic.Id} - {diagnostic.GetMessage()}");
                HasCompilerWarning = true;
            }
        }
        else
        {
            diagnostics.Add("Compilation succeeded!");
        }

        return diagnostics;
    }


    public MethodInfo? GetExecutionMethod()
    {
        if (compilation is null)
        {
            throw new InvalidOperationException("Roslyn compilation is not initialized.");
        }

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var diag in result.Diagnostics)
                Console.WriteLine(diag);

            Console.WriteLine($"Compilation failed: \n\r{code}");

            throw new InvalidOperationException("Roslyn compilation failed.");
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var type = assembly.GetType("Runner");
        var method = type?.GetMethod("Execute");

        return method;
    }

    public bool HasCompilerWarning { get; set; }

    public bool HasCompilerError { get; set; }
}