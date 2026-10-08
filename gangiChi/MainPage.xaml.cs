 
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
                G.Initiate_server.Tcp_server?.Start();

                Application.Current?.Dispatcher.Dispatch(  () =>
                {
                    // loading .....
                    IsLoader(true, [creatingConnection_Click, client_click_btn], "Waiting for a device at " + ServerLocalIP + ":" + serverPort);
                });
                while (true)
                {
                    //client is connected
                    TcpClient? Tcp_client = G.Initiate_server?.Tcp_server?.AcceptTcpClient();
                    if (G.Initiate_server != null)
                    {
                        string desktopScreenShotFolderN = DesktopScreenShotFolder();
                        string fileServerUrl = Start_FileServer(desktopScreenShotFolderN); 

                        G.Initiate_server.Stream = Tcp_client?.GetStream();
                        Application.Current?.Dispatcher.Dispatch(async () => {
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
                            //navigate to messages 
                            if (Navigation.NavigationStack.Count > 0) {
                                G.ObservableCollection_Messages = [];
                                await Navigation.PopToRootAsync();
                            }
                            await Navigation.PushAsync(new Messages());
                        });
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
                await Navigation.PushAsync(new Messages());


            }
            catch (Exception ex)
            {
                await DisplayAlert("", "Keep trying till server comes online.\nError connecting to server: " + ex.Message, "ok");
            }
            IsLoader(false, [creatingConnection_Click, client_click_btn], "waiting for client on\n");

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
