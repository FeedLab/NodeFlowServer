using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using NodeFlow.Server.Nodes.Common;
using NodeFlow.Server.Nodes.Common.Collection;
using NodeFlow.Server.Nodes.Common.Exception;
using NodeFlow.Server.Nodes.Common.Extension;
using NodeFlow.Server.Nodes.Common.Helper;
using NodeFlow.Server.Nodes.Common.Model;

namespace NodeFlow.Server.Nodes.Inject;

public class NodeInject : BaseNode
{
    public ActivateAfter ActivateAfter { get; }
    public Repeat Repeat { get; }
    public List<Parameter> Parameters { get; }

    public NodeInject(
        BaseNodeList nodes,
        string id,
        string typeId,
        string name,
        bool isEnabled,
        bool activateOnStart,
        int xPosition,
        int yPosition,
        Storage storage)
        : base(
            nodes,
            id,
            typeId,
            name,
            isEnabled,
            activateOnStart,
            xPosition,
            yPosition,
            storage
        )
    {
        Repeat = new Repeat("Second", 10);
        ActivateAfter = new ActivateAfter("Second", 1);
        Parameters = [new Parameter("Timestamp", "Number", "Timestamp", "")];


        var statusBodyHeight = InitializeDimensions();

        Inputs.Clear();
        Outputs.Clear();

        Outputs.Add(new Output(Guid.CreateVersion7(), "Output", [],
            new Point(0, 0 / 2)));

        OutputMessage = "OK";
    }

    private double InitializeDimensions()
    {
        const double height = 70;
        const double width = 240;
        const double statusBodyHeight = 12;
        const double anchorWidth = 60;

        return statusBodyHeight;
    }


    public NodeInject(
        BaseNodeList nodes,
        string id,
        string typeId,
        string name,
        bool isEnabled,
        bool activateOnStart,
        int xPosition,
        int yPosition,
        List<Output> outputs,
        List<Input> inputs,
        JsonElement nodeElement,
        Storage storage)
        : base(
            nodes,
            id,
            typeId,
            name,
            isEnabled,
            activateOnStart,
            xPosition,
            yPosition,
            outputs,
            inputs,
            storage
        )
    {
        try
        {
            InitializeDimensions();


            Repeat = new Repeat(
                nodeElement.GetProperty("Repeat").GetProperty("Type").GetString()!,
                nodeElement.GetProperty("Repeat").GetProperty("Value").GetInt32()
            );
        }
        catch (System.Exception e)
        {
            throw new NodeParseException(this, nameof(Repeat), e);
        }

        try
        {
            ActivateAfter = new ActivateAfter(
                nodeElement.GetProperty("ActivateAfter").GetProperty("Type").GetString()!,
                nodeElement.GetProperty("ActivateAfter").GetProperty("Value").GetInt32());
        }
        catch (System.Exception e)
        {
            throw new NodeParseException(this, nameof(ActivateAfter), e);
        }

        try
        {
            Parameters = nodeElement.GetProperty("Parameters").EnumerateArray()
                .Select(p =>
                {
                    var source = p.TryGetProperty("Source", out var sourceProp)
                        ? sourceProp.GetString() ?? "primitive"
                        : "primitive";

                    var declaredType = p.GetProperty("Type").GetString() ?? "string";
                    var effectiveType = string.Equals(source, "environment", StringComparison.OrdinalIgnoreCase)
                        ? "environment"
                        : declaredType;

                    return new Parameter(
                        p.GetProperty("Name").GetString() ?? "Unknown",
                        effectiveType,
                        source,
                        p.TryGetProperty("Value", out var val) ? (val.GetString() ?? "") : ""
                    );
                }).ToList();
        }
        catch (System.Exception e)
        {
            throw new NodeParseException(this, nameof(Parameters), e);
        }
        
        Parameters.Add(new Parameter("Timestamp", "Number", "Timestamp", ""));
    }

    public override void Reset()
    {
        base.Reset();
    }

    public override Task<string> Run()
    {
        var stopwatch = EnterNode(this);

        try
        {
            if (!ActivateOnStart)
            {
                return Task.FromResult(OutputMessage);
            }


            _ = Task.Run(async () => { await RunAsync(); });

            return Task.FromResult(OutputMessage);
        }
        finally
        {
            LeaveNode(this, stopwatch);
        }
    }

