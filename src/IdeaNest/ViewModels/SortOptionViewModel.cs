namespace IdeaNest.ViewModels;

public class SortOptionViewModel
{
    public string Value { get; }
    public string DisplayName { get; }

    public SortOptionViewModel(string value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }
}
