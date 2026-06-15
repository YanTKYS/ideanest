using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using IdeaNest.ViewModels;

namespace IdeaNest.Views;

/// <summary>
/// IdeaNest-local verification host. It intentionally supplies no AppShell
/// commands and performs no file save or auto-save.
/// </summary>
public partial class WorkspaceHostPreviewWindow : Window, INotifyPropertyChanged
{
    private string _dirtyStatus = "変更なし";

    public IdeaNestWorkspaceViewModel Workspace { get; } = new();

    public string DirtyStatus
    {
        get => _dirtyStatus;
        private set
        {
            if (_dirtyStatus == value) return;
            _dirtyStatus = value;
            OnPropertyChanged();
        }
    }

    public string CardStatus => $"Cards: {Workspace.TotalCount} / Visible: {Workspace.VisibleCardCount}";
    public string FilterStatus => Workspace.Filter.HasActiveFilter ? "Filter: Active" : "Filter: None";

    public WorkspaceHostPreviewWindow()
    {
        InitializeComponent();
        DataContext = this;
        Workspace.DirtyRequested += (_, _) => DirtyStatus = "未保存の変更あり（検証用・保存なし）";
        Workspace.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(IdeaNestWorkspaceViewModel.TotalCount)
                               or nameof(IdeaNestWorkspaceViewModel.VisibleCardCount))
            {
                OnPropertyChanged(nameof(CardStatus));
            }
            if (e.PropertyName == nameof(IdeaNestWorkspaceViewModel.CurrentFilterContext))
            {
                OnPropertyChanged(nameof(FilterStatus));
            }
        };
        Loaded += (_, _) => WorkspaceView.FocusWorkspace();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
