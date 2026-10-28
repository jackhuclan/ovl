using Marvel.Application40;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.CD;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.DataModels.Recipe40;
using Marvel.DataModels.Recipe40.Rules;
using System.Windows;
using System.Windows.Controls;

using Ovl.UI.Recipe.ViewModel;
namespace Ovl.UI.Recipe.View
{
    /// <summary>
    /// Interaction logic for AddTestView_OM40.xaml
    /// </summary>
    public partial class AddTestView_OM40 : UserControl
    {
        private bool _isUpdating = false;

        public AddTestView_OM40()
        {
            InitializeComponent();
            this.Loaded += AddTestView40_Loaded;
        }

        private void AddTestView40_Loaded(object sender, RoutedEventArgs e)
        {
            // Attach event handlers
            cbFaceMode.SelectionChanged += cbFaceMode_SelectionChanged;
            cbTestType.SelectionChanged += cbTestType_SelectionChanged;
            cbSubType.SelectionChanged += cbSubType_SelectionChanged;
            cbInnerType.SelectionChanged += cbInnerType_SelectionChanged;
            cbOuterType.SelectionChanged += cbOuterType_SelectionChanged;

            // Initialize FaceMode ComboBox
            cbFaceMode.Items.Add(EnumFaceMode.SingleFace);
            cbFaceMode.Items.Add(EnumFaceMode.DoubleFace);
            cbFaceMode.SelectedIndex = 0; // Default to SingleFace

            // Initialize TestType ComboBox for SingleFace
            cbTestType.Items.Add("Overlay");
            cbTestType.Items.Add("CD");
            cbTestType.SelectedIndex = 0;

            // Initialize SubType for Overlay by default
            UpdateSubTypeOptionsForOverlay();

            // Initialize InnerType and OuterType with EnumTargetType options
            InitializeTargetTypeComboBox(cbInnerType);
            InitializeTargetTypeComboBox(cbOuterType);

            // Update UI state based on initial selection
            UpdateUIForFaceMode();
            UpdateUIForTestType();
            CreateTest();
        }

        private void InitializeTargetTypeComboBox(ComboBox comboBox)
        {
            comboBox.Items.Clear();
            comboBox.Items.Add(EnumTargetType.Bar);
            comboBox.Items.Add(EnumTargetType.Box);
            comboBox.Items.Add(EnumTargetType.Frame);
            comboBox.Items.Add(EnumTargetType.Circle);
            comboBox.Items.Add(EnumTargetType.Cross);
            comboBox.Items.Add(EnumTargetType.LAngle);
            comboBox.Items.Add(EnumTargetType.LCV);
            comboBox.SelectedIndex = 0;
        }

        private void UpdateSubTypeOptionsForOverlay()
        {
            cbSubType.Items.Clear();
            cbSubType.Items.Add(EnumTargetType.Bar);
            cbSubType.Items.Add(EnumTargetType.Box);
            cbSubType.Items.Add(EnumTargetType.Frame);
            cbSubType.Items.Add(EnumTargetType.Circle);
            cbSubType.Items.Add(EnumTargetType.Cross);
            cbSubType.Items.Add(EnumTargetType.LAngle);
            cbSubType.Items.Add(EnumTargetType.LCV);
            cbSubType.SelectedIndex = 0;
        }

        private void UpdateSubTypeOptionsForCD()
        {
            cbSubType.Items.Clear();
            cbSubType.Items.Add(EnumTargetType.LS );
            //cbSubType.Items.Add(EnumTargetType.Aperture);
            cbSubType.SelectedIndex = 0;
        }

        private void cbFaceMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _isUpdating) return;
            _isUpdating = true;

            try
            {
                UpdateUIForFaceMode();
                UpdateUIForTestType();
                CreateTest();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void UpdateUIForFaceMode()
        {
            var viewModel = DataContext as AddTestViewModel40;
            if (viewModel == null) return;

            EnumFaceMode selectedFaceMode = (EnumFaceMode)cbFaceMode.SelectedItem;
            viewModel.IsSingleFace = selectedFaceMode == EnumFaceMode.SingleFace;
            viewModel.IsDoubleFace = selectedFaceMode == EnumFaceMode.DoubleFace;
        }

        private void cbTestType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _isUpdating) return;
            _isUpdating = true;

