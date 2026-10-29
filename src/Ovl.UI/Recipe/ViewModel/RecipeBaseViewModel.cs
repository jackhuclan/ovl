using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ovl.UI.Recipe.ViewModel;

public partial class RecipeBaseViewModel : ObservableObject
{
    [ObservableProperty]
    string _id;
    [ObservableProperty]
    string _recipeName;
    [ObservableProperty]
    DateTime _createDate;
    [ObservableProperty]
    string _createUser;
    [ObservableProperty]
    DateTime _updateDate;
    [ObservableProperty]
    string _updateUser;
    public RecipeBaseViewModel()
    {

    }
}
