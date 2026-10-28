using CommunityToolkit.Mvvm.ComponentModel;
using Marvel.Application40;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe40;

namespace Ovl.UI.Recipe.ViewModel
{
    internal partial class AddTestViewModel40 : AddTestViewModel
    {
        
        public AddTestViewModel40(string defaultName) : base(defaultName)
        {
            Test = Test40Factory.Build40(EnumTargetType.Bar, EnumTargetType.Bar, EnumFaceMode.SingleFace, defaultName);
            // Initialize properties
            IsSingleFace = true;
            IsDoubleFace = false;
            ShowSubType = true;
        }

        [ObservableProperty]
        private bool _isSingleFace;

        [ObservableProperty]
        private bool _isDoubleFace;

        [ObservableProperty]
        private bool _showSubType;
    }
}