            try
            {
                UpdateUIForTestType();
                CreateTest();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void UpdateUIForTestType()
        {
            if (cbFaceMode.SelectedItem == null) return;
            
            var viewModel = DataContext as AddTestViewModel40;
            if (viewModel == null || cbTestType.SelectedItem == null) return;

            string selectedTestType = cbTestType.SelectedItem.ToString();
            viewModel.ShowSubType = viewModel.IsSingleFace;

            if (selectedTestType == "Overlay")
            {
                UpdateSubTypeOptionsForOverlay();
            }
            else if (selectedTestType == "CD")
            {
                UpdateSubTypeOptionsForCD();
            }
        }

        private void cbSubType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || cbSubType.Items.Count == 0 || _isUpdating) return;
            CreateTest();
        }

        private void cbInnerType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || cbInnerType.Items.Count == 0 || _isUpdating) return;
            _isUpdating = true;

            try
            {
                NormalizeDoubleFaceSelection(changedSideIsInner: true);
                CreateTest();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void cbOuterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || cbOuterType.Items.Count == 0 || _isUpdating) return;
            _isUpdating = true;

            try
            {
                NormalizeDoubleFaceSelection(changedSideIsInner: false);
                CreateTest();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// 规范化双面模式下的内外类型选择，避免出现 LAngle 不一致的非法组合。
        /// </summary>
        private void NormalizeDoubleFaceSelection(bool changedSideIsInner)
        {
            if (cbFaceMode.SelectedItem is not EnumFaceMode faceMode || faceMode != EnumFaceMode.DoubleFace)
            {
                return;
            }

            if (cbInnerType.SelectedItem is not EnumTargetType innerType ||
                cbOuterType.SelectedItem is not EnumTargetType outerType)
            {
                return;
            }

            var normalized = AddTestTypeConsistencyRule.NormalizeDoubleFaceSelection(
                innerType,
                outerType,
                changedSideIsInner);

            cbInnerType.SelectedItem = normalized.innerType;
            cbOuterType.SelectedItem = normalized.outerType;
        }

        private void CreateTest()
        {
            var viewModel = DataContext as AddTestViewModel40;
            if (viewModel == null || cbFaceMode.SelectedItem == null) return;

            EnumFaceMode faceMode = (EnumFaceMode)cbFaceMode.SelectedItem;
            EnumTargetType innerType;
            EnumTargetType outerType;
            if (faceMode == EnumFaceMode.SingleFace)
            {
                if (cbTestType.SelectedItem == null) return;
                string testType = cbTestType.SelectedItem.ToString();

                switch (testType)
                {
                    case "Overlay":
                        if (cbSubType.SelectedItem == null) return;
                        innerType = (EnumTargetType)cbSubType.SelectedItem;
                        viewModel.Test = Test40Factory.Build40(innerType, innerType, EnumFaceMode.SingleFace, viewModel.Test.Name);
                        break;
                    case "CD":
                        if (cbSubType.SelectedItem == null) return;
                        innerType = (EnumTargetType)cbSubType.SelectedItem;
                        viewModel.Test = Test40Factory.BuildCD40(innerType, viewModel.Test.Name);
                        break;
                }
                viewModel.Test.Name = viewModel.Test.Name;
            }
            else // DoubleFace
            {
                if (cbInnerType.SelectedItem == null || cbOuterType.SelectedItem == null) return;
                NormalizeDoubleFaceSelection(changedSideIsInner: true);
                innerType = (EnumTargetType)cbInnerType.SelectedItem;
                outerType = (EnumTargetType)cbOuterType.SelectedItem;
                viewModel.Test = Test40Factory.Build40(innerType, outerType, EnumFaceMode.DoubleFace, viewModel.Test.Name);
            }

            // Update the icon control
            if (viewModel.Test != null)
            {
                var defaultTest = (DataContext as AddTestViewModel).Test;
                (viewModel.Test).Name = defaultTest.Name;
                (DataContext as AddTestViewModel40).Test = viewModel.Test;
                if (viewModel.Test is TestInfo_Overlay)
                {
                    cti.DataContext = viewModel.Test.TrainSetting;
                }
                else if (viewModel.Test is TestInfo_CD testCD)
                {
                    TrainSettings_Overlay trainSettings_Overlay = new TrainSettings_BiB();
                    trainSettings_Overlay.InnerType = testCD.TrainSetting_CD.Type;
                    trainSettings_Overlay.OuterType = testCD.TrainSetting_CD.Type;
                    cti.DataContext = trainSettings_Overlay;
                }
            }
        }
    }
}
