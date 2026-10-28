using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.Recipe.Ui.Model
{
    public partial class RecipeDetail:ObservableObject
    {
        [ObservableProperty]
        public string _id;
        [ObservableProperty]
        public int _num;
        [ObservableProperty]
        public string _recipeId;
        [ObservableProperty]
        public string _recipeName;
        [ObservableProperty]
        public string _name;
        [ObservableProperty]
        public string _description;
        [ObservableProperty]
        public DateTime _createDate;
        [ObservableProperty]
        public string _createUser;
        [ObservableProperty]
        public DateTime _updateDate;
        [ObservableProperty]
        public string _updateUser;
        [ObservableProperty]
        public string _version;

    }
}
