using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Ovl.UI.Controls
{
    /// <summary>
    /// WaferMapStageControl.xaml 的交互逻辑
    /// </summary>
    public partial class WaferMapStageControl : UserControl
    {
        public WaferMapStageControl()
        {
            InitializeComponent();

            wmc.ActionOffset = MoveStage;

            this.Loaded += WaferMapStageControl_Loaded;
            this.Unloaded += WaferMapStageControl_Unloaded;
        }

        /// <summary>
        /// 是否运行点击界面，移动轴
        /// </summary>
        public bool IsEnableMove { get; set; }
        IAlignmentService _alignmentService;
        protected WaferMapInfo WaferMap { get { return _alignmentService.WaferMapInfo; } }
        //RowColumnInfo _currentField;
        //WcsPoint _currentR3 = new WcsPoint();
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        //object lockobj = new object();
        private void WaferMapStageControl_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.Elapsed -= DispatcherTimer_Tick;
            StopTimer();
        }


        private void WaferMapStageControl_Loaded(object sender, RoutedEventArgs e)
        {
            timer.Elapsed -= DispatcherTimer_Tick;
            timer.Elapsed += DispatcherTimer_Tick;
            if (true == tb.IsChecked)
            {
                StartTimer();
            }
        }


        /// <summary>
        /// 初始化对齐服务
        /// </summary>
        /// <param name="alignmentService">对齐服务</param>
        public void Init(IAlignmentService alignmentService, WaferMapInfo waferMap)
        {

            _alignmentService = alignmentService;

            wmc.Draw(waferMap);
            // _currentField = RowColumnInfo.Zero;
            //lock (lockobj)
            //{
            //    _currentR3 = new WcsPoint();
            //}
        }

        bool isMoving = false;
        private void MoveStage(RowColumnInfo field, WcsPoint r3)
        {
            if (!IsEnableMove) return;

            bool isCoordMap = cbCoord.IsChecked.Value;
            var dstPoint = _alignmentService.GetAxisPoint(field, r3, isCoordMap);
            LogHelper.LogInfo($"Move stage:Current Field={field},r3={r3},dstPoint={dstPoint}");
            AsyncMove(dstPoint);
        }




        private async void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (_alignmentService == null || isMoving) return;
            var point = await _alignmentService.GetCurrentAxisPositionAsync();
            var wcsPoint = _alignmentService.GetWcsPoint(point);
            Dispatcher.Invoke(() => { wmc.UpdateOffset(wcsPoint); });
            //lock (lockobj)
            //{
            //    var waferMap = WaferMap;
            //    if (waferMap == null) return;
            //    var fieldWidth = waferMap.FieldWidth;
            //    var fieldHeight = waferMap.FieldHeight;

            //    var llc = wcsPoint - waferMap.LlcR1;
            //    int colIndex = (int)(Math.Abs(llc.X) / fieldWidth);
            //    if (WaferMapInfo.CoefficientX == 1)
            //    {
            //        colIndex = llc.X < 0 ? -(colIndex + 1) : colIndex;
            //    }
            //    else if (WaferMapInfo.CoefficientX == -1)
            //    {
            //        colIndex = llc.X < 0 ? -colIndex : colIndex + 1;
            //    }
            //    else
            //    {
            //        LogHelper.LogError("CoefficientX only can be -1 or 1");
            //    }
            //    int rowIndex = (int)(Math.Abs(llc.Y) / fieldHeight);

            //    if (WaferMapInfo.CoefficientY == 1)
            //    {
            //        //Y轴的正方向向下时，（0，0）Field的Y值范围是（-FieldHeight ，0）;
            //        //所以当llc.Y<0，rowIndex 小于等于0，llc.Y>0时，rowIndex 大于等于1；
            //        rowIndex = llc.Y < 0 ? -rowIndex : rowIndex + 1;
            //    }
            //    else if (WaferMapInfo.CoefficientY == -1)
            //    {
            //        //Y轴的正方向向上时，（0，0）Field的Y值范围是（ 0 ，FieldHeight）;
            //        //所以当llc.Y<0，rowIndex 小于等于-1，llc.Y>0时，rowIndex 大于等于0；
            //        rowIndex = llc.Y < 0 ? -(rowIndex + 1) : rowIndex;
            //    }
            //    else
            //    {
            //        LogHelper.LogError("CoefficientY only can be -1 or 1");
            //    }
            //  //  _currentField = new RowColumnInfo() { ColumnIndex = colIndex, RowIndex = rowIndex };
            //   // _currentR3 = new WcsPoint(llc.X - colIndex * fieldWidth, llc.Y - rowIndex * fieldHeight);

            // var fieldR3=   _alignmentService.GetFieldR3(point);
            //    _currentField = fieldR3.Field;
            //    _currentR3 = fieldR3.R3;
            //    //LogHelper.LogInfo($"Update stage:Current Field={_currentField},r3={_currentR3}");
            //    //if (_currentR3.X < 0)
            //    //{
            //    //    _currentR3.X += waferMap.FieldWidth;
            //    //}
            //    //else if (_currentR3.X > waferMap.FieldWidth)
            //    //{
            //    //    _currentR3.X -= waferMap.FieldWidth;
            //    //}
            //    //if (_currentR3.Y > 0)
            //    //{
            //    //    _currentR3.Y -= waferMap.FieldHeight;
            //    //}
            //    //else if (_currentR3.Y < -waferMap.FieldHeight)
            //    //{
            //    //    _currentR3.Y += waferMap.FieldHeight;
            //    //}
            //}
        }

        private void tb_Checked(object sender, RoutedEventArgs e)
        {
            if (true == tb.IsChecked)
            {
                StartTimer();
            }
            else
            {
                StopTimer();
            }
        }

        private void StopTimer()
        {
            timer.Stop();
        }

        private void StartTimer()
        {
            timer.Start();
        }

        private void AsyncMove(Point2d dstPoint)
        {
            if (isMoving)
            {
                LogHelper.LogInfo("The stage is moving now.");
                return;
            }
            isMoving = true;
            Task.Run(() =>
            {
                try
                {
                    var fieldRange = Math.Max(WaferMap.FieldWidth, WaferMap.FieldHeight);
                    if (GeometryHelper.MeasureRadius(_alignmentService.Cow, dstPoint) > WaferMap.Diameter / 2 + fieldRange)
                    {
                        LogHelper.LogInfo($"Stage out of rang:{dstPoint}");
                        return;
                    }
                    _alignmentService.MoveTo(dstPoint);
                }
                finally
                {
                    isMoving = false;
                }
            });
        }

        private void Ellipse_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AsyncMove(_alignmentService.Cow);
        }

        //private RowColumnInfo MoveField(int row, int col)
        //{
        //    var rowIndex = _currentField.RowIndex + row * WaferMap.FieldRowSpan * WaferMapInfo.CoefficientY;
        //    var columnIndex = _currentField.ColumnIndex + col * WaferMap.FieldColumnSpan * WaferMapInfo.CoefficientX;
        //    var field = new RowColumnInfo() { RowIndex = rowIndex, ColumnIndex = columnIndex };
        //    return field;
        //}

        private void MoveStageByField(int row, int col)
        {
            var currentAxis = _alignmentService.GetCurrentAxisPosition();
            var oldAxis = currentAxis;
            currentAxis.X += col * WaferMap.CoordFieldWidth;
            currentAxis.Y += row * WaferMap.CoordFieldHeight;
            LogHelper.LogInfo($"MoveStageByField:RowOffset={row},colOffset={col},oldAxis={oldAxis},newAxis={currentAxis}");

            AsyncMove(currentAxis);
        }

        private void Button_Click_UpField(object sender, RoutedEventArgs e)
        {
            MoveStageByField(-1, 0);
            //var field = MoveField(-1, 0);
            //MoveStage(field, _currentR3);
        }

        private void Button_Click_DownField(object sender, RoutedEventArgs e)
        {
            MoveStageByField(1, 0);
            // MoveStage(field, _currentR3);
        }

        private void Button_Click_LeftField(object sender, RoutedEventArgs e)
        {
            MoveStageByField(0, -1);
            // MoveStage(field, _currentR3);

        }

        private void Button_Click_RightField(object sender, RoutedEventArgs e)
        {
            MoveStageByField(0, 1);
            // MoveStage(field, _currentR3);
        }

        private void MoveStageByCorner(EnumR3OrigionPoint enumR3OrigionPoint)
        {
            LogHelper.LogInfo($"Move to corner:{enumR3OrigionPoint}");
            var currentField = _alignmentService.GetFieldR3(_alignmentService.GetCurrentAxisPosition())?.Field;
            if (currentField == null) return;
            var r3 = new WcsPoint();
            switch (enumR3OrigionPoint)
            {
                case EnumR3OrigionPoint.ULC:
                    r3 = new WcsPoint(0, -WaferMap.CoordFieldHeight); break;
                case EnumR3OrigionPoint.LRC:
                    r3 = new WcsPoint(WaferMap.CoordFieldWidth, 0); break;
                case EnumR3OrigionPoint.URC:
                    r3 = new WcsPoint(WaferMap.CoordFieldWidth, -WaferMap.CoordFieldHeight); break;
                case EnumR3OrigionPoint.COF:
                    r3 = new WcsPoint(WaferMap.CoordFieldWidth / 2, -WaferMap.CoordFieldHeight / 2); break;

            }
            MoveStage(currentField, r3);
        }

        private void Button_Click_FieldULC(object sender, RoutedEventArgs e)
        {
            MoveStageByCorner(EnumR3OrigionPoint.ULC);
            //MoveStage(_currentField, new WcsPoint(0, -WaferMap.CoordFieldHeight));
        }

        private void Button_Click_FieldLLC(object sender, RoutedEventArgs e)
        {
            MoveStageByCorner(EnumR3OrigionPoint.LLC);
            // MoveStage(_currentField, new WcsPoint());
        }

        private void Button_Click_FieldLRC(object sender, RoutedEventArgs e)
        {
            //此处需要考虑坐标方向
            MoveStageByCorner(EnumR3OrigionPoint.LRC);
            //MoveStage(_currentField, new WcsPoint(WaferMap.CoordFieldWidth, 0));
        }

        private void Button_Click_FieldURC(object sender, RoutedEventArgs e)
        {
            //此处需要考虑坐标方向
            MoveStageByCorner(EnumR3OrigionPoint.URC);
            //MoveStage(_currentField, new WcsPoint(WaferMap.CoordFieldWidth, -WaferMap.CoordFieldHeight));
        }
    }
}
