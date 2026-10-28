using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ovl.UI.ViewModels.Interface;
namespace Ovl.UI.ViewModels.Recipe.ViewModel
{
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
}
