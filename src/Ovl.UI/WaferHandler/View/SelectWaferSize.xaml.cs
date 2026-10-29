using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.EfemApi;

namespace Ovl.UI.WaferHandler.View;

public partial class SelectWaferSize : Window, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Prompt { get; }
    public string[] WaferSizes { get; }

    public WaferSize? SelectedWaferSize { get; private set; }

    private string? _selectedWaferSizeText;
    public string? SelectedWaferSizeText
    {
        get => _selectedWaferSizeText;
        private set
        {
            if (string.Equals(_selectedWaferSizeText, value, StringComparison.OrdinalIgnoreCase)) return;
            _selectedWaferSizeText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedWaferSizeText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanConfirm)));

            if (ConfirmCommand is RelayCommand rc)
                rc.NotifyCanExecuteChanged();
        }
    }

    public bool CanConfirm => !string.IsNullOrWhiteSpace(SelectedWaferSizeText);

    public ICommand ConfirmCommand { get; }

    private bool _allowClose;

    public SelectWaferSize()
    {
        InitializeComponent();
        Prompt = string.Empty;
        WaferSizes = Array.Empty<string>();
        ConfirmCommand = new RelayCommand(() => { }, () => false);
    }

    public SelectWaferSize(string prompt, params string[] waferSizes)
    {
        InitializeComponent();

        Prompt = prompt ?? string.Empty;

        var list = waferSizes?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();

        if (list.Length == 0)
            throw new ArgumentException("waferSizes is empty.", nameof(waferSizes));

        WaferSizes = list;

        ConfirmCommand = new RelayCommand(Confirm, () => CanConfirm);

        DataContext = this;

        Closing += OnClosing;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.RadioButton rb) return;
        var text = rb.Content?.ToString();
        if (string.IsNullOrWhiteSpace(text)) return;

        SelectedWaferSizeText = text;
    }

    private void Confirm()
    {
        if (!TryParseWaferSize(SelectedWaferSizeText, out var waferSize))
        {
            MessageBox.Show($"Invalid wafer size: {SelectedWaferSizeText}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SelectedWaferSize = waferSize;

        _allowClose = true;
        DialogResult = true;
        Close();
    }

    private static bool TryParseWaferSize(string? text, out WaferSize size)
    {
        size = WaferSize.None;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var s = text.Trim();

        if (Enum.TryParse(s, ignoreCase: true, out WaferSize enumSize) && enumSize != WaferSize.None)
        {
            size = enumSize;
            return true;
        }

        var digits = new string(s.Where(char.IsDigit).ToArray());
        if (!int.TryParse(digits, out var inch)) return false;

        size = inch switch
        {
            2 => WaferSize.INCH_2,
            4 => WaferSize.INCH_4,
            5 => WaferSize.INCH_5,
            6 => WaferSize.INCH_6,
            8 => WaferSize.INCH_8,
            12 => WaferSize.INCH_12,
            _ => WaferSize.None
        };

        return size != WaferSize.None;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F4 && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (CanConfirm)
            {
                Confirm();
                e.Handled = true;
            }
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
        }
    }
}
