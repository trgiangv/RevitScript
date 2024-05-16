using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using HandyControl.Themes;

namespace RevitScript.UI.Views;

public partial class SelectFromList : Window
{
    public SelectFromList()
    {
        InitializeComponent();
        Resources.MergedDictionaries.Add(new Theme());
        Resources.MergedDictionaries.Add(new StandaloneTheme());
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(e.Uri.AbsoluteUri);
    }
}