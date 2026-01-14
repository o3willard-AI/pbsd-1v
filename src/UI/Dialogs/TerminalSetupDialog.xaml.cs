using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PairAdmin.UI.Terminal;

namespace PairAdmin.UI.Dialogs;

/// <summary>
/// First-launch dialog for terminal configuration
/// </summary>
public partial class TerminalSetupDialog : Window
{
    private readonly TerminalSettings _settings;
    private bool _integratedAvailable;
    private bool _externalAvailable;

    /// <summary>
    /// Gets the selected terminal mode
    /// </summary>
    public TerminalMode SelectedMode { get; private set; } = TerminalMode.Integrated;

    /// <summary>
    /// Gets whether the user confirmed their selection
    /// </summary>
    public bool Confirmed { get; private set; }

    public TerminalSetupDialog()
    {
        InitializeComponent();
        _settings = TerminalSettings.Load();
        CheckAvailability();
        UpdateUI();
    }

    private void CheckAvailability()
    {
        _integratedAvailable = TerminalSettings.IsIntegratedAvailable();
        _externalAvailable = TerminalSettings.IsExternalAvailable();
    }

    private void UpdateUI()
    {
        // Update integrated status
        if (_integratedAvailable)
        {
            IntegratedStatusText.Text = "Available";
            IntegratedStatusText.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
        }
        else
        {
            IntegratedStatusText.Text = "Not Found";
            IntegratedStatusText.Foreground = new SolidColorBrush(Color.FromRgb(192, 57, 43));
            IntegratedRadio.IsEnabled = false;
        }

        // Update external status
        if (_externalAvailable)
        {
            var path = TerminalSettings.FindExternalPuTTY();
            ExternalStatusText.Text = "Found";
            ExternalStatusText.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
        }
        else
        {
            ExternalStatusText.Text = "Not Installed";
            ExternalStatusText.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));
        }

        // Select best available option
        if (_integratedAvailable)
        {
            IntegratedRadio.IsChecked = true;
            SelectOption(true);
        }
        else if (_externalAvailable)
        {
            ExternalRadio.IsChecked = true;
            SelectOption(false);
        }
    }

    private void SelectOption(bool integrated)
    {
        // Update visual selection
        var selectedBrush = new SolidColorBrush(Color.FromRgb(39, 174, 96));
        var normalBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70));

        IntegratedOption.BorderBrush = integrated ? selectedBrush : normalBrush;
        ExternalOption.BorderBrush = integrated ? normalBrush : selectedBrush;

        IntegratedRadio.IsChecked = integrated;
        ExternalRadio.IsChecked = !integrated;

        SelectedMode = integrated ? TerminalMode.Integrated : TerminalMode.External;
    }

    private void IntegratedOption_Click(object sender, MouseButtonEventArgs e)
    {
        if (_integratedAvailable || IntegratedRadio.IsEnabled)
        {
            SelectOption(true);
        }
    }

    private void ExternalOption_Click(object sender, MouseButtonEventArgs e)
    {
        SelectOption(false);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
        Close();
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate selection
        if (SelectedMode == TerminalMode.External && !_externalAvailable)
        {
            var result = MessageBox.Show(
                "PuTTY is not installed on your system.\n\n" +
                "You can:\n" +
                "- Install PuTTY from https://www.putty.org\n" +
                "- Choose 'Integrated Terminal' instead\n\n" +
                "Continue anyway?",
                "PuTTY Not Found",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;
        }

        // Save settings
        _settings.Mode = SelectedMode;
        _settings.Save();

        Confirmed = true;
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Show the dialog and return the selected mode
    /// </summary>
    public static TerminalMode? ShowSetupDialog()
    {
        var dialog = new TerminalSetupDialog();
        var result = dialog.ShowDialog();

        if (result == true && dialog.Confirmed)
        {
            return dialog.SelectedMode;
        }

        return null;
    }
}
