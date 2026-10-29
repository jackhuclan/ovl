using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Ovl.UI.Controls;

/// <summary>
/// 曝光场及其内部Die的布局控件
/// </summary>
public class FieldElement : FrameworkElement
{
    public FieldElement(int rowSpan, int columnSpan)
    {
        _rowSpan = Math.Max(1, rowSpan);
        _columnSpan = Math.Max(1, columnSpan);
    }
    int _rowSpan = 1;
    int _columnSpan = 1;

    protected override void OnRender(DrawingContext drawingContext)
    {
        Pen pen = new Pen(Brushes.Green, 1);
        base.OnRender(drawingContext);
        double divX = this.ActualWidth / _columnSpan;
        double divY = this.ActualHeight / _rowSpan;
        drawingContext.PushOpacity(0.8);
        for (int i = 1; i < _rowSpan; i++)
        {
            double y = divY * i;
            drawingContext.DrawLine(pen, new Point(0, y), new Point(this.ActualWidth, y));
        }
        for (int j = 1; j < _columnSpan; j++)
        {
            double x = divX * j;
            drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, this.ActualHeight));
        }
        drawingContext.Pop();
    }


}
