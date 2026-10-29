using System.Windows;
using System.Windows.Input;

namespace Ovl.UI.Motion.View;

public partial class MotionView : Window
{
    public int ViewIndex { get; set; } = 0;

    public MotionView()
    {
        InitializeComponent();

        //! act view
        Activated += (s, e) =>
        {
            ActiveView();
        };
    }

    private void btnIO_Click(object sender, RoutedEventArgs e)
    {
        viewAxis.Visibility = Visibility.Collapsed;
        viewIO.Visibility = Visibility.Visible;

    }

    private void btnAxis_Click(object sender, RoutedEventArgs e)
    {
        viewAxis.Visibility = Visibility.Visible;
        viewIO.Visibility = Visibility.Collapsed;
    }
    private void btnMachine_Click(object sender, RoutedEventArgs e)
    {
        viewAxis.Visibility = Visibility.Collapsed;
        viewIO.Visibility = Visibility.Collapsed;
    }

    //! esc
    private void EscCommand(object sender, ExecutedRoutedEventArgs arg)
    {
        Close();
    }

    private void View_Loaded(object sender, RoutedEventArgs e)
    {
        ActiveView();
    }

    void ActiveView()
    {
        if (ViewIndex == 0)
        {
            viewAxis.Visibility = Visibility.Collapsed;
            viewIO.Visibility = Visibility.Visible;
        }
        else if (ViewIndex == 1)
        {
            viewAxis.Visibility = Visibility.Visible;
            viewIO.Visibility = Visibility.Collapsed;
        }
        else
        {
            //! invalid input
        }
    }
}
