namespace IdeaNest.ViewModels;

/// <summary>
/// IdeaNest standalone application shell. During the first separation stage it
/// derives from the workspace view model to preserve existing bindings while
/// exposing the reusable workspace explicitly.
/// </summary>
public class MainViewModel : IdeaNestWorkspaceViewModel
{
    public IdeaNestWorkspaceViewModel Workspace => this;
}
