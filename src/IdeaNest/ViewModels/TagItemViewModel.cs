namespace IdeaNest.ViewModels;

public class TagItemViewModel : ViewModelBase
{
    private string _name;
    private int _count;

    public TagItemViewModel(string name, int count)
    {
        _name = name;
        _count = count;
    }

    public string Name  { get => _name;  set { if (SetField(ref _name, value))  { OnPropertyChanged(nameof(DisplayName)); } } }
    public int    Count { get => _count; set { if (SetField(ref _count, value)) { OnPropertyChanged(nameof(CountText));  } } }

    public string DisplayName => $"#{Name}";
    public string CountText   => $"{Count}件";
}
