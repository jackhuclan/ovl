using InteVega.CBB.Common.Algorithm.Shapes;
using Marvel.DataModels.Recipe;
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
using InteVega.CBB.ComUI.Helpers;

namespace Ovl.UI.ViewModels.Controls
{
    /// <summary>
    /// ControlTargetArray.xaml 的交互逻辑
    /// </summary>
    public partial class ControlTargetArray : UserControl
    {
        public ControlTargetArray()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Target控件字典，便于查询
        /// </summary>
        Dictionary<int, ControlTestIcon> dicElments = new Dictionary<int, ControlTestIcon>();
        SolidColorBrush targetBrush = ConvertHelper.BrushFromString("99afafa0");
        TargetArrayLocation _arrayTarget;
        List<ControlTestIcon> selectedBtns = new List<ControlTestIcon>();
        ControlTestIcon _referenceControl = null;
        ControlTestIcon _rightClickedControl = null;
        /// <summary>
        /// 重绘目标点阵列示意图
        /// </summary>
        /// <param name="wi">wafer信息</param>
        public void Redraw(TestInfoBase test, TargetArrayLocation array)
        {
            try
            {
                var test_overlay = test as TestInfoBase ;
                _arrayTarget = array;
                canvas.Children.Clear();
                dicElments.Clear();
                selectedBtns.Clear();
                double targetWidth = Math.Max(test_overlay.TrainSetting .GetOutlineWidth(), 5);
                double targetHeight=Math.Max(test_overlay.TrainSetting.GetOutlineHeight(), 5);
                for (int i = 0; i < _arrayTarget.RowCount; i++)
                {
                    for (int j = 0; j < _arrayTarget.ColumnCount; j++)
                    {
                        RowColumnInfo rowColumnInfo = new RowColumnInfo() { RowIndex = i, ColumnIndex = j };
                        ControlTestIcon cti = new ControlTestIcon
                        {
                            DataContext = test_overlay.TrainSetting ,
                            Width = targetWidth,
                            Height =targetHeight,
                            BorderThickness = new Thickness(0.1),
                            Background = targetBrush,
                            Tag = rowColumnInfo,
                            ContextMenu = (ContextMenu)FindResource("ContextMenuTemplate"),
                        };

                        if (rowColumnInfo.Equals(_arrayTarget.ReferenceTarget))
                        {
                            SetReferenceTarget(cti);
                        }
                        else
                        {
                            cti.BorderBrush = ConvertHelper.BrushFromString("ffff00");
                        }
                        cti.MouseLeftButtonDown += (s, e) => ReverseSelect(cti);
                        cti.PreviewMouseRightButtonDown += (s, e) => _rightClickedControl = cti;

                        int id = GetId(i, j);
                        dicElments.Add(id, cti);
                        double x = j * (cti.Width + _arrayTarget.ColumnOffset.X) + i * _arrayTarget.RowOffset.X;
                        double y = i * (cti.Height + _arrayTarget.RowOffset.Y) + j * _arrayTarget.ColumnOffset.Y;
                        Canvas.SetLeft(cti, x);
                        Canvas.SetTop(cti, y);
                        canvas.Children.Add(cti);
                    }
                }
                canvas.Width = (targetWidth + _arrayTarget.ColumnOffset.X + _arrayTarget.RowOffset.X) * _arrayTarget.ColumnCount;
                canvas.Height = (targetHeight + _arrayTarget.ColumnOffset.Y + _arrayTarget.RowOffset.Y) * _arrayTarget.RowCount;
                //ts.X = -canvas.Width / 2;
                //ts.Y = -canvas.Height / 2;
            }
            catch
            {

            }
        }

        /// <summary>
        /// 重绘目标点阵列示意图
        /// </summary>
        /// <param name="wi">wafer信息</param>
        public void Draw(TestInfoBase test, TargetArrayLocation array)
        {
            Redraw(test, array);
            Select(array.SelectedTargets);
        }

