using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GB2JIS;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private DataObject? dp;

    private bool UseUnihan => Dispatcher.Invoke(() => (UseUnihanCheck.IsChecked ?? false) && CN2JA.UnihanValid);

    private void LocalSearchText(string text)
    {
        var cmd = "LocalSearch.exe";
        var opt = text;
        if (!System.IO.File.Exists(cmd) || string.IsNullOrEmpty(text)) return;
        
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = cmd,
            Arguments = opt,
            UseShellExecute = true
        });
    }

    private string[] SplitText(string text)
    {
        return (string.IsNullOrEmpty(text) ? [text] : text.Split(new[] { " ", "\n\r", "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CN2JA.InitGBJISTable();
        //Resources.
    }

    private void PasteText_Click(object sender, RoutedEventArgs e)
    {
        if (Clipboard.ContainsText() && (dp is null || !Clipboard.IsCurrent(dp)))
        {
            dp = Clipboard.GetDataObject() as DataObject;
            SrcText.Text = Clipboard.GetText();
        }
    }

    private void ConvertText_Click(object sender, RoutedEventArgs e)
    {
        if (sender == ConvertTextC2J)
            DstText.Text = UseUnihan ? CN2JA.UnihanChinese2Japanese(SrcText.Text) : CN2JA.ConvertChinese2Japanese(SrcText.Text);
        else if (sender == ConvertTextJ2C)
            SrcText.Text = UseUnihan ? CN2JA.UnihanJapanese2Chinese(SrcText.Text) : CN2JA.ConvertJapanese2Chinese(DstText.Text);
        else if (sender == SearchTextCN)
            LocalSearchText(string.Join(" ", SplitText(SrcText.Text)).Trim());
        else if (sender == SearchTextJA)
            LocalSearchText(string.Join(" ", SplitText(DstText.Text)).Trim());
    }

    private void CopyText_Click(object sender, RoutedEventArgs e)
    {
        //if (Clipboard.ContainsText() && !Clipboard.IsCurrent(dp))
        {
            Clipboard.SetText(DstText.Text);
        }
    }

}