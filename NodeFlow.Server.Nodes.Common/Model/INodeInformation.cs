namespace NodeSharp.Nodes.Common.Model;

public interface INodeInformation
{
    string TypeId { get; }
    string DisplayName { get; }
    string RuntimeType { get; }
    string Group { get; }
    Color Background { get; }
    bool ActivateOnStart { get; }
    bool IsEnabled { get; }
    int NumberOfInputs { get; }
    int NumberOfOutputs { get; }
    bool HasOverviewText { get; }
    INodePresentationInformation PresentationInformation { get; init; }
}

public interface INodePresentationInformation
{
    string OverviewText { get; init; }
    string FontFamilyName { get; init; }
    string Symbol { get; init; }
}