        /// <summary>
        /// 获取Target的ID
        /// </summary>
        /// <param name="row">Feild所在的行号</param>
        /// <param name="column">Feild所在的列号</param>
        /// <returns></returns>
        private int GetId(int row, int column)
        {
            return row * _arrayTarget.ColumnCount + column;
        }

        /// <summary>
        /// 选中Target，更改背景色
        /// </summary>
        /// <param name="ele"></param>
        private void Select(ControlTestIcon ele)
        {
            if (ele == null) return;
            ele.Background = ConvertHelper.BrushFromString("0f54c6");
            if (!selectedBtns.Contains(ele))
                selectedBtns.Add(ele);
        }

        /// <summary>
        /// 取消选中指定的Target
        /// </summary>
        /// <param name="ele"></param>
        private void UnSelect(ControlTestIcon ele)
        {
            if (ele == null) return;
            ele.Background = targetBrush;
            if (selectedBtns.Contains(ele))
                selectedBtns.Remove(ele);

        }

        /// <summary>
        /// 批量选中
        /// </summary>
        /// <param name="lstSelected">需要选中的Target列表</param>
        /// <param name="isClearOthers">是否清空其它选中项</param>
        private void Select(List<ControlTestIcon> lstSelected, bool isClearOthers = false)
        {
            if (isClearOthers)
                UnSelectAll();
            foreach (var btn in lstSelected)
            {
                Select(btn);
            }
        }

        /// <summary>
        /// 反向选中
        /// </summary>
        /// <param name="btn">需要进行反向选中的Target</param>
        /// <param name="isClearOther">是否需要清除其它选中项</param>
        private void ReverseSelect(ControlTestIcon btn, bool isClearOther = false)
        {
            bool isSelected = selectedBtns.Contains(btn);

            if (isSelected)
            {
                UnSelect(btn);
            }
            else
            {
                if (isClearOther)
                    UnSelectAll();
                Select(btn);
            }
        }

        /// <summary>
        /// 选中指定的Target列表
        /// </summary>
        /// <param name="targets">需要选中的Target列表</param>
        public void Select(IList<RowColumnInfo> targets)
        {
            foreach (var targetInfo in targets)
            {
                var id = GetId(targetInfo.RowIndex, targetInfo.ColumnIndex);
                if (dicElments.TryGetValue(id, out var btn))
                {
                    Select(btn);
                }
            }
        }

        /// <summary>
        /// 全选
        /// </summary>
        public void SelectAll()
        {
            foreach (var ele in dicElments)
            {
                Select(ele.Value);
            }
        }
        /// <summary>
        /// 清空所选中的项
        /// </summary>
        public void UnSelectAll()
        {
            while (selectedBtns.Count > 0)
            {
                UnSelect(selectedBtns[0]);
            }
        }


        /// <summary>
        /// 获取被选中的Target列表
        /// </summary>
        /// <returns></returns>
        public List<RowColumnInfo> GetSelectedTargets()
        {
            List<RowColumnInfo> targets = new List<RowColumnInfo>();
            foreach (var btn in selectedBtns)
            {
                RowColumnInfo target = btn.Tag as RowColumnInfo;
                if (target != null)
                    targets.Add(target);
            }
            return targets;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetReferenceTarget(_rightClickedControl);
        }

        private void SetReferenceTarget(ControlTestIcon referenceTarget)
        {
            if (_referenceControl != null)
            {
                _referenceControl.BorderThickness = new Thickness(0.1);
                _referenceControl.BorderBrush = ConvertHelper.BrushFromString("ffff00");
            }
            if (referenceTarget != null)
            {
                _referenceControl = referenceTarget;
                _referenceControl.BorderBrush = Brushes.Red ;
                _referenceControl.BorderThickness = new Thickness(0.2);
                _arrayTarget.ReferenceTarget = referenceTarget.Tag as RowColumnInfo;
            }
            else
            {
                _arrayTarget.ReferenceTarget = null;
            }
        }
    }
}
