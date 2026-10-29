using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using Marvel.DataModels.Recipe;
using ScottPlot.AxisLimitCalculators;

namespace Ovl.UI.Controls;

public class TargetSettingControl : UserControl
{
    public const double Max_Width = 250;
    public const double Max_Height = 250;
    public const double Default_Scale = 1;


    private Dictionary<System.Windows.Shapes.Shape, (bool isChecked, TargetLocation location)> Rects = new Dictionary<System.Windows.Shapes.Shape, (bool isChecked, TargetLocation location)>();
    public TargetSettingControl()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Root = new Canvas();

        Viewbox viewbox = new Viewbox();
        viewbox.Child = Root;
        Content = viewbox;

        SizeChanged += TargetSettingControl_SizeChanged;
    }

    private void TargetSettingControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Margin = new Thickness(Width / 2, Height / 2, Width / 2, Height / 4);
    }

    //public void Load()
    //{
    //    //DrawPanel();
    //}

    public Canvas Root { get; set; }





    /// <summary>
    /// 是否可以被勾选
    /// </summary>
    public bool IsCheckable
    {
        get { return (bool)GetValue(IsCheckableProperty); }
        set { SetValue(IsCheckableProperty, value); }
    }

    public static readonly DependencyProperty IsCheckableProperty =
        DependencyProperty.Register("IsCheckable",
                                    typeof(bool),
                                    typeof(TargetSettingControl),
                                    new PropertyMetadata(false,
                                    OnParameterChanged));

    private static void OnParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
    }

    /// <summary>
    /// 单选模式：每次只能选中一个 Target
    /// </summary>
    public bool IsSingleSelect
    {
        get { return (bool)GetValue(IsSingleSelectProperty); }
        set { SetValue(IsSingleSelectProperty, value); }
    }

    public static readonly DependencyProperty IsSingleSelectProperty =
        DependencyProperty.Register("IsSingleSelect", typeof(bool), typeof(TargetSettingControl),
            new PropertyMetadata(false));

    /// <summary>
    /// 单选模式下选中项变化时触发，参数为选中的 TargetLocation 对象
    /// </summary>
    public event EventHandler<TargetLocation> SelectionChanged;

    /// <summary>
    /// 选中对应 Target（单选模式）；按 TargetID(ToString) 匹配，避免 Clone 后引用不等导致选不中
    /// </summary>
    public void Select(TargetLocation target)
    {
        if (target == null) return;
        ClearAllTargetColor();
        foreach (var key in Rects.Keys.ToList())
            Rects[key] = (false, Rects[key].location);

        var targetId = target.ToString();
        foreach (var item in Rects.ToList())
        {
            if (item.Value.location != null && item.Value.location.ToString() == targetId)
            {
                item.Key.Fill = new SolidColorBrush(Colors.Green);
                Canvas.SetZIndex(item.Key, Root.Children.Count);
                Rects[item.Key] = (true, item.Value.location);
                break;
            }
        }
    }


    /// <summary>
    /// 绘画
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void Refresh(List<TargetLocation> targetPoints,
                        double fieldWidth,
                        double fieldHeight)
    {
        Root.Children.Clear();
        double minSize = 40;
        var originTargets = targetPoints;
        if (fieldWidth < minSize && fieldHeight < minSize)
        {
            double zoom = Math.Min(minSize / fieldWidth, minSize / fieldHeight);
            fieldWidth *= zoom;
            fieldHeight *= zoom;
            List<TargetLocation> newPoints = new List<TargetLocation>();
            foreach (var target in targetPoints)
            {
                var newTarget = (TargetLocation)CloneHelper.ReflectionClone(target);
                newTarget.R3Vector.X *= zoom;
                newTarget.R3Vector.Y *= zoom;
                newPoints.Add(newTarget);
            }
            targetPoints = newPoints;
        }

        //绘制Field形状
        DrawTarget.DrawTargetPanel(Root, Math.Abs(fieldWidth), Math.Abs(fieldHeight));

        var rects = DrawTarget.DrawTargets(targetPoints,
                                           Root,
                                           5,
                                           fieldWidth,
                                           fieldHeight);
        Rects.Clear();
        for (int i = 0; i < rects.Count(); i++)
        {
            Rects.Add(rects.ElementAt(i), (false, originTargets[i]));
        }
    }

    public void Check(List<TargetLocation> targetPoints)
    {
        if (!(targetPoints?.Any() ?? false)) return;
        foreach (var item in Rects)
        {
            foreach (var item1 in targetPoints)
            {
                if (item.Value.location.R3Vector.Equals(item1.R3Vector))
                {
                    Rects[item.Key] = (true, item.Value.location);
                    item.Key.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(112, 182, 3));
                }
            }
        }
    }



    #region MouseEvent
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        foreach (var item in Root.Children)
        {
            if (item is System.Windows.Shapes.Rectangle rect)
            {
                if (DrawTarget.IsInRect(e.GetPosition(Root),
                                        Canvas.GetLeft(rect),
                                        Canvas.GetTop(rect),
                                        rect.Width,
                                        rect.Height,
                                        Root))
                {
                    if (Rects.ContainsKey(rect) && IsCheckable)
                    {
                        if (IsSingleSelect)
                        {
                            ClearAllTargetColor();
                            foreach (var key in Rects.Keys.ToList())
                                Rects[key] = (false, Rects[key].location);
                            rect.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(112, 182, 3));
                            Rects[rect] = (true, Rects[rect].location);
                            SelectionChanged?.Invoke(this, Rects[rect].location);
                        }
                        else
                        {
                            rect.Fill = Rects[rect].Item1
                                ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 240, 124, 10))
                                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(112, 182, 3));
                            var rectInfo = Rects[rect];
                            Rects[rect] = (!rectInfo.isChecked, rectInfo.location);
                        }
                    }
                }
            }
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
    }
    #endregion

    #region check
    public IEnumerable<TargetLocation> GetCheckedFields() => Rects?.Where(_ => _.Value.Item1)
                                                                   .Select(_ => _.Value.Item2)?.ToList();

    #endregion
    public void MarkSelectedTargets(TargetLocation targetLocation)
    {
        if (targetLocation == null) return;
        ClearAllTargetColor();
        var selectedRects = Rects.Where(l => l.Value.location==targetLocation).ToList();
        foreach (var selectedRect in selectedRects)
        {
            Shape shape = selectedRect.Key;
            if (shape is System.Windows.Shapes.Rectangle rectangle)
            {
                rectangle.Fill = new SolidColorBrush(Colors.Green);
                // 将选中的矩形移动到最顶层
                Canvas.SetZIndex(rectangle, Root.Children.Count);
            }
        }
    }
    void ClearAllTargetColor()
    {
        foreach (var rect in Rects.Keys)
        {
            if (rect is System.Windows.Shapes.Rectangle rectangle)
            {
                rectangle.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255,255,255));
                Canvas.SetZIndex(rectangle, 0); // 恢复层级顺序
            }
        }
    }
}
