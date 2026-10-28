using InteVega.CBB.Recipe.Ui.ViewModel;
using Microsoft.Extensions.DependencyInjection;
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

namespace InteVega.CBB.Recipe.Ui.View
{
    /// <summary>
    /// RecipeMenu.xaml 的交互逻辑
    /// </summary>
    public partial class RecipeMenu : UserControl
    {
        public RecipeMenu()
        {
            InitializeComponent();
			UseTheScrollViewerScrolling(treeView);
		}
		public void UseTheScrollViewerScrolling(FrameworkElement fElement)
		{
			fElement.PreviewMouseWheel += (sender, e) =>
			{
				var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
				eventArg.RoutedEvent = UIElement.MouseWheelEvent;
				eventArg.Source = sender;
				fElement.RaiseEvent(eventArg);
			};
		}

		private bool IsDrop = false;
		#region
		private Point _lastMouseDown;
		private void tvRequire_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			//if (e.ChangedButton == MouseButton.Left)
			//{
			//	var ele = e.OriginalSource as FrameworkElement;
			//	if (ele != null && ele.DataContext.GetType().ToString() == "InteVega.CBB.Recipe.Ui.ViewModel.MenuTreeModel")
			//	{
			//		_lastMouseDown = e.GetPosition(treeView);
			//		moveTreeItem = (MenuTreeModel)ele.DataContext;
			//	}
			//}
		}
		private MenuTreeModel moveTreeItem;
		private MenuTreeModel keyTreeItem;
		private void tvRequire_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Point currentPosition = e.GetPosition(treeView);
				if ((Math.Abs(currentPosition.X - _lastMouseDown.X) > 2.0) || (Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 2.0))
				{
					if (!IsDrop)
					{
						if (moveTreeItem != null)
						{
							IsDrop = true;
							this.treeView.Cursor = Cursors.Hand;
						}
					}
				}
			}
		}
		private void tvRequire_MouseUp(object sender, MouseEventArgs e)
		{
			if (moveTreeItem == null)
			{
				return;
			}
			var ele = e.OriginalSource as FrameworkElement;
			if (ele != null)
			{
                if (ele.DataContext.GetType().ToString() == "InteVega.CBB.Recipe.Ui.ViewModel.MenuTreeModel")
                {
					var targetTreeItem = (MenuTreeModel)ele.DataContext;
					if (targetTreeItem == null)
					{
						ClearTreeDrop();
						return;
					}
					if (targetTreeItem.Name != moveTreeItem.Name)
					{
						if (targetTreeItem.Level == 0)
						{
							if (targetTreeItem.IsAddIn)
							{
								if (targetTreeItem.CheckMove?.Invoke(targetTreeItem.RecipeInfo, moveTreeItem.RecipeInfo)??false) 
								{
									#region 移动到目标子集
									var tmp = targetTreeItem.Children.SingleOrDefault(l => l.Id == moveTreeItem.Id);
									if (tmp == null)
									{
										if (moveTreeItem.Parent != null)
										{
											moveTreeItem.Parent.Children.Remove(moveTreeItem);
											moveTreeItem.Parent = targetTreeItem;
											AddIn(targetTreeItem, moveTreeItem);
											moveTreeItem.MoveCompleteAction?.Invoke(moveTreeItem);
										}
									}
									targetTreeItem.IsAddIn = false;
									#endregion
								}
							}
							else if (targetTreeItem.IsAdd)
							{
								#region 移动到目标同级
								moveTreeItem.Parent.Children.Remove(moveTreeItem);
								Add(targetTreeItem.Parent, targetTreeItem, moveTreeItem);
								targetTreeItem.IsAdd = false;
								#endregion
							}
						}
					}
				}
			}
			ClearTreeDrop();
		}
		private void AddIn(MenuTreeModel argTarget, MenuTreeModel argMove)
		{
			if (argTarget != null)
			{
				var sortList = new List<MenuTreeModel>();
				sortList.Add(argMove);
				foreach (var item in argTarget.Children)
				{
					sortList.Add(item);
				}
				argTarget.Children.Clear();
				foreach (var item in sortList)
				{
					argTarget.Children.Add(item);
				}
			}
		}
		private void Add(MenuTreeModel argParent, MenuTreeModel argTarget, MenuTreeModel argMove)
		{
			if (argParent != null)
			{
				var sortList = new List<MenuTreeModel>();
				foreach (var item in argParent.Children)
				{
					sortList.Add(item);
					if (item.Id == argTarget.Id)
					{
						argMove.Parent = item.Parent;
						sortList.Add(argMove);
					}
				}
				argParent.Children.Clear();
				foreach (var item in sortList)
				{
					argParent.Children.Add(item);
				}
			}
		}
		private void tvRequire_MouseLeave(object sender, MouseEventArgs e)
		{
			ClearTreeDrop();
		}
		private void ClearTreeDrop()
		{
			moveTreeItem = null;
			IsDrop = false;
			this.treeView.Cursor = Cursors.Arrow;
		}

		private void Image_MouseEnter(object sender, MouseEventArgs e)
		{
			var ele = e.OriginalSource as Image;
			if (ele != null)
			{
				if (ele.DataContext.GetType().ToString() == "InteVega.CBB.Recipe.Ui.ViewModel.MenuTreeModel")
				{
					var ti = (MenuTreeModel)ele.DataContext;
					if (ti != null)
					{
						if (IsDrop)
						{
							ti.IsAdd = true;
						}
					}
				}
			}
		}
		private void Image_MouseLeave(object sender, MouseEventArgs e)
		{
			var ele = e.OriginalSource as Image;
			if (ele != null)
			{
				if (ele.DataContext.GetType().ToString() == "InteVega.CBB.Recipe.Ui.ViewModel.MenuTreeModel")
				{
					var ti = ele.DataContext as MenuTreeModel;
					if (ti != null)
					{
						ti.IsAdd = false;
					}
				}
			}
		}
		private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
		{
			var ele = e.OriginalSource as TextBlock;
			if (ele != null)
			{
				if (ele.DataContext.GetType().ToString() == "InteVega.CBB.Recipe.Ui.ViewModel.MenuTreeModel")
				{
					var ti = (MenuTreeModel)ele.DataContext;
					if (ti != null)
					{
						if (IsDrop)
						{
							ti.IsAddIn = true;
						}
					}
				}
			}
		}
		private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
		{
			var ele = e.OriginalSource as TextBlock;
			if (ele != null)
			{
				if (ele.DataContext.GetType().ToString() == "InteVega.CBB.Recipe.Ui.ViewModel.MenuTreeModel")
				{
					var ti = (MenuTreeModel)ele.DataContext;
					if (ti != null)
					{
						ti.IsAddIn = false;
					}
				}
			}
		}
        #endregion

        private void treeView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
			if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
			{
				var ele = e.OriginalSource as FrameworkElement;
				if (ele != null && ele.DataContext.GetType().ToString() == "InteVega.CBB.Recipe.Ui.ViewModel.MenuTreeModel")
				{
					keyTreeItem = (MenuTreeModel)ele.DataContext;
					keyTreeItem.CopyRecipeEvent?.Invoke(keyTreeItem.RecipeInfo);
				}
			}
			else if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control) 
			{
				var ele = e.OriginalSource as FrameworkElement;
				if (ele != null && ele.DataContext.GetType().ToString() == "InteVega.CBB.Recipe.Ui.ViewModel.MenuTreeModel")
				{
					keyTreeItem = (MenuTreeModel)ele.DataContext;
					keyTreeItem.PasteRecipeEvent?.Invoke(keyTreeItem.RecipeInfo);
				}
			}
		}

        private void treeView_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
			e.Handled = true;
		}
    }
}
