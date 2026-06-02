namespace IdeaNest.ViewModels;

public class ColorFilterItemViewModel
{
    public string Name { get; }
    public string DisplayName { get; }

    public ColorFilterItemViewModel(string name, string displayName)
    {
        Name = name;
        DisplayName = displayName;
    }
}
