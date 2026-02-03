using NodeFlow.Server.Nodes.Common.Model;

namespace NodeFlow.Server.Nodes.Inject;



public class NodePresentationInformation : INodePresentationInformation
{
    public string OverviewText { get; init; } = "The Inject node can initiate a flow with a specific payload value";
    public string FontFamilyName { get; init; } = "FontSolid";
    public string Symbol { get; init; } = "\uf70c";
}

public class NodeInformation : INodeInformation
{
    private const string DefaultTypeId = "Inject";
    private const string DefaultRuntimeType = "NodeInject";

    public string TypeId => DefaultTypeId;
    public string DisplayName => "Inject";
    public string RuntimeType => DefaultRuntimeType;
    public string Group => "Core";
    public bool ActivateOnStart => true;
    public bool IsEnabled => true;
    public int NumberOfInputs => 0;
    public int NumberOfOutputs => 1;

    public bool HasOverviewText => !string.IsNullOrWhiteSpace(PresentationInformation.OverviewText);
    public INodePresentationInformation PresentationInformation { get; init; } = new NodePresentationInformation();

    public override string ToString() =>
        $"""
         {nameof(TypeId)}: {TypeId}
         {nameof(ActivateOnStart)}: {ActivateOnStart}
         {nameof(IsEnabled)}: {IsEnabled}
         {nameof(PresentationInformation.Symbol)}: {PresentationInformation.Symbol}
         {nameof(HasOverviewText)}: {HasOverviewText}
         """;
}


