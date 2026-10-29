using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Ovl.UI.Bases
{
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
        public Visibility _visible;
    }
}