    private async Task RunAsync()
    {
        if (ActivateAfter.Value > 0)
        {
            Debug.WriteLine(
                $"Inject: Delay is enabled. Waiting {ActivateAfter.ActivateAfterMilliseconds} milliseconds before execute.");

            // await PeriodicExecutor.DelayedPeriodicExecution(
            //     delay: TimeSpan.FromSeconds(ActivateAfter.Value),
            //     interval: TimeSpan.FromMilliseconds(250),
            //     action: (percentComplete) => { BoxNodeStatus.Value = (decimal)percentComplete; },
            //     cancellationToken: Cts.Token
            // );
        }

        if (Repeat.IsEnabled)
        {
            Debug.WriteLine("Inject: Starting repeating");

            // await MainThread.InvokeOnMainThreadAsync(() => { BoxNodeStatus.Value = 0; });

            var repeatMs = Repeat.Type.ConvertTimeToMilliseconds(Repeat.Value);
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(repeatMs));

            try
            {
                do
                {
                    await base.Run();
                    var parametersJsonString = await BuildParametersJson(Parameters);

                    await SendToConnectedChildrenAsync(parametersJsonString);

                    // await PeriodicExecutor.DelayedPeriodicExecution(
                    //     delay: TimeSpan.FromSeconds(Repeat.Value),
                    //     interval: TimeSpan.FromMilliseconds(1000),
                    //     action: (percentComplete) => { BoxNodeStatus.Value = (decimal)percentComplete; },
                    //     cancellationToken: Cts.Token);
                } while (await timer.WaitForNextTickAsync(Cts.Token));
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Inject timer cancelled.");
            }
            finally
            {
                timer?.Dispose();
            }
        }
        else
        {
            Debug.WriteLine("Inject: Starting once.");
            await base.Run();
            var parametersJsonString = await BuildParametersJson(Parameters);

            var jsonNode = JsonNode.Parse(parametersJsonString) ?? "";

            // await MainThread.InvokeOnMainThreadAsync(() => { BoxNodeStatus.Value = 100; });

            await SendToConnectedChildrenAsync(jsonNode);
        }
    }


    private static async Task<string> BuildParametersJson(IList<Parameter> parameters)
    {
        var sb = new StringBuilder();
        sb.Append("{\"Parameters\": [");

        var isFirstItem = true;
        foreach (var parameter in parameters)
        {
            if (!isFirstItem)
            {
                sb.Append(',');
            }

            if (parameter.Source.Equals("primitive", StringComparison.OrdinalIgnoreCase))
            {
                await AppendParameterJson(sb, parameter);
            }
            else if (parameter.Source.Equals("timestamp", StringComparison.OrdinalIgnoreCase))
            {
                var param = new Parameter(parameter.Name, "number", "timestamp", DateTime.Now.Ticks.ToString());
                await AppendParameterJson(sb, param);
            }
            else if (parameter.Source.Equals("environment", StringComparison.OrdinalIgnoreCase))
            {
                var envValue = GetRequiredEnvironmentVariable(parameter.Value) ?? "";

                if (IsNumeric(envValue))
                {
                    var param = new Parameter(parameter.Name, "number", "primitive", envValue);
                    await AppendParameterJson(sb, param);
                }
                else
                {
                    var param = new Parameter(parameter.Name, "string", "primitive", envValue);
                    await AppendParameterJson(sb, param);
                }
            }
            else
            {
                throw new InvalidOperationException($"Invalid parameter source: {parameter.Source}");
            }

            isFirstItem = false;
        }

        sb.Append("]}");
        return sb.ToString();
    }

    private static bool IsNumeric(string? value)
    {
        return decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out _);
    }

    private static async Task AppendParameterJson(StringBuilder sb, Parameter parameter)
    {
        var name = parameter.Name;
        var type = parameter.Type.Trim().ToLowerInvariant();
        var source = parameter.Source.Trim().ToLowerInvariant();

        sb.Append('{');
        //       sb.Append($"\"name\": \"{name}\",");

        switch (type)
        {
            case "string":
                sb.Append($"\"{parameter.Name}\": \"{parameter.Value}\"");
                break;

            case "number":
                if (!decimal.TryParse(parameter.Value, NumberStyles.Number, CultureInfo.InvariantCulture,
                        out var number))
                {
                    var errorMessage =
                        $"Parameter '{name}' has invalid number value '{parameter.Value}'. Expected InvariantCulture numeric format.";

                    sb.Append($"\"{parameter.Name}\": {0.ToString(CultureInfo.InvariantCulture)}");

                    await FileLogger.Error(errorMessage);
                }
                else
                {
                    sb.Append($"\"{parameter.Name}\": {number.ToString(CultureInfo.InvariantCulture)}");
                }

                break;

            case "boolean":
                if (!bool.TryParse(parameter.Value, out var boolean))
                {
                    throw new InvalidOperationException(
                        $"Parameter '{name}' has invalid boolean value '{parameter.Value}'. Expected 'true' or 'false'.");
                }

                sb.Append($"\"{parameter.Name}\": {(boolean ? "true" : "false")}");
                break;

            case "environment":
                var envValue = GetRequiredEnvironmentVariable(parameter.Value);
                sb.Append($"\"{parameter.Name}\": \"{envValue}\"");
                break;

            default:
                throw new InvalidOperationException($"Invalid parameter type: {parameter.Type}");
        }

        sb.Append('}');
    }

    private static string GetRequiredEnvironmentVariable(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required environment variable '{variableName}' is not set (or is empty).");
        }

        return value;
    }

    public string NodeName { get; } = "Inject";
}

public class ActivateAfter
{
    public string Type { get; set; }
    public int Value { get; set; }

    [JsonIgnore] public int ActivateAfterMilliseconds { get; }

    public ActivateAfter(string type, int value)
    {
        Type = type;
        Value = value;

        ActivateAfterMilliseconds = Type.ConvertTimeToMilliseconds(Value);
    }
}

public class Repeat
{
    public string Type { get; set; }

    public int Value { get; set; }

    public bool IsEnabled => Value > 0;

    public int RepeatAfterMilliseconds { get; }

    public Repeat(string type, int value)
    {
        Type = type;
        Value = value;

        RepeatAfterMilliseconds = Type.ConvertTimeToMilliseconds(Value);
    }
}

public class Parameter
{
    public string Name { get; }
    public string Type { get; }
    public string Source { get; }
    public string Value { get; }

    public Parameter(string name, string type, string source, string value)
    {
        Name = name;
        Type = type;
        Source = source;
        Value = value;
    }
}