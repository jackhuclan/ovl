using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ovl.UI.Bases;

public partial class MenuNode : ObservableObject
{

    public string Id { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public string RecipeId { get; set; }
    public int Index { get; set; }
    [ObservableProperty]
    private bool _isSelect;
    [ObservableProperty]
    private Visibility _visible;
}
