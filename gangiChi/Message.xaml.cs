 
using System.Runtime.InteropServices; 
using toolsHelper; 


namespace gangiChi;
public partial class Messages : ContentPage
{
    bool _closed;

    public Messages()
    {
        InitializeComponent();

        G.ObservableCollection_Messages.Add(new G.Vuvu { Textstr = "Connected", Kind = "System" });
        listerbox.ItemsSource = G.ObservableCollection_Messages;
        Task.Run(() =>
        {
            G.Initiate_server?.ReceiveMessages((cu) =>
            {
                var recii = G.Vuvu.AddTo(cu, "Received");
                try { if (recii.Imagevisible) { B.DownloadShare(recii.Imagestr); } } catch (Exception c) {_=c; }
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        // collection is bound to the UI, so it must change on the main thread (WinUI throws otherwise)
                        G.ObservableCollection_Messages.Add(recii);
                        listerbox.ScrollTo(G.ObservableCollection_Messages.Count - 1, position: ScrollToPosition.End, animate: true);
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
                });
            },
            // the connection ended (remote closed or dropped) → return home safely
            onClosed: HandleDisconnected);
        });
    }

    // Close the connection and go back to the home page, at most once, without crashing.
    void HandleDisconnected()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_closed) return;
            _closed = true;
            try { G.Initiate_server?.CloseConnection(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            try
            {
                if (Navigation.NavigationStack.Count > 1)
                    await Navigation.PopToRootAsync();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        });
    }

    private void Image_MouseLeftButtonDown(object sender, TappedEventArgs e)
    {

        var param = e.Parameter as G.Vuvu;
        string? imageUrl = param?.Imagestr;
        B.DownloadShare(imageUrl);
    }

    private void Sendmessages_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(texttosend.Text))
            {
                bool ok = G.Initiate_server?.SendMessage(texttosend.Text) ?? false;
                texttosend.Text = "";
                if (!ok)
                {
                    // send failed → the connection is broken, head home
                    HandleDisconnected();
                    return;
                }
                listerbox.ScrollTo(G.ObservableCollection_Messages.Count - 1, position: ScrollToPosition.End, animate: true);
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }


















    // Constants for Windows API
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    private void Hide_btn_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (G.Initiate_server?.Tcp_server != null && sender is Button button)
            {
#if WINDOWS
                const int GWL_EXSTYLE = -20;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                var mauiContext = Application.Current?.Windows[0]?.Handler?.MauiContext;
                var window = mauiContext?.Services.GetService<Microsoft.UI.Xaml.Window>();
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    
                    ShowWindow(hwnd, 0); // SW_HIDE
                    int currentStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    SetWindowLong(hwnd, GWL_EXSTYLE, currentStyle | WS_EX_TOOLWINDOW);
                }
#endif
            }
        }
        catch (Exception ez) { _ = ez; }
    }
}

