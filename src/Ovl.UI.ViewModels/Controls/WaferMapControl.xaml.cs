using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.ComUI.Helpers;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Ovl.UI.ViewModels.Controls
{
    /// <summary>
    /// 晶圆曝光场示意图控件
    /// </summary>
    public partial class WaferMapControl : UserControl
    {
        public WaferMapControl()
        {
            InitializeComponent();
            Cursor = Cursors.Arrow;
            PreviewMouseDown += GridFields_PreviewMouseDown;
            PreviewMouseMove += GridFields_PreviewMouseMove;
            PreviewMouseUp += GridFields_PreviewMouseUp;
            gridFields.MouseDown += GridFields_MouseDown;
        }



        /// <summary>
        /// Field控件字典，便于查询
        /// </summary>
        Dictionary<int, Button> dicElments = new Dictionary<int, Button>();
        SolidColorBrush fieldBrush = ConvertHelper.BrushFromString("99afafa0");

        WcsPoint startPoint = new WcsPoint(0, 0);
        double fieldWidth;
        double fieldHeight;
        int fieldRowSpan;
        int fieldColumnSpan;
        double dieWidth;
        double dieHeight;
        int rowCount;
        int columnCount;
        double fieldDiameter;
        double coordFieldWidth;
        double coordFieldHeight;
        WaferMapInfo WaferMapInfo;

        //(0,0)Field在晶格中的行列号
        RowColumnInfo _gridField0;
        Brush notchBrush = Brushes.Transparent;
        /// <summary>
        /// 鼠标按下事件，返回点击点与晶圆中心的偏移量
        /// </summary>
        public Action<RowColumnInfo, WcsPoint> ActionOffset;
        /// <summary>
        /// 鼠标摁下后，验证Field数量
        /// </summary>
        public Func<bool> FuncCheckField;

        EnumEditMode _editMode = EnumEditMode.MultiSelect;
        /// <summary>
        /// 控件的当前编辑状态
        /// </summary>
        public EnumEditMode EditMode
        {
            get { return _editMode; }
            set
            {
                _editMode = value;
                pathCross.Visibility = Visibility.Collapsed;
                Cursor = Cursors.Arrow;
                switch (_editMode)
                {
                    case EnumEditMode.EditField:
                        Cursor = Cursors.Pen; break;
                    case EnumEditMode.Move:
                        pathCross.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        /// <summary>
        /// 是否可以进行框选
        /// </summary>
        bool IsEnableBoxSelect
        {
            get
            {
                return EditMode == EnumEditMode.MultiSelect || EditMode == EnumEditMode.BoxSelect || EditMode == EnumEditMode.Check;
            }
        }



        /// <summary>
        /// 初始化wafermap数据
        /// </summary>
        /// <param name="wi">wafer信息</param>
        /// <param name="actInitR1">设置R1的委托，界面重绘时会用界面数据计算出R1</param>
        private void InitData(WaferMapInfo wi)
        {
            WaferMapInfo = wi;
            fieldDiameter = wi.Diameter - wi.EdgeExclusion * 2;
            if (fieldDiameter <= 0)
                return;
            fieldWidth = wi.FieldWidth;
            fieldHeight = wi.FieldHeight;
            fieldRowSpan = wi.FieldRowSpan;
            fieldColumnSpan = wi.FieldColumnSpan;
            coordFieldHeight = wi.CoordFieldHeight;
            coordFieldWidth = wi.CoordFieldWidth;


            //将Field0的左下角设定为Grid的中心位置，然后分别计算所需要的Field的行数和列数
            var leftFieldCount = (int)((fieldDiameter / 2 + wi.LlcR1.X) / fieldWidth); //Field0左侧可容纳的Field数量
            var rightFieldCount = (int)((fieldDiameter / 2 - wi.LlcR1.X) / fieldWidth); //Field0右侧可容纳Field数量（包含Field0）
            var fieldColumnCount = Math.Max(leftFieldCount, rightFieldCount) * 2 + 2; //取两者的最大值，使得左右两边数量相等，保证Fied0的左边在Grid的中心位置
            var topFieldCount = (int)((fieldDiameter / 2 + wi.LlcR1.Y) / fieldHeight); //Field0上方可容纳的Field数量（包含Field0本身）
            var bottomFieldCount = (int)((fieldDiameter / 2 - wi.LlcR1.Y) / fieldHeight); //Field0下方可容纳的Field数量 
            var fieldRowCount = Math.Max(topFieldCount, bottomFieldCount) * 2 + 2; //取两者的最大值，使得上下两边Field数量相等，保证Field的右下角在Grid的中心问题

            //获取中心位置Field的行号与列号
            _gridField0 = new RowColumnInfo() { RowIndex = (fieldRowCount / 2 - 1) * fieldRowSpan, ColumnIndex = fieldColumnCount / 2 * fieldColumnSpan };
            rowCount = fieldRowCount * fieldRowSpan;
            columnCount = fieldColumnCount * fieldColumnSpan;

            dieWidth = fieldWidth / fieldColumnSpan;
            dieHeight = fieldHeight / fieldRowSpan;
            gridFields.Height = rowCount * dieHeight;
            gridFields.Width = columnCount * dieWidth;

        }


        /// <summary>
        /// 根据wafer信息初始化控件
        /// </summary>
        /// <param name="wi">wafer信息</param>
        /// <param name="actInitR1">设置R1的委托，界面重绘时会用界面数据计算出R1</param>
        private void Init(WaferMapInfo wi)
        {
            ChangeShape(wi.Shape);

            InitData(wi);
            var tsX = wi.LlcR1.X * wi.CoefficientX;
            var tsY = wi.LlcR1.Y * wi.CoefficientY;
            tsFields.X = tsX;
            tsFields.Y = tsY;
            //外圆半径
            gridOuter.Width = gridOuter.Height = wi.Diameter;
            //内圆半径
            gridInner.Width = gridInner.Height = fieldDiameter;
            //  清空原数据
            dicElments.Clear();
            UnSelectAll();
            gridFields.Children.Clear();
            gridFields.ColumnDefinitions.Clear();
            gridFields.RowDefinitions.Clear();


            //设置Grid的行与列，也就是die的总行数和列数
            for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                gridFields.ColumnDefinitions.Add(new ColumnDefinition());
            }
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                gridFields.RowDefinitions.Add(new RowDefinition());
            }

            DrawNotch(wi);
        }

        /// <summary>
        /// 绘制Notch角
        /// </summary>
        /// <param name="wi"></param>
        public void DrawNotch(WaferMapInfo wi)
        {
            epNotch.Visibility = Visibility.Hidden;
            rectFlat.Visibility = Visibility.Hidden;
            switch (wi.NotchType)
            {
                case EnumNotchType.Notch:
                    DrawEllipseNotch(wi);
                    break;
                case EnumNotchType.Flat:
                    DrawFlatNotch(wi);
                    break;
                case EnumNotchType.None:
                default:
                    break;
            }
        }

        /// <summary>
        /// 绘制Notch角
        /// </summary>
        /// <param name="wi">WaferMapInfo对象</param>
        private void DrawEllipseNotch(WaferMapInfo wi)
        {
            SetVisibilityAndFill(epNotch);

            SetNotchFill(epNotch, wi.NotchDirection);
        }

        /// <summary>
        /// 绘制Flat类型Notch角
        /// </summary>
        /// <param name="wi">WaferMapInfo对象</param>
        private void DrawFlatNotch(WaferMapInfo wi)
        {
            SetVisibilityAndFill(rectFlat);

            SetFlatNotchFill(rectFlat, wi.NotchDirection);
        }

        /// <summary>
        /// 设置定位槽类型Notch角的显示和填充
        /// </summary>
        /// <param name="notchElement">Notch角的UI元素</param>
        private void SetNotchFill(Shape notchElement, EnumNotchDirection direction)
        {
            switch (direction)
            {
                case EnumNotchDirection.Left:
                    notchElement.HorizontalAlignment = HorizontalAlignment.Left;
                    notchElement.VerticalAlignment = VerticalAlignment.Center;
                    break;
                case EnumNotchDirection.Right:
                    notchElement.HorizontalAlignment = HorizontalAlignment.Right;
                    notchElement.VerticalAlignment = VerticalAlignment.Center;
                    break;
                case EnumNotchDirection.Up:
                    notchElement.HorizontalAlignment = HorizontalAlignment.Center;
                    notchElement.VerticalAlignment = VerticalAlignment.Top;
                    break;
                case EnumNotchDirection.Down:
                    notchElement.HorizontalAlignment = HorizontalAlignment.Center;
                    notchElement.VerticalAlignment = VerticalAlignment.Bottom;
                    break;
            }
        }

        /// <summary>
        /// 设置Flat类型Notch角的填充
        /// </summary>
        /// <param name="flatNotchElement">FlatNotch角的UI元素</param>
        private void SetFlatNotchFill(Shape flatNotchElement, EnumNotchDirection direction)
        {
            switch (direction)
            {

                case EnumNotchDirection.Left:
                    flatNotchElement.Width = 10;
                    flatNotchElement.Height = double.NaN;
                    flatNotchElement.HorizontalAlignment = HorizontalAlignment.Left;
                    flatNotchElement.VerticalAlignment = VerticalAlignment.Stretch;
                    break;
                case EnumNotchDirection.Right:
                    flatNotchElement.Width = 10;
                    flatNotchElement.Height = double.NaN;
                    flatNotchElement.HorizontalAlignment = HorizontalAlignment.Right;
                    flatNotchElement.VerticalAlignment = VerticalAlignment.Stretch;
                    break;
                case EnumNotchDirection.Up:
                    flatNotchElement.Width = double.NaN;
                    flatNotchElement.Height = 10;
                    flatNotchElement.HorizontalAlignment = HorizontalAlignment.Stretch;
                    flatNotchElement.VerticalAlignment = VerticalAlignment.Top;
                    break;
                case EnumNotchDirection.Down:
                    flatNotchElement.Width = double.NaN;
                    flatNotchElement.Height = 10;
                    flatNotchElement.HorizontalAlignment = HorizontalAlignment.Stretch;
                    flatNotchElement.VerticalAlignment = VerticalAlignment.Bottom;
                    break;
            }
        }

        /// <summary>
        /// 设置Visibility 和 Fill 的通用方法
        /// </summary>
        /// <param name="notchElement">UI元素</param>
        private void SetVisibilityAndFill(Shape notchElement)
        {
            notchElement.Visibility = Visibility.Visible;
            notchBrush = this.Background;
            if (notchBrush == null || notchBrush == Brushes.Transparent)
            {
                // 查找背景色
                FrameworkElement parent = VisualLogicTreeHelper.FindVisualParent<Control>(this, CheckBackground);
                if (parent == null)
                {
                    parent = VisualLogicTreeHelper.FindLogicalParent<FrameworkElement>(this, CheckBackground);
                }
            }
            notchElement.Fill = notchBrush;
        }

        /// <summary>
        /// 重绘Wafer示意图
        /// </summary>
        /// <param name="wi">wafer信息</param>
        /// <param name="field0R1">Field0的左下角与晶圆圆心的偏移</param>
        public void RedrawByLlc(WaferMapInfo wi)
        {
            //  wi.R1 = field0R1;
            Init(wi);
            Redraw();
        }


        ///// <summary>
        ///// 获取Field0左下角到晶格中心的偏移
        ///// </summary>
        ///// <returns></returns>
        //private WcsPoint GetField0Offset()
        //{
        //    //Field0的左下角坐标
        //    var llc = new WcsPoint(field0.ColumnIndex * dieWidth, field0.RowIndex * dieHeight + fieldHeight);
        //    return llc - gridCenter;
        //}

        /// <summary>
        /// 绘制waferMap，根据已有的Field列表绘制Field
        /// </summary>
        /// <param name="map">waferMap</param>
        public void Draw(WaferMapInfo map)
        {
            Init(map);
            foreach (var field in map.FieldInfos)
            {
                var absField = GetAbsoluteField(field);
                AddField(absField.RowIndex, absField.ColumnIndex);
            }
            //var offset = GetField0Offset();
            //return offset + map.GridCenterR1;
        }



        private bool CheckBackground(DependencyObject p)
        {
            if (p is Control c)
            {
                bool hasBrush = c.Background != null && c.Background != Brushes.Transparent;
                if (hasBrush)
                {
                    notchBrush = c.Background;
                }
                return hasBrush;
            }
            else if (p is Panel pl)
            {
                bool hasBrush = pl.Background != null && pl.Background != Brushes.Transparent;
                if (hasBrush)
                {
                    notchBrush = pl.Background;
                }
                return hasBrush;
            }
            return false;

        }

        /// <summary>
        /// 获取某个位置相对于Field0的相对坐标
        /// </summary>
        /// <param name="rowIndex">grid的行号</param>
        /// <param name="columnIndex">grid的列号</param>
        /// <returns></returns>
        RowColumnInfo GetRelativeField(int rowIndex, int columnIndex)
        {
            return new RowColumnInfo((rowIndex - _gridField0.RowIndex) * WaferMapInfo.CoefficientY, (columnIndex - _gridField0.ColumnIndex) * WaferMapInfo.CoefficientX);
        }

        /// <summary>
        /// 获取某个Field相对于Field0的绝对序号坐标
        /// </summary>
        /// <param name="rowIndex">grid的行号</param>
        /// <param name="columnIndex">grid的列号</param>
        /// <returns></returns>
        RowColumnInfo GetAbsoluteField(RowColumnInfo field)
        {
            return new RowColumnInfo(field.RowIndex / WaferMapInfo.CoefficientY + _gridField0.RowIndex, field.ColumnIndex / WaferMapInfo.CoefficientX + _gridField0.ColumnIndex);
        }

        void Redraw()
        {
            //重新绘制Field
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    if (rowIndex % fieldRowSpan == 0 && columnIndex % fieldColumnSpan == 0)
                    {
                        AddField(rowIndex, columnIndex, true);
                    }
                }
            }
        }

        /// <summary>
        /// 获取Field的ID
        /// </summary>
        /// <param name="row">Feild所在的行号</param>
        /// <param name="column">Feild所在的列号</param>
        /// <returns></returns>
        private int GetId(int row, int column)
        {
            return row * columnCount + column;
        }

        bool CheckAddAble(int rowIndex, int columnIndex, bool ischeckEdge = false)
        {
            return WaferMapInfo.ClipToBounds ? CheckAddAbleClipToBounds(rowIndex, columnIndex, ischeckEdge) : CheckAddAbleDefault(rowIndex, columnIndex, ischeckEdge);
        }
        /// <summary>
        /// 检测指定的行与列所在的Field是否有一部分在晶圆内部，如果都不在晶圆内部则不绘制
        /// </summary>
        /// <param name="rowIndex">行号</param>
        /// <param name="columnIndex">列号</param>
        /// <param name="ischeckEdge">是否排除边缘空白</param>
        /// <returns></returns>
        bool CheckAddAbleDefault(int rowIndex, int columnIndex, bool ischeckEdge = false)
        {
            var field = GetRelativeField(rowIndex, columnIndex);
            var wcsPoint = WaferMapInfo.GetWcsPoint(field, EnumR3OrigionPoint.COF, new WcsPoint());
            double fieldCenterX = wcsPoint.X;
            double fieldCenterY = wcsPoint.Y;
            //判断Field的四个顶点
            if (fieldCenterX < 0)
            {
                wcsPoint.X += fieldWidth / 2;
                if (fieldCenterY < 0) //晶圆中心左上角的Field判断该Field的右下角是否在晶圆内
                {
                    wcsPoint.Y += fieldHeight / 2;
                }
                else  //晶圆中心左下角的Field判断该Field的右上角是否在晶圆内
                {
                    wcsPoint.Y -= fieldHeight / 2;
                }
            }
            else
            {
                wcsPoint.X -= fieldWidth / 2;
                if (fieldCenterY < 0) //晶圆中心右上角的Field判断该Field的左下角是否在晶圆内
                {
                    wcsPoint.Y += fieldHeight / 2;
                }
                else //晶圆中心右下角的Field判断该Field的左上角是否在晶圆内
                {
                    wcsPoint.Y -= fieldHeight / 2;
                }
            }
            var distance = GeometryHelper.Measure(0, 0, wcsPoint.X, wcsPoint.Y);
            if (ischeckEdge)
                return distance <= fieldDiameter / 2;
            else
                return distance <= gridOuter.Width / 2;
        }


        /// <summary>
        /// 检测指定的行与列所在的Field是否超出晶圆范围，如果超出晶圆范围，则不允许绘制
        /// </summary>
        /// <param name="rowIndex">行号</param>
        /// <param name="columnIndex">列号</param>
        /// <param name="ischeckEdge">是否排除边缘空白</param>
        /// <returns></returns>
        bool CheckAddAbleClipToBounds(int rowIndex, int columnIndex, bool ischeckEdge = false)
        {
            var field = GetRelativeField(rowIndex, columnIndex);
            var wcsPoint = WaferMapInfo.GetWcsPoint(field, EnumR3OrigionPoint.COF, new WcsPoint());
            double fieldCenterX = wcsPoint.X;
            double fieldCenterY = wcsPoint.Y;
            //判断Field的四个顶点，确保在晶圆内每个Field都是完整的
            if (fieldCenterX < 0)
            {
                wcsPoint.X -= fieldWidth / 2;
                if (fieldCenterY < 0) //左上角
                {
                    wcsPoint.Y -= fieldHeight / 2;
                }
                else  //左下角
                {
                    wcsPoint.Y += fieldHeight / 2;
                }
            }
            else
            {
                wcsPoint.X += fieldWidth / 2;
                if (fieldCenterY < 0) //右上角
                {
                    wcsPoint.Y -= fieldHeight / 2;
                }
                else //右下角
                {
                    wcsPoint.Y += fieldHeight / 2;
                }
            }
            var distance = GeometryHelper.Measure(0, 0, wcsPoint.X, wcsPoint.Y);
            if (ischeckEdge)
                return distance <= fieldDiameter / 2;
            else
                return distance <= gridOuter.Width / 2;
        }

        /// <summary>
        /// 添加Field并判断Field是否超出边界
        /// </summary>
        /// <param name="rowIndex">Field左上角的行号</param>
        /// <param name="columnIndex">Field左上角的列号</param>
        /// <returns></returns>
        private Button AddField(int rowIndex, int columnIndex, bool ischeckEdge = false)
        {

            //是否允许添加Field
            bool isEnableAdd = CheckAddAble(rowIndex, columnIndex, ischeckEdge);

            if (isEnableAdd)
            {
                int id = GetId(rowIndex, columnIndex);
                if (dicElments.ContainsKey(id))
                    return dicElments[id] as Button;
                var btn = new Button();
                if (fieldColumnSpan > 1 || fieldRowSpan > 1)
                {
                    btn.Content = new FieldElement(fieldRowSpan, fieldColumnSpan);
                }
                btn.Width = fieldWidth;
                btn.Height = fieldHeight;
                //Field的点击事件，如果是编辑状态，当点击该Field时，就是移除Field
                //如果是多选状态，则将该控件添加到选中列表
                //如果是单选状态，则先清空其它选中项，然后将该控件添加到选中列表
                btn.Click += (s, e) =>
                {
                    switch (EditMode)
                    {
                        case EnumEditMode.EditField:
                            Remove(btn);
                            break;
                        case EnumEditMode.MultiSelect:
                            ReverseSelect(btn);
                            break;
                        case EnumEditMode.SingleSelect:
                        case EnumEditMode.BoxSelect:
                            ReverseSelect(btn, true);
                            break;
                        case EnumEditMode.Check:
                            ReverseCheck(btn);
                            break;
                    }
                };
                btn.MouseRightButtonDown += (s, e) =>
                {
                    switch (EditMode)
                    {
                        case EnumEditMode.None:
                            var cm = (ContextMenu)FindResource("Res_ContextMenu");
                            cm.DataContext = btn.DataContext;
                            cm.IsOpen = true;
                            break;
                        default:
                            break;
                    }
                };

                btn.PreviewMouseDown += Field_PreviewMouseDown;

                Panel.SetZIndex(btn, 1);
                Grid.SetRowSpan(btn, fieldRowSpan);
                Grid.SetColumnSpan(btn, fieldColumnSpan);
                Grid.SetColumn(btn, columnIndex);
                Grid.SetRow(btn, rowIndex);
                var field = GetRelativeField(rowIndex, columnIndex);
                btn.ToolTip = $"({field.ColumnIndex},{field.RowIndex})";
                btn.DataContext = field;
                btn.Background = fieldBrush;
                if (field.Equals(RowColumnInfo.Zero))
                {
                    btn.BorderBrush = Brushes.White;
                    btn.BorderThickness = new Thickness(1);
                }
                else
                {
                    btn.BorderBrush = ConvertHelper.BrushFromString("ffff00");
                    btn.BorderThickness = new Thickness(0.5);
                }
                gridFields.Children.Add(btn);
                dicElments.Add(id, btn);
                return btn;
            }
            return null;
        }



        /// <summary>
        /// 移除Field
        /// </summary>
        /// <param name="ele"></param>
        private void Remove(Button ele)
        {
            var fieldInfo = ele.DataContext as RowColumnInfo;
            if (fieldInfo.Equals(_gridField0))
                return;
            gridFields.Children.Remove(ele);
            var absField = GetAbsoluteField(fieldInfo);
            var id = GetId(absField.RowIndex, absField.ColumnIndex);
            dicElments.Remove(id);
            UnSelect(ele);


        }

        public void DisplayFields(bool isDisplay)
        {
            foreach (var item in gridFields.Children)
            {
                if (item is Button button)
                {
                    button.Visibility = !isDisplay ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }



        Shape outBound = null;
        Shape innerBound = null;

        /// <summary>
        /// 修改晶圆形状
        /// </summary>
        /// <param name="shape"></param>
        public void ChangeShape(EnumWrapeShape shape)
        {
            ChangeShape(shape, gridOuter, ref outBound);
            ChangeShape(shape, gridInner, ref innerBound);
        }

        private void ChangeShape(EnumWrapeShape shape, Grid grid, ref Shape bound)
        {
            if (bound != null)
            {
                grid.Children.Remove(bound);
            }

            switch (shape)
            {
                case EnumWrapeShape.Circle:
                    bound = new Ellipse();
                    break;
                case EnumWrapeShape.Square:
                    bound = new System.Windows.Shapes.Rectangle();
                    break;
                case EnumWrapeShape.Polygon:
                    bound = new System.Windows.Shapes.Polygon() { Points = new PointCollection() { new System.Windows.Point(0, 2), new System.Windows.Point(1, 0), new System.Windows.Point(3, 0), new System.Windows.Point(4, 2), new System.Windows.Point(3, 4), new System.Windows.Point(1, 4) } };
                    break;
            }
            bound.Opacity = 0.9;
            bound.Fill = ConvertHelper.BrushFromString("555555");
            bound.Stretch = Stretch.Uniform;
            if (bound == outBound)
            {
                bound.Stroke = ConvertHelper.BrushFromString("AFC4E6");
                bound.StrokeThickness = 1;
            }
            Panel.SetZIndex(bound, 0);
            grid.Children.Add(bound);

        }

        private void MISetField0_Click(object sender, RoutedEventArgs e)
        {
            var cm = (ContextMenu)FindResource("Res_ContextMenu");
            if (cm.DataContext is RowColumnInfo field)
            {
                var x = field.ColumnIndex * WaferMapInfo.FieldWidth;
                var y = field.RowIndex * WaferMapInfo.FieldHeight;
                WaferMapInfo.R1 += new WcsPoint(x, y);
                RedrawByLlc(WaferMapInfo);
            }
            cm.IsOpen = false;
        }
    }

    /// <summary>
    /// 编辑模式
    /// </summary>
    public enum EnumEditMode
    {
        /// <summary>
        /// 无任何操作
        /// </summary>
        None,
        /// <summary>
        /// 多选模式，可以点击或者框选多个
        /// </summary>
        MultiSelect,

        /// <summary>
        /// 单选模式，只能选择一个
        /// </summary>
        SingleSelect,

        /// <summary>
        /// 单选模式，只能选择一个，或者只能框选一个区域
        /// </summary>
        BoxSelect,

        /// <summary>
        /// 编辑Field模式，此时可以擦除或者填充Field
        /// </summary>
        EditField,
        /// <summary>
        /// 勾选模式
        /// </summary>
        Check,
        /// <summary>
        /// 移动模式
        /// </summary>
        Move,
    }
}

