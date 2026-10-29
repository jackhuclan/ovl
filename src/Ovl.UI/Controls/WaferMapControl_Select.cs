using Marvel.DataModels.Recipe;
using System.Collections.Generic;
using System.Windows.Controls;
using InteVega.CBB.ComUI.Helpers;
using System;
using System.Windows.Media;

namespace Ovl.UI.Controls
{
    /// <summary>
    /// 处理与Select相关的交互逻辑
    /// </summary>
    public partial class WaferMapControl : UserControl
    {
        public bool DispSelectedFieldNum = true;
        List<Button> selectedBtns = new List<Button>();
        /// <summary>
        /// 选中Field，更改背景色
        /// </summary>
        /// <param name="ele"></param>
        private void Select(Button ele, bool isRefreshAll = true)
        {
            if (ele == null) return;
            ele.Background = ConvertHelper.BrushFromString("0f54c6");
            if (!selectedBtns.Contains(ele))
                selectedBtns.Add(ele);
            DispSelectedNumbers(ele);
            if(isRefreshAll) UpdateButtonNumbers();

        }

        /// <summary>
        /// 取消选中指定的Field
        /// </summary>
        /// <param name="ele"></param>
        private void UnSelect(Button ele)
        {
            if (ele == null) return;
            ele.Background = fieldBrush;
            if (selectedBtns.Contains(ele))
                selectedBtns.Remove(ele);
            UnCheck(ele);
            UpdateButtonNumbers();
        }

        /// <summary>
        /// 批量选中
        /// </summary>
        /// <param name="lstSelected">需要选中的Field列表</param>
        /// <param name="isClearOthers">是否清空其它选中项</param>
        private void Select(List<Button> lstSelected, bool isClearOthers)
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
        /// <param name="btn">需要进行反向选中的Field</param>
        /// <param name="isClearOther">是否需要清除其它选中项</param>
        private void ReverseSelect(Button btn, bool isClearOther = false)
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

                if (FuncCheckField?.Invoke() ?? true)
                {
                    Select(btn);
                }
            }
        }

        /// <summary>
        /// 选中指定的Field列表
        /// </summary>
        /// <param name="fields">需要选中的Field列表</param>
        public void Select(IList<RowColumnInfo> fields)
        {
            foreach (var fieldInfo in fields)
            {
                var absField=GetAbsoluteField(fieldInfo);
                var id = GetId(absField.RowIndex, absField.ColumnIndex);
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
        /// 获取被选中的Field列表
        /// </summary>
        /// <returns></returns>
        public List<RowColumnInfo> GetSelectedFields()
        {
            List<RowColumnInfo> fields = new List<RowColumnInfo>();
            foreach (var btn in selectedBtns)
            {
                RowColumnInfo field = btn.DataContext as RowColumnInfo;
                fields.Add(field);
            }
            return fields;
        }

        /// <summary>
        /// 获取Field列表
        /// </summary>
        /// <returns></returns>
        public List<RowColumnInfo> GetFields()
        {
            List<RowColumnInfo> fields = new List<RowColumnInfo>();
            foreach (var btn in dicElments.Values)
            {
                RowColumnInfo field = btn.DataContext as RowColumnInfo;
                fields.Add(field);
            }
            return fields;
        }
        private void UpdateButtonNumbers()
        {
            if (!DispSelectedFieldNum) return;
            // 对选中的按钮重新排序并更新内容
            for (int i = 0; i < selectedBtns.Count; i++)
            {
                Button btn = selectedBtns[i];
                btn.Content = new TextBlock()
                {
                    Text = (i + 1).ToString(),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Foreground = Brushes.White
                };
            }
        }
        private void DispSelectedNumbers(Button ele)
        {
            if (!DispSelectedFieldNum) return;
            ele.Content = selectedBtns.Count;
            double newSize = Math.Min(ele.Width, ele.Height) * 0.6; // Adjust the multiplier as needed
            ele.FontSize = newSize;
            ele.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            ele.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
        }
    }
}
