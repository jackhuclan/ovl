using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using InteVega.CBB.CommonLib.Helpers;
using Marvel.DataModels.Enums;

using Ovl.UI.WaferHandler.ViewModel;

namespace Ovl.UI.WaferHandler.View;

/// <summary>
/// WaferHandler600View.xaml 的交互逻辑
/// </summary>
public partial class WaferHandler600View : Window
{
    SlotItem _selectSlotItem;
    public WaferHandler600View()
    {
        InitializeComponent();
    }

    private void StackPanel_Drop(object sender, DragEventArgs e)
    {
        if (e.Handled == false)
        {
            StackPanel _panel = (StackPanel)sender; //取得当前panel对象
            UIElement _element = (UIElement)e.Data.GetData("Object");//移动对象转换成WPF核心基类对象

            if (_panel != null && _element != null)
            {
                //获取移动对象的父对象
                Panel _parent = (Panel)VisualTreeHelper.GetParent(_element);

                if (_parent != null)
                {
                    if ((e.KeyStates == DragDropKeyStates.ControlKey &&
                        e.AllowedEffects.HasFlag(DragDropEffects.Copy) || e.AllowedEffects.HasFlag(DragDropEffects.Move)))
                    {
                        _selectSlotItem?.CallbackDrag?.Invoke(_panel.Name, _selectSlotItem, ((SlotSource)_panel.Tag)?.SlotItem);
                        e.Effects = DragDropEffects.Copy;
                    }
                }
            }
        }
    }

    private void StackPanel_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("Object"))
        {
            if (e.KeyStates == DragDropKeyStates.ControlKey)
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.Move;
            }
        }
    }

    private void ItemsControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var ele = e.OriginalSource as FrameworkElement;
        if (ele.DataContext.GetType().ToString() == "Ovl.UI.WaferHandler.ViewModel.SlotSource")
        {
            _selectSlotItem = ((SlotSource)ele.DataContext).SlotItem;
        }
        else if (((SlotView)e.Source).Content.GetType().ToString() == "Ovl.UI.WaferHandler.ViewModel.SlotItem")
        {
            _selectSlotItem = (SlotItem)((SlotView)e.Source).Content;
        }
    }

    private void EscCommand(object sender, ExecutedRoutedEventArgs arg)
    {
        Close();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var waferHandler600ViewModel = DataContext as WaferHandler600ViewModel;
        if (waferHandler600ViewModel == null) return;
        waferHandler600ViewModel.ViewLoaded();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is FrameworkElement frameworkElement && frameworkElement.DataContext is WaferHandler600ViewModel viewModel && !viewModel.CanClose)
        {
            e.Cancel = true;
        }
        LogHelper.LogInfo($"WaferHandler View Closing, e.Cancel={e.Cancel}");
    }

    private void Slot_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        // 先清理上一次残留的菜单
        if (element.ContextMenu != null)
        {
            element.ContextMenu.IsOpen = false;
            element.ContextMenu = null;
        }

        var slotItem = ResolveSlotItem(e);
        LogHelper.LogInfo($"Slot_MouseRightButtonUp, slotItem.Value={slotItem?.Value} slotItem?.Status={slotItem?.Status}");
        if (slotItem == null || string.IsNullOrWhiteSpace(slotItem.Value) || slotItem.Status == EnumSlotStatus.Empty)
        {
            return;
        }

        _selectSlotItem = slotItem;

        var menu = BuildContextMenu(slotItem);
        if (menu == null || menu.Items.Count == 0)
        {
            return;
        }

        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(element.ContextMenu, menu))
            {
                element.ContextMenu = null;
            }
        };

        element.ContextMenu = menu;
        menu.PlacementTarget = element;
        menu.IsOpen = true;
        e.Handled = true;
    }
    private SlotItem ResolveSlotItem(MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement element && element.DataContext is SlotSource slotSource)
        {
            return slotSource.SlotItem;
        }

        if (e.Source is SlotView slotView && slotView.Content is SlotItem slotItem)
        {
            return slotItem;
        }

        return null;
    }
    private ContextMenu BuildContextMenu(SlotItem sourceSlotItem)
    {
        var menu = new ContextMenu();

        if (IsLoadPort(sourceSlotItem.PortNo))
        {
            AddMenuItem(menu, "Move To Stage", () => sourceSlotItem.CallbackDrag?.Invoke(
               "StagePanel",
               sourceSlotItem,
               CreateTargetSlotItem(EnumSlotSource.Stage, StagePanel.Name, sourceSlotItem.SourcePortNo)));

            AddMenuItem(menu, "Move To PA", () => sourceSlotItem.CallbackDrag?.Invoke(
                "AlignerPanel",
                sourceSlotItem,
                CreateTargetSlotItem(EnumSlotSource.Aligner, AlignerPanel.Name, sourceSlotItem.SourcePortNo)));
            return menu;
        }

        if (sourceSlotItem.PortNo == EnumSlotSource.Stage)
        {
            AddMenuItem(menu, "Move To LP", () => sourceSlotItem.CallbackDrag?.Invoke("", sourceSlotItem, null));

            AddMenuItem(menu, "Move To PA", () => sourceSlotItem.CallbackDrag?.Invoke(
                "AlignerPanel",
                sourceSlotItem,
                CreateTargetSlotItem(EnumSlotSource.Aligner, AlignerPanel.Name, sourceSlotItem.SourcePortNo)));
            return menu;
        }

        if (sourceSlotItem.PortNo == EnumSlotSource.Aligner)
        {
            AddMenuItem(menu, "Move To LP", () => sourceSlotItem.CallbackDrag?.Invoke("", sourceSlotItem, null));

            AddMenuItem(menu, "Move To Stage", () => sourceSlotItem.CallbackDrag?.Invoke(
                "StagePanel",
                sourceSlotItem,
                CreateTargetSlotItem(EnumSlotSource.Stage, StagePanel.Name, sourceSlotItem.SourcePortNo)));
            AddEditWaferInfoMenuItem(menu, sourceSlotItem);
            return menu;
        }

        return menu;
    }


    private void AddMenuItem(ContextMenu menu, string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) =>
        {
            action();
            menu.IsOpen = false;
        };
        menu.Items.Add(item);
    }

    private static bool IsLoadPort(EnumSlotSource source)
    {
        return source == EnumSlotSource.Port1
            || source == EnumSlotSource.Port2
            || source == EnumSlotSource.Port3;
    }
    private SlotItem CreateTargetSlotItem(EnumSlotSource targetPortNo, string targetName, EnumSlotSource sourcePortNo)
    {
        return new SlotItem
        {
            Id = Guid.NewGuid().ToString(),
            MinIndex = 1,
            MaxIndex = 1,
            Value = targetName,
            PortNo = targetPortNo,
            SourcePortNo = sourcePortNo,
            Status = EnumSlotStatus.Empty
        };
    }
    private void AddEditWaferInfoMenuItem(ContextMenu menu, SlotItem sourceSlotItem)
    {
        if (DataContext is not WaferHandlerViewModel viewModel)
        {
            return;
        }

        menu.Items.Add(new Separator());

        var item = new MenuItem { Header = "Edit Wafer Info" };
        item.Click += (_, _) =>
        {
            if (viewModel.EditStationWaferInfoCommand.CanExecute(sourceSlotItem))
            {
                viewModel.EditStationWaferInfoCommand.Execute(sourceSlotItem);
            }
            menu.IsOpen = false;
        };
        menu.Items.Add(item);
    }
}
