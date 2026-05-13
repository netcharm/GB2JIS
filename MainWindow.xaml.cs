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

    private void LocalSearchText(string text)
    {
        //System.Dia
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
            DstText.Text = CN2JA.ConvertChinese2Japanese(SrcText.Text);
        else if (sender == ConvertTextJ2C)
            SrcText.Text = CN2JA.ConvertJapanese2Chinese(DstText.Text);
        else if (sender == SearchTextCN)
            SrcText.Text = CN2JA.ConvertJapanese2Chinese(DstText.Text);
        else if (sender == SearchTextJA)
            SrcText.Text = CN2JA.ConvertJapanese2Chinese(DstText.Text);
    }

    private void CopyText_Click(object sender, RoutedEventArgs e)
    {
        //if (Clipboard.ContainsText() && !Clipboard.IsCurrent(dp))
        {
            Clipboard.SetText(DstText.Text);
        }
    }

}