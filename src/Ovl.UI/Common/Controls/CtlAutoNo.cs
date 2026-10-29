using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ovl.UI.Common.Controls;

/// <summary>
/// 表格行号控件（替代厂内 InteVega.CBB.ComUI.Controls.CtlAutoNo）。
///
/// 用法与原版一致：放在 DataGridTemplateColumn 的 CellTemplate 内，
/// <code>&lt;controls:CtlAutoNo AncestorType="{x:Type DataGrid}" /&gt;</code>
/// 控件沿可视树向上找到 <see cref="AncestorType"/> 指定的列表控件，再定位所在
/// <see cref="DataGridRow"/>，显示其 1 基序号；虚拟化回收后重新计算。
/// </summary>
public sealed class CtlAutoNo : TextBlock
{
    /// <summary>用于定位行容器的祖先控件类型（通常为 DataGrid）。</summary>
    public static readonly DependencyProperty AncestorTypeProperty =
        DependencyProperty.Register(
            nameof(AncestorType),
            typeof(Type),
            typeof(CtlAutoNo),
            new PropertyMetadata(typeof(DataGrid), OnAncestorTypeChanged));

    public Type? AncestorType
    {
        get => (Type?)GetValue(AncestorTypeProperty);
        set => SetValue(AncestorTypeProperty, value);
    }

    public CtlAutoNo()
    {
        Loaded += (_, _) => Refresh();
        DataContextChanged += (_, _) => Refresh();
    }

    private static void OnAncestorTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((CtlAutoNo)d).Refresh();

    private void Refresh()
    {
        var itemsControl = FindAncestor<ItemsControl>(this, AncestorType ?? typeof(DataGrid));
        var row = FindAncestor<DataGridRow>(this, typeof(DataGridRow));
        if (itemsControl is null || row is null)
        {
            return;
        }

        var index = itemsControl.ItemContainerGenerator.IndexFromContainer(row);
        Text = index >= 0 ? (index + 1).ToString() : string.Empty;
    }

    private static T? FindAncestor<T>(DependencyObject? start, Type ancestorType)
        where T : DependencyObject
    {
        var current = start;
        while (current is not null)
        {
            if (current is T match && ancestorType.IsInstanceOfType(current))
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
        }

        return null;
    }
}
