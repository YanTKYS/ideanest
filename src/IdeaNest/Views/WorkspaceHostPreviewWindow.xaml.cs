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

    public WorkspaceHostPreviewWindow()
    {
        InitializeComponent();
        DataContext = this;
        Workspace.DirtyRequested += (_, _) => DirtyStatus = "未保存の変更あり（検証用・保存なし）";
        Loaded += (_, _) => WorkspaceView.FocusWorkspace();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
