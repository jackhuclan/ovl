using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;
using System.Windows.Controls;

namespace Ovl.UI.Recipe.ViewModel
{
    public partial class GoPointTestViewModel: ObservableObject,IDialogAware
    {
 
        public GoPointTestViewModel(IServiceProvider serviceProvider)
        {
 

            WaferMapCanvas = new Canvas();
            ////wafer中心stage坐标+fieldsize+"field参考点位置及stage坐标"
            //WaferMapStageControl.Wafer wafer = new WaferMapStageControl.Wafer(0, 100, 31.3625, 23.6525, 0, 0, 7);
            //Point waferCenterStage = new Point(-31.6466, 13.0369);
            //Point Field00InStage = new Point() { X =-34.2354, Y = 2.9577 };
            //WaferMapStageControl.WaferMap wafermap = new WaferMapStageControl.WaferMap(waferCenterStage,Field00InStage, wafer);
            //wafermap.scale = 2;
            //List<ThreeCoorList> threeCoorLists = wafermap.DrawingWaferMap(WaferMapCanvas, WaferMapStageControl.FieldReference.LBC);
            //wafermap.ClickedFieldInfos += Wafermap_ClickedFieldInfos;
        }
        [ObservableProperty]
        private Canvas waferMapCanvas;

        public event Action<IDialogResult> RequestClose;

        //public DataType Title = "走点测试";

        string IDialogAware.Title => "走点测试";

        //private void Wafermap_ClickedFieldInfos(ThreeCoorList obj)
        //{
        //    _MotionControl.Move(new AxisType[] { AxisType.X, AxisType.Y },
        //        new double[] { obj.StageCoor.LLC.X,obj.StageCoor.LLC.Y},false);
        //    //MessageBox.Show($"{obj.RowColNum.X},{obj.RowColNum.Y}\r\n" +
        //    //    $"ref:{obj.FieldReference}\r\n" +
        //    //    $"wafermap:ltc:{obj.DrawWaferMapCoor.LTC} rtc:{obj.DrawWaferMapCoor.RTC}llc: {obj.DrawWaferMapCoor.LLC} rlc:{obj.DrawWaferMapCoor.RLC}\r\n" +
        //    //    $"stagemap: ltc{obj.StageCoor.LTC} rtc:{obj.StageCoor.RTC}llc: {obj.StageCoor.LLC} rlc:{obj.StageCoor.RLC}\r\n", $"field{obj.RowColNum.X},{obj.RowColNum.Y}信息");
        //}

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
           
        }
    }
}
