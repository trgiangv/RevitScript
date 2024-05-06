using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using RevitScript.Runtime.Views;

namespace RevitScript.Runtime.Commands
{
    [Transaction(TransactionMode.Manual)]
    [UsedImplicitly]
    public class CefSharpCommand : ExternalCommand
    {
        static CefSharpWebView WebView { get; set; }

        public override void Execute()
        {
            OpenWebView("dcmvn.com");
        }

        private static void OpenWebView(string address)
        {
            WebView?.SetAddress(address);
            if (WebView is null)
            {
                WebView = new CefSharpWebView(address);
                WebView.Closed += (s, e) => { WebView = null; };
                WebView.Show();
            }
            WebView?.Activate();
        }
    }
}
