using System.Windows;
using System.Windows.Controls;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.CD;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.DataModels.Recipe.TestInfo;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// AddTestView_Default.xaml 的交互逻辑
/// </summary>
public partial class AddTestView_Default : UserControl
{
    public AddTestView_Default()
    {
        InitializeComponent();

        this.Loaded += AddTestView_Loaded;
    }

    private void AddTestView_Loaded(object sender, RoutedEventArgs e)
    {
        cbType.Items.Add("BiB");
        cbType.Items.Add("Circle in Circle");
        cbType.Items.Add("Cross in Cross");
        cbType.Items.Add("AIM");
        cbType.Items.Add("CD");
        cbType.SelectedIndex = 0;
        CreateTest();
    }

    private void cbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        cbSubType.Items.Clear();
        cbSubType.Visibility = Visibility.Collapsed;
        switch (cbType.SelectedIndex)
        {
            case 0:
                cbSubType.Items.Add(EnumTargetType.Bar);
                cbSubType.Items.Add(EnumTargetType.Box);
                cbSubType.Items.Add(EnumTargetType.Frame);
                break;
            case 1:
                cbSubType.Items.Add(EnumTargetType.Circle);
                break;
            case 2:
                cbSubType.Items.Add(EnumTargetType.Cross);
                break;
            case 3:
                cbSubType.Items.Add(EnumTargetType.AIM);
                if (MarvelVersion.CurrentVersion >= EnumOMVersion.OM300)
                {
                    cbSubType.Items.Add("XYS");
                }
                break;
            case 4:
                cbSubType.Items.Add(EnumTargetType.LS);
                //cbSubType.Items.Add(EnumTargetType.Aperture);
                //cbSubType.Items.Add(EnumTargetType.DS);
                break;
        }
        var vm = DataContext as AddTestViewModel;
        cbSubType.Visibility = !vm.IsRename && cbSubType.Items.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        if (vm.IsRename)
        { cti.Visibility = Visibility.Collapsed; }
        cbSubType.SelectedIndex = 0;
    }

    private void cbSubType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cbSubType.Items.Count == 0 ||!IsLoaded )
            return;
        CreateTest();
    }

    private void CreateTest()
    {
        EnumTargetType type;
        if (!Enum.TryParse($"AIM_{cbSubType.SelectedItem}", out type))
        {
            type = (EnumTargetType)cbSubType.SelectedItem;
        }
        var testInfo = TestInfoFactory.Build(type);

        if (testInfo != null)
        {
            var defaultTest = (DataContext as AddTestViewModel).Test;
            (testInfo).Name = defaultTest.Name;
            (DataContext as AddTestViewModel).Test = testInfo;
            if (testInfo is TestInfo_Overlay)
            {
                cti.DataContext = testInfo.TrainSetting;
            }
            else if (testInfo is TestInfo_CD testCD)
            {
                TrainSettings_Overlay trainSettings_Overlay = new TrainSettings_BiB();
                trainSettings_Overlay.InnerType = testCD.TrainSetting_CD.Type;
                trainSettings_Overlay.OuterType = testCD.TrainSetting_CD.Type;
                cti.DataContext = trainSettings_Overlay;
            }
        }
    }
}
