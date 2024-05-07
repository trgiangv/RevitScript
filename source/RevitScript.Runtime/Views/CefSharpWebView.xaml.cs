using System.Windows;

namespace RevitScript.Runtime.Views
{
    public partial class CefSharpWebView : Window
    {
        public CefSharpWebView(string address)
        {
            InitializeComponent();
            InitializeWindow();
            InitializeBrowser(address);
        }

        private void InitializeBrowser(string address)
        {
            Browser.TitleChanged += (s, e) =>
            {
                Title = $"{Browser.Title} - CefSharp: {CefSharp.Cef.CefSharpVersion}";
                if (Parent is Window window)
                {
                    window.Title = Title;
                }
            };
            Browser.Address = address;
        }

        public void SetAddress(string address)
        {
            Browser.Address = address;
        }

        #region InitializeWindow
        private void InitializeWindow()
        {
            MinHeight = 400;
            MinWidth = 600;
            SizeToContent = SizeToContent.WidthAndHeight;
            ShowInTaskbar = true;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            new System.Windows.Interop.WindowInteropHelper(this) { Owner = Autodesk.Windows.ComponentManager.ApplicationWindow };
        }
        #endregion
    }
}