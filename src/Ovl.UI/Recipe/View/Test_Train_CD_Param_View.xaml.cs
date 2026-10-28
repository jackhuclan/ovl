using InteVega.CBB.StageControlModule.Views;
using Marvel.Interfaces.Application;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Prism.Ioc;

using Ovl.UI.Recipe.ViewModel;
namespace Ovl.UI.Recipe.View
{
    /// <summary>
    /// Test_Train_CD_Param_View.xaml 的交互逻辑
    /// </summary>
    public partial class Test_Train_CD_Param_View : UserControl
    {
        IServiceProvider _serviceProvider;
        public Test_Train_CD_Param_View()
        {
            InitializeComponent();            
        }

    }
}
