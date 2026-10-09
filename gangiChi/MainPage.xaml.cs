 
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using toolsHelper; 

namespace gangiChi
{
    public partial class MainPage : ContentPage
    {
        string? ServerLocalIP;
        bool _isHosting;
        IDispatcherTimer? _connectionWatcher;
#if WINDOWS
        H.GlobalKeyboardHook Keyboard_Hook = new();
#endif
        private static readonly Random _rand = new ();
        static string? GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return null;
        }
        void IsLoader(bool g, View?[] viewToNotDisplay, string hg = "")
        {
            connectionStatus.IsVisible = g;
            loader.IsVisible = g;
            loader.IsRunning = g;
            loaderText.Text = hg;
            foreach (var item in viewToNotDisplay)
            {
                if (item != null) item.IsVisible = !g;
            }
        }

        // Begin watching the live connection; the status area is refreshed every 2s.
        // Used by both the host and the client.
        void StartConnectionWatcher()
        {
            _connectionWatcher?.Stop();
            _connectionWatcher = Dispatcher.CreateTimer();
            _connectionWatcher.Interval = TimeSpan.FromSeconds(2);
            _connectionWatcher.Tick += (_, _) => RefreshConnectionUi();
            _connectionWatcher.Start();
            RefreshConnectionUi();
        }

        // Periodic check: decide what the status area shows from the live connection.
        void RefreshConnectionUi()
        {
            try
            {
                var server = G.Initiate_server;
                bool connected = server?.Stream != null;

                if (connected)
                {
                    // An open connection exists → offer "Continue to messages" and a close (X).
                    connectionStatus.IsVisible = true;
                    loader.IsVisible = loader.IsRunning = false;
                    loaderText.Text = "Connected";
                    cancelHosting_Click.IsVisible = false;
                    continueToMessages_Click.IsVisible = true;
                    creatingConnection_Click.IsVisible = false;
                    client_click_btn.IsVisible = false;

                    connectionList.IsVisible = true;
                    connectionList.Clear();
                    connectionList.Add(BuildConnectionRow(server));
                    return;
                }

                if (_isHosting)
                {
                    // Still waiting for a device → keep the loader and the Cancel button.
                    connectionStatus.IsVisible = true;
                    loader.IsVisible = loader.IsRunning = true;
                    cancelHosting_Click.IsVisible = true;
                    continueToMessages_Click.IsVisible = false;
                    creatingConnection_Click.IsVisible = false;
                    client_click_btn.IsVisible = false;
                    connectionList.IsVisible = false;
                    connectionList.Clear();
                    return;
                }

                // No connection and not hosting → back to the home form.
                ResetConnectionUi();
            }
            catch (Exception ex) { Debug.WriteLine(ex); }
        }

        View BuildConnectionRow(G.TrepCserver? server)
        {
            var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            row.Add(new Label { Text = "Connected", VerticalTextAlignment = TextAlignment.Center }, 0, 0);
            var close = new Button
            {
                Text = "✕",
                BackgroundColor = Colors.Transparent,
                TextColor = (Application.Current?.Resources.TryGetValue("Primary", out var p) == true ? (Color)p : Colors.MediumVioletRed),
                Padding = 0,
                WidthRequest = 40
            };
            close.Clicked += (_, _) =>
            {
                try { server?.CloseConnection(); } catch (Exception ex) { Debug.WriteLine(ex); }
                RefreshConnectionUi();
            };
            row.Add(close, 1, 0);
            return row;
        }

        // Stop watching and restore the home form.
        void ResetConnectionUi()
        {
            _isHosting = false;
            _connectionWatcher?.Stop();
            _connectionWatcher = null;
            connectionStatus.IsVisible = false;
            loader.IsVisible = loader.IsRunning = false;
            cancelHosting_Click.IsVisible = false;
            continueToMessages_Click.IsVisible = false;
            connectionList.IsVisible = false;
            connectionList.Clear();
            creatingConnection_Click.IsVisible = true;
            client_click_btn.IsVisible = true;
        }

