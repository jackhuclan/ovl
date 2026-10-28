using Marvel.DataModels.Core;
using Marvel.DataModels.Recipe;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ovl.UI.ViewModels.Controls
{
    /// <summary>
    /// 处理与鼠标操作相关的交互逻辑
    /// </summary>
    public partial class WaferMapControl : UserControl
    {
        /// <summary>
        /// 是否允许滚轮缩放
        /// </summary>
        public bool IsEnableScale { get; set; } = true;

        Border selectBorder = new Border() { Background = Brushes.Gray, Opacity = 0.4 };
        Canvas selectCanvas = new Canvas();

        private void GridFields_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(gridFields);
            startPoint = new WcsPoint(point.X, point.Y);
        }
        private void Field_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (EditMode != EnumEditMode.Move) return;
            var btn = sender as Button;
            var field = btn.DataContext as RowColumnInfo;
            var point = e.GetPosition(btn);
            //此处坐标计算需要带方向
            var x = point.X / btn.Width * coordFieldWidth;
            var y = (point.Y - btn.Height) / btn.Height * coordFieldHeight;
            var offsetPoint = new WcsPoint(x, y);
            ActionOffset?.Invoke(field, offsetPoint);
        }

        /// <summary>
        /// 点击GridFields的空白区域时，如果是编辑状态，此时在点击位置上判断是否能添加Field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridFields_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (EditMode == EnumEditMode.EditField)
            {
                int columnIndex = (int)(startPoint.X / dieWidth);
                int rowIndex = (int)(startPoint.Y / dieHeight);
                //依次以所点击的点作为Field的0行0列，0行1列……，去判断此时组成的Field是否会与其它的Field发生重叠
                for (int i = 0; i < fieldRowSpan; i++)
                {
                    for (int j = 0; j < fieldColumnSpan; j++)
                    {
                        int topRowIndex = rowIndex - i;
                        int leftColumnIndex = columnIndex - j;
                        //如果没有重叠，则尝试添加Field
                        if (!ExistField(topRowIndex, leftColumnIndex))
                        {
                            //如果成功添加Field，则跳出循环
                            if (AddField(topRowIndex, leftColumnIndex) != null)
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 判断以topRowIndex,leftColumnIndex为左上角的Field是否有与其它的Field重叠
        /// </summary>
        /// <param name="topRowIndex">左上角行号</param>
        /// <param name="leftColumnIndex">左上角列号</param>
        /// <returns></returns>
        private bool ExistField(int topRowIndex, int leftColumnIndex)
        {
            //如果某个Field与当前的Field存在重叠，
            //则该Field的左上角的行号在topRowIndex - fieldRowSpan + 1和 topRowIndex + fieldRowSpan-1之间；
            //列号在 leftColumnIndex - fieldColumSpan + 1和 leftColumnIndex + fieldColumSpan-1之间；
            for (int i = topRowIndex - fieldRowSpan + 1; i < topRowIndex + fieldRowSpan; i++)
            {
                for (int j = leftColumnIndex - fieldColumnSpan + 1; j < leftColumnIndex + fieldColumnSpan; j++)
                {
                    if (i < 0 || j < 0) continue;
                    int id = GetId(i, j);
                    if (dicElments.ContainsKey(id))
                        return true;
                }
            }
            return false;
        }

        int boxSelectWidth = 5;
        private void GridFields_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            if (!IsEnableBoxSelect)
                return;
            var movePoint = e.GetPosition(gridFields);
            var startX = Math.Max(0, Math.Min(startPoint.X, movePoint.X));
            var startY = Math.Max(0, Math.Min(startPoint.Y, movePoint.Y));
            var endX = Math.Min(gridFields.Width, Math.Max(startPoint.X, movePoint.X));
            var endY = Math.Min(gridFields.Height, Math.Max(startPoint.Y, movePoint.Y));
            Canvas.SetLeft(selectBorder, startX);
            Canvas.SetTop(selectBorder, startY);
            var width = endX - startX;
            var height = endY - startY;
            //绘制选择框
            if (width >= boxSelectWidth && height >= boxSelectWidth)
            {
                selectBorder.Width = width;
                selectBorder.Height = height;

                if (!selectCanvas.Children.Contains(selectBorder))
                    selectCanvas.Children.Add(selectBorder);
                if (!gridFields.Children.Contains(selectCanvas))
                {
                    Grid.SetRowSpan(selectCanvas, gridFields.RowDefinitions.Count);
                    Grid.SetColumnSpan(selectCanvas, gridFields.ColumnDefinitions.Count);
                    Panel.SetZIndex(selectCanvas, 5);
                    gridFields.Children.Add(selectCanvas);
                    gridFields.CaptureMouse();
                }
            }
            else
            {
                if (gridFields.Children.Contains(selectCanvas))
                {
                    gridFields.Children.Remove(selectCanvas);
                }
            }

        }

        private void GridFields_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

            gridFields.ReleaseMouseCapture();
            if (!gridFields.Children.Contains(selectCanvas))
            {
                return;
            }
            gridFields.Children.Remove(selectCanvas);
            if (!IsEnableBoxSelect)
                return;
            var endPoint = e.GetPosition(gridFields);
            var startX = Math.Max(0, Math.Min(startPoint.X, endPoint.X));
            var startY = Math.Max(0, Math.Min(startPoint.Y, endPoint.Y));
            var endX = Math.Min(gridFields.Width, Math.Max(startPoint.X, endPoint.X));
            var endY = Math.Min(gridFields.Height, Math.Max(startPoint.Y, endPoint.Y));
            //判断是否是框选
            if (endX - startX < boxSelectWidth || endY - startY < boxSelectWidth)
                return;
            int startRow = Math.Max(0, (int)(startY / dieHeight));
            int endRow = Math.Min((int)(endY / dieHeight), rowCount - 1);
            int startColumn = Math.Max(0, (int)(startX / dieWidth));
            int endColumn = Math.Min((int)(endX / dieWidth), columnCount - 1);
            List<Button> lstSelected = new List<Button>();
            for (int i = startRow; i <= endRow; i++)
            {
                for (int j = startColumn; j <= endColumn; j++)
                {
                    int id = GetId(i, j);
                    if (dicElments.ContainsKey(id))
                    {
                        lstSelected.Add(dicElments[id]);
                    }
                }
            }
            switch (EditMode)
            {
                case EnumEditMode.BoxSelect:
                    Select(lstSelected, true);
                    break;
                case EnumEditMode.MultiSelect:
                    Select(lstSelected, false);
                    break;
                case EnumEditMode.Check:
                    Check(lstSelected, false);
                    break;
            }
        }



        int _scaleIndex = 0;
        private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!IsEnableScale) return;
            //if (e.Delta > 0 && _scaleIndex <10) { _scaleIndex++; }
            //else if (e.Delta < 0 && _scaleIndex >-10) { _scaleIndex--; }
            //scale.ScaleX = scale.ScaleY = Math.Pow(1.1, _scaleIndex);
            #region ************中心点放大
            //// 定义放大和缩小的步长
            //double scaleStep = 0.1;

            //// 获取滚轮的方向
            //int delta = e.Delta;

            //// 根据滚轮方向调整放大倍率
            //if (delta > 0)
            //{
            //    // 放大
            //    scale.ScaleX += scaleStep;
            //    scale.ScaleY += scaleStep;
            //}
            //else if (delta < 0)
            //{
            //    // 缩小，确保放大倍率不小于0.1
            //    if (scale.ScaleX > 0.1)
            //    {
            //        scale.ScaleX -= scaleStep;
            //        scale.ScaleY -= scaleStep;
            //    }
            //}
            #endregion
            //**********当前位置放大***************
            // 获取鼠标相对于控件的位置
            Point mousePos = e.GetPosition(vb);
            // 计算鼠标相对于控件中心的偏移量
            double offsetX = mousePos.X - vb.ActualWidth / 2;
            double offsetY = mousePos.Y - vb.ActualHeight / 2;

            // 定义放大和缩小的步长
            double scaleStep = 0.1;

            // 获取滚轮的方向
            int delta = e.Delta;

            // 根据滚轮方向调整放大倍率
            if (delta > 0)
            {
                // 放大
                scale.ScaleX += scaleStep;
                scale.ScaleY += scaleStep;

                // 调整控件的位置，使鼠标位置保持不变
                vb.RenderTransformOrigin = new Point(0.5 + offsetX / vb.ActualWidth, 0.5 + offsetY / vb.ActualHeight);
            }
            else if (delta < 0)
            {
                // 缩小，确保放大倍率不小于0.1
                if (scale.ScaleX > 0.1)
                {
                    scale.ScaleX -= scaleStep;
                    scale.ScaleY -= scaleStep;

                    // 调整控件的位置，使鼠标位置保持不变
                    vb.RenderTransformOrigin = new Point(0.5 + offsetX / vb.ActualWidth, 0.5 + offsetY / vb.ActualHeight);
                }
            }
        }



        /// <summary>
        /// 检查是否能够上下移动Field
        /// </summary>
        /// <param name="rowDirection">上下移动的方向，-1表示向上移动，1表示向下移动，0则表示不进行上下移动</param>
        /// <param name="columnDirection">左右移动的方向，-1表示向左移动，1表示向右移动，0则表示不进行左右移动</param>
        /// <param name="minRow">选中的Field中最上面一行的行号</param>
        /// <param name="maxRow">选中的Field中最下面一行的行号</param>
        /// <param name="minColumn">选中的Field中最左边一列的列号</param>
        /// <param name="maxColumn">选中的Field中最右边一列的列号</param>
        /// <returns></returns>
        bool CheckRowMoveAble(int rowDirection, int minRow, int maxRow, int minColumn, int maxColumn)
        {
            if (rowDirection == 0) return true;

            for (int j = minColumn; j <= maxColumn; j += fieldColumnSpan)
            {

                //选中的Field中最上面或者最下面一行的行号，如果是向上移动，则取最上面的行号，如果是向下移动，则取最下行
                //遍历选中的Field的最上面一行或者最下面一行的所有Field,判断其上移后是否会与其它的Field重叠
                int rowi = rowDirection == 1 ? maxRow : minRow;
                var id = GetId(rowi, j);
                if (!dicElments.ContainsKey(id))
                    continue;
                int nextRow = rowi + fieldRowSpan * rowDirection;
                //判断是否会移出上下边界
                if (!CheckAddAble(nextRow, j))
                    return false;
                //if (nextRow < 0 || (nextRow + fieldRowSpan) * dieHeight > fieldDiameter)
                //    return false;
                for (int k = j - fieldColumnSpan + 1; k < j + fieldColumnSpan; k++)
                {

                    id = GetId(nextRow, k);
                    if (dicElments.ContainsKey(id))
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 检查是否能够左右移动Field
        /// </summary>
        /// <param name="columnDirection">左右移动的方向，-1表示向左移动，1表示向右移动，0则表示不进行左右移动</param>
        /// <param name="minRow">选中的Field中最上面一行的行号</param>
        /// <param name="maxRow">选中的Field中最下面一行的行号</param>
        /// <param name="minColumn">选中的Field中最左边一列的列号</param>
        /// <param name="maxColumn">选中的Field中最右边一列的列号</param>
        /// <returns></returns>
        bool CheckColumnMoveAble(int columnDirection, int minRow, int maxRow, int minColumn, int maxColumn)
        {
            if (columnDirection == 0) return true;

            for (int i = minRow; i <= maxRow; i += fieldRowSpan)
            {

                //选中的Field中最上面或者最下面一行的行号，如果是向上移动，则取最上面的行号，如果是向下移动，则取最下行
                //遍历选中的Field的最上面一行或者最下面一行的所有Field,判断其上移后是否会与其它的Field重叠
                int columni = columnDirection == 1 ? maxColumn : minColumn;
                var id = GetId(i, columni);
                if (!dicElments.ContainsKey(id))
                    continue;
                int nextColumn = columni + fieldRowSpan * columnDirection;
                //判断是否会移出上下边界
                if (!CheckAddAble(i, nextColumn))
                    return false;

                for (int k = i - fieldRowSpan + 1; k < i + fieldRowSpan; k++)
                {

                    id = GetId(k, nextColumn);
                    if (dicElments.ContainsKey(id))
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 检查是否能够移动Field
        /// </summary>
        /// <param name="rowDirection">上下移动的方向，-1表示向上移动，1表示向下移动，0则表示不进行上下移动</param>
        /// <param name="columnDirection">左右移动的方向，-1表示向左移动，1表示向右移动，0则表示不进行左右移动</param>
        /// <returns></returns>
        bool CheckMoveable(int rowDirection, int columnDirection)
        {
            int minRow = int.MaxValue;
            int minColumn = int.MaxValue;
            int maxRow = 0;
            int maxColumn = 0;


            //获取选中的区域的边界
            foreach (var btn in selectedBtns)
            {
                var fieldInfo = btn.DataContext as RowColumnInfo;
                if (fieldInfo.RowIndex > maxRow)
                {
                    maxRow = fieldInfo.RowIndex;
                }
                if (fieldInfo.RowIndex < minRow)
                {
                    minRow = fieldInfo.RowIndex;
                }

                if (fieldInfo.ColumnIndex > maxColumn)
                {
                    maxColumn = fieldInfo.ColumnIndex;
                }
                if (fieldInfo.ColumnIndex < minColumn)
                {
                    minColumn = fieldInfo.ColumnIndex;
                }
            }
            if (!CheckRowMoveAble(rowDirection, minRow, maxRow, minColumn, maxColumn))
                return false;
            if (!CheckColumnMoveAble(columnDirection, minRow, maxRow, minColumn, maxColumn))
                return false;

            return true;
        }
        /// <summary>
        /// 上下左右移动
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        void Move(int row, int column)
        {
            if (!CheckMoveable(row, column))
                return;
            var list = new List<Button>(selectedBtns);
            foreach (var selectedBtn in list)
            {
                Remove(selectedBtn);
            }
            foreach (var selectedBtn in list)
            {
                var field = selectedBtn.DataContext as RowColumnInfo;
                field = new RowColumnInfo(field.RowIndex + row, field.ColumnIndex + column);
                var button = AddField(field.RowIndex, field.ColumnIndex);
                Select(button);
            }
        }
        /// <summary>
        /// 向左移动1列
        /// </summary>
        public void MoveLeft()
        {
            Move(0, -1);
        }
        /// <summary>
        /// 向上移动1行
        /// </summary>
        public void MoveTop()
        {
            Move(-1, 0);
        }
        /// <summary>
        /// 向右移动1列
        /// </summary>
        public void MoveRight()
        {
            Move(0, 1);
        }
        /// <summary>
        /// 向下移动1行
        /// </summary>
        public void MoveDown()
        {
            Move(1, 0);
        }

        /// <summary>
        /// 更新十字架的位置
        /// </summary>
        /// <param name="pointOffset">与晶圆中心的偏移量</param>
        public void UpdateOffset(WcsPoint pointOffset)
        {

            pathCross.Margin = new Thickness(pointOffset.X * 2 * WaferMapInfo.CoefficientX, pointOffset.Y * 2 * WaferMapInfo.CoefficientY, 0, 0);
        }

        private double LimitOffset(double x, double gridSize, double ctlSize)
        {
            var size = gridSize - ctlSize;
            x = x * 2;
            if (Math.Abs(x) > size)
            {
                x = Math.Sign(x) * size;
            }
            return x;
        }
    }
}
