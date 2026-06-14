using System.Windows.Input;

namespace IdeaNest.ViewModels;

/// <summary>Small IdeaNest-specific command set supplied by a workspace host.</summary>
public sealed class IdeaNestWorkspaceHostCommands
{
    public ICommand? NewWorkspace { get; init; }
    public ICommand? Open { get; init; }
    public ICommand? Save { get; init; }
    public ICommand? SaveAs { get; init; }
    public ICommand? ShowTutorial { get; init; }
    public ICommand? Exit { get; init; }
}