        // Cancel hosting / close an open connection and go back to the home form.
        private void CancelHosting_Click(object sender, EventArgs e)
        {
            try { G.Initiate_server?.CloseConnection(); } catch (Exception ex) { Debug.WriteLine(ex); }
            ResetConnectionUi();
        }

        // Both sides: move to the chat once a connection exists.
        private async void ContinueToMessages_Click(object sender, EventArgs e)
        {
            try
            {
                if (G.Initiate_server?.Stream == null)
                {
                    await DisplayAlert("Not connected", "The connection is no longer available.", "OK");
                    ResetConnectionUi();
                    return;
                }
                _connectionWatcher?.Stop();
                G.ObservableCollection_Messages = [];
                await Navigation.PushAsync(new Messages());
            }
            catch (Exception ex)
            {
                await DisplayAlert("", ex.Message, "OK");
            }
        }






        // Replace the NotificationRequestOptions with NotificationRequest
        static private void Initt()
        {
            return;

        }

        public MainPage()
        {
            InitializeComponent();

            this.Title = "gangiChi";
            Initt();
        }

        // Returning here (e.g. after a disconnect popped us home) → sync the UI to the live connection.
        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (G.Initiate_server?.Stream != null)
                StartConnectionWatcher();
            else
                ResetConnectionUi();
        }
        private async void Server_Connect_Click(object sender, EventArgs e)
        {

            if (!int.TryParse(servingFromPort.Text?.Trim(), out int serverPort) || serverPort is < 1 or > 65535)
            {
                await DisplayAlert("Invalid port", "Enter a port number between 1 and 65535.", "OK");
                servingFromPort.Focus();
                return;
            }

            ServerLocalIP = GetLocalIPAddress();

            if (ServerLocalIP == null)
            {
                await DisplayAlert("", "Error getting ip....", "ok");
                return;
            }

            //run without holding main thread
            _ = Task.Run(() =>
            {
                //close all connections before connecting
                G.Initiate_server?.CloseConnection();
                G.Initiate_server = new()
                {
                    Tcp_server = new(IPAddress.Parse(ServerLocalIP), serverPort)
                };
                var thisListener = G.Initiate_server.Tcp_server;
                thisListener?.Start();

                Application.Current?.Dispatcher.Dispatch(  () =>
                {
                    // loading .....
                    IsLoader(true, [creatingConnection_Click, client_click_btn], "Waiting for a device at " + ServerLocalIP + ":" + serverPort);
                    _isHosting = true;
                    StartConnectionWatcher();
                });
                while (true)
                {
                    //client is connected
                    TcpClient? Tcp_client;
                    try
                    {
                        Tcp_client = thisListener?.AcceptTcpClient();
                    }
                    catch (SocketException)
                    {
                        // listener was stopped (hosting cancelled) – leave the accept loop
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }

                    // if hosting was cancelled while we were waiting, stop here
                    if (!_isHosting || G.Initiate_server == null || !ReferenceEquals(G.Initiate_server.Tcp_server, thisListener))
                    {
                        Tcp_client?.Close();
                        break;
                    }

                    if (G.Initiate_server != null)
                    {
                        try
                        {
                            string desktopScreenShotFolderN = DesktopScreenShotFolder();
                            string fileServerUrl = Start_FileServer(desktopScreenShotFolderN);

                            G.Initiate_server.Stream = Tcp_client?.GetStream();
                            Application.Current?.Dispatcher.Dispatch(() => {
                                try
                                {
                                    this.Title += " s:" + fileServerUrl;

                                    //if windows
#if WINDOWS
                                    try {
                                        Keyboard_Hook.Unhook();
                                    } catch (Exception) {
                                        Keyboard_Hook=new();
                                    }
                                    Keyboard_Hook.Hook((t, j) =>
                                    {
                                        if (t == 0x0100 && j == 90){//key pressed && key is tab
                                            Application.Current?.Dispatcher.Dispatch(() => {
                                                string sc_dir =  B.TakeScreenshotAsync(desktopScreenShotFolderN);
                                                G.Initiate_server?.SendMessage(fileServerUrl + sc_dir);
                                            });
                                        }
                                     });
#endif
                                    // a device connected → the watcher now shows "Continue to messages"
                                    RefreshConnectionUi();
                                }
                                catch (Exception ex) { Debug.WriteLine(ex); }
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            Tcp_client?.Close();
                        }
                    }
                }
            });
        }
    
        private async void Client_Connect_Click(object sender, EventArgs e)
        { 
            if (string.IsNullOrWhiteSpace(connectToIpAddress.Text))
            {
                await DisplayAlert("IP address required", "Enter the host device’s local IP address.", "OK");
                connectToIpAddress.Focus();
                return;
            }
            if (!int.TryParse(connectToPort.Text?.Trim(), out int clientPort) || clientPort is < 1 or > 65535)
            {
                await DisplayAlert("Invalid port", "Enter a port number between 1 and 65535.", "OK");
                connectToPort.Focus();
                return;
            }

            IsLoader(true, [creatingConnection_Click, client_click_btn], "Connecting to " + connectToIpAddress.Text.Trim() + ":" + clientPort + "…");

            try
            { 
                G.Initiate_server?.CloseConnection();
                G.Initiate_server = new()
                {

                    //Tcp_client = new TcpClient("192.168.0.185", 5000)
                    Tcp_client = new TcpClient(connectToIpAddress.Text.Trim(), clientPort)
                };


                G.Initiate_server.Stream = G.Initiate_server.Tcp_client.GetStream();

                // connected → show "Continue to messages" (same as the host side)
                _isHosting = false;
                StartConnectionWatcher();
            }
            catch (Exception ex)
            {
                try { G.Initiate_server?.CloseConnection(); } catch (Exception c) { Debug.WriteLine(c); }
                ResetConnectionUi();
                await DisplayAlert("", "Keep trying till server comes online.\nError connecting to server: " + ex.Message, "OK");
            }
        }

        static private string Start_FileServer(string folder)
        {
            var ipAddress = IPAddress.Parse(GetLocalIPAddress() ?? "127.0.0.1");
            var port = GetAvailablePort();

            async Task AcceptClients(TcpListener server)
            {
                while (true)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
                }
            }

            async Task HandleClient(TcpClient client)
            {
                try
                {
                    using (client)
                    using (NetworkStream stream = client.GetStream())
                    using (StreamReader reader = new (stream, Encoding.UTF8))
                    using (StreamWriter writer = new (stream, new UTF8Encoding(false)) { AutoFlush = true })
                    {
                        // Read HTTP request headers
                        string requestLine = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(requestLine)) return;

                        // Parse request
                        string[] parts = requestLine.Split(' ');
                        if (parts.Length < 3 || parts[0] != "GET")
                        {
                            await WriteHttpResponse(writer, "400 Bad Request", "text/plain", "Bad Request");
                            return;
                        }

                        string requestedPath = parts[1];

                        // Skip remaining headers
                        string line;
                        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                        {
                            // Read until empty line (end of headers)
                        }

                        // Handle URL decoding and path security
                        string decodedPath = Uri.UnescapeDataString(requestedPath);
                        string relativePath = decodedPath.TrimStart('/');

                        // Security: prevent directory traversal
                        if (relativePath.Contains("..") || relativePath.Contains(':'))
                        {
                            await WriteHttpResponse(writer, "403 Forbidden", "text/plain", "Access denied");
                            return;
                        }

                        // Handle root path
                        if (string.IsNullOrEmpty(relativePath))
                        {
                            await WriteHttpResponse(writer, "200 OK", "text/html", GenerateDirectoryListing(folder, folder));
                            return;
                        }

                        string filePath = Path.Combine(folder, relativePath.Replace('/', Path.DirectorySeparatorChar));

                        if (File.Exists(filePath))
                        {
                            await ServeFile(writer, stream, filePath);
                        }
                        else if (Directory.Exists(filePath))
                        {
                            await WriteHttpResponse(writer, "200 OK", "text/html", GenerateDirectoryListing(filePath, relativePath));
                        }
                        else
                        {
                            await WriteHttpResponse(writer, "404 Not Found", "text/plain", "File not found");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Client handling error: {ex.Message}");
                }
            }

            async Task WriteHttpResponse(StreamWriter writer, string status, string contentType, string body)
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
                await writer.WriteLineAsync($"HTTP/1.1 {status}");
                await writer.WriteLineAsync($"Content-Type: {contentType}; charset=utf-8");
                await writer.WriteLineAsync($"Content-Length: {bodyBytes.Length}");
                await writer.WriteLineAsync("Connection: close");
                await writer.WriteLineAsync();
                await writer.BaseStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
            }

            async Task ServeFile(StreamWriter writer, Stream stream, string filePath)
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                string contentType = GetContentType(filePath);

                await writer.WriteLineAsync($"HTTP/1.1 200 OK");
                await writer.WriteLineAsync($"Content-Type: {contentType}");
                await writer.WriteLineAsync($"Content-Length: {fileBytes.Length}");
                await writer.WriteLineAsync("Connection: close");
                await writer.WriteLineAsync();
                await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
            }

            string GetContentType(string filePath)
            {
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                return extension switch
                {
                    ".html" or ".htm" => "text/html",
                    ".css" => "text/css",
                    ".js" => "application/javascript",
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".svg" => "image/svg+xml",
                    ".json" => "application/json",
                    ".txt" => "text/plain",
                    _ => "application/octet-stream"
                };
            }

            string GenerateDirectoryListing(string directoryPath, string relativePath)
            {
                var html = new StringBuilder();
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html><head><title>Directory Listing</title></head><body>");
                html.AppendLine($"<h1>Directory: /{relativePath}</h1>");
                html.AppendLine("<ul>");

                // Add parent directory link if not root
                if (!string.IsNullOrEmpty(relativePath))
                {
                    html.AppendLine($"<li><a href='../'>../</a></li>");
                }

                try
                {
                    // List directories
                    foreach (var dir in Directory.GetDirectories(directoryPath))
                    {
                        string dirName = Path.GetFileName(dir);
                        html.AppendLine($"<li><a href='{Uri.EscapeDataString(dirName)}/'>{dirName}/</a></li>");
                    }

                    // List files
                    var filesByDateDesc = new DirectoryInfo(directoryPath)
                    .GetFiles()
                    .OrderByDescending(f => f.LastWriteTime) // or f.CreationTime
                    .Select(f => f.FullName)
                    .ToArray();
                    foreach (var file in filesByDateDesc)
                    {
                        string fileName = Path.GetFileName(file);
                        html.AppendLine($"<li><a href='{Uri.EscapeDataString(fileName)}'>{fileName}</a></li>");
                    }
                }
                catch (Exception ex)
                {
                    html.AppendLine($"<li>Error reading directory: {ex.Message}</li>");
                }

                html.AppendLine("</ul>");
                html.AppendLine("</body></html>");
                return html.ToString();
            }

            TcpListener listener = new(ipAddress, port);
            listener.Start();
            Debug.WriteLine($"File server running at http://{ipAddress}:{port}/");
            Task.Run(() => AcceptClients(listener));
            return $"http://{ipAddress}:{port}/";
        }

        static int GetAvailablePort()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
        static string DesktopScreenShotFolder()
        {
            string downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    "KingOla",
                    H.RandomString(_rand.Next(20, 30))
                );
            if (!Directory.Exists(downloadsPath))
            {
                Directory.CreateDirectory(downloadsPath);
            }
            return Path.GetFullPath(downloadsPath);
        }
    }



}
