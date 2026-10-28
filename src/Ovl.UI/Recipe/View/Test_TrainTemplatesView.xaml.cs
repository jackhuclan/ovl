using HPImageViewer.Core;
using InteVega.CBB.TemplateMatchingModule.Views;
using System.Windows;
using System.Windows.Controls;

using Ovl.UI.ViewModels.Controls;
namespace Ovl.UI.Recipe.View
{
    /// <summary>
    /// Test_TrainTemplatesView.xaml 的交互逻辑
    /// </summary>
    public partial class Test_TrainTemplatesView : UserControl
    {
        public static readonly DependencyProperty ImageViewerProperty =
            DependencyProperty.Register(
                "ImageViewer",
                typeof(IHPImageViewer),
                typeof(Test_TrainTemplatesView),
                new PropertyMetadata(null));

        public IHPImageViewer ImageViewer
        {
            get { return (IHPImageViewer)GetValue(ImageViewerProperty); }
            set { SetValue(ImageViewerProperty, value); }
        }

        public Test_TrainTemplatesView()
        {
            InitializeComponent();
        }

        public SingleTemplateControl InnerTemplateControl => innerModelTemplateControl;
        public SingleTemplateControl OuterTemplateControl => outerModelTemplateControl;
        public PositioningModelControl PositioningControl => positioningModelControl;
    }
}
