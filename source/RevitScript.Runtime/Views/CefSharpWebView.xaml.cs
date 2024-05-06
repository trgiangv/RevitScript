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
                this.Title = $"{Browser.Title} - CefSharp: {CefSharp.Cef.CefSharpVersion}";
                if (this.Parent is Window window)
                {
                    window.Title = this.Title;
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
            this.MinHeight = 400;
            this.MinWidth = 600;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.ShowInTaskbar = true;
            this.ResizeMode = ResizeMode.CanResizeWithGrip;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            new System.Windows.Interop.WindowInteropHelper(this) { Owner = Autodesk.Windows.ComponentManager.ApplicationWindow };
        }
        #endregion
    }
}