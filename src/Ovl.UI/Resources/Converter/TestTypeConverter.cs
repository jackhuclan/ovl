using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.CD;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Ovl.UI.Recipe.ViewModel;
using Ovl.UI.ViewModels.Recipe.ViewModel;
namespace Ovl.UI.Resources.Converter
{
    internal class TestTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TestManageInfo tmi)
            {
                var test = tmi.TestInfo;
                return test.GetTestType();
                //switch (test)
                //{

                //    case TestInfo_CD:
                //        return ((TestInfo_CD)test).TrainSetting_CD.Type;
                //    case TestInfo_AIM_XYS testXYS:
                //        return $"{testXYS.TrainSetting_XYS.InnerType}";
                //    case TestInfo_AIM testAim:
                //        return $"{testAim.TrainSetting_BiB.InnerType}";
                //    case TestInfo_BiB testBib:
                //        return $"{testBib.TrainSetting_BiB.InnerType} in {testBib.TrainSetting_BiB.OuterType}";
                //}
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
