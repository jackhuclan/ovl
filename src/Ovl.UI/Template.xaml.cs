using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Ovl.UI;

/// <summary>
/// Template.xaml 的交互逻辑
/// </summary>
public partial class Template : Window
{
    public Template()
    {
        InitializeComponent();
        BindDataList();
    }
    public void BindDataList()
    {
        List<DataTemp> dataTemps= new List<DataTemp>();
        dataTemps.Add(new DataTemp {
            Num=0,
            Name="Tese1",
            Status="Already set",
            TIS="--",
            X=12.12,
            Y=23.23
        });
        dataTemps.Add(new DataTemp
        {
            Num = 0,
            Name = "Tese1",
            Status = "Already set",
            TIS = "--",
            X = 12.12,
            Y = 23.23
        });
        dataTemps.Add(new DataTemp
        {
            Num = 0,
            Name = "Tese1",
            Status = "Already set",
            TIS = "--",
            X = 12.12,
            Y = 23.23
        });
        dataTemps.Add(new DataTemp
        {
            Num = 0,
            Name = "Tese1",
            Status = "Already set",
            TIS = "--",
            X = 12.12,
            Y = 23.23
        });
        gridArea.ItemsSource = dataTemps;
        //grid1.ItemsSource = dataTemps;
    }
}
public class DataTemp
{
    public int Num { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public string TIS { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}
