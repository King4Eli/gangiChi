using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using toolsHelper;

namespace gangiChi
{
    public class G
    {
        public class Vuvu
        {
            public string? Textstr { get; set; }
            public string? Imagestr { get; set; } = "";
            public bool Imagevisible { get; set; } = false;
            public Color BgColor { get; set; } = Colors.MediumVioletRed;
            // "Sent", "Received" or "System" - drives bubble alignment and colors in Message.xaml
            public string Kind { get; set; } = "System";
            public static Vuvu AddTo(string text, string from)
            {
                if (text.Trim().EndsWith(".png", StringComparison.CurrentCultureIgnoreCase))
                {
                    //image
                    return new Vuvu { Textstr = "Tap image to download", Imagestr = text, Imagevisible = true, Kind = from };
                }
                else
                {   //text
                    return (new Vuvu { Textstr = text, Kind = from });
                }
            }
        }
        public static TrepCserver? Initiate_server { get; set; }
        public static ObservableCollection<Vuvu> ObservableCollection_Messages { get; set; } = [];
        public class TrepCserver
        {
            public TcpListener? Tcp_server { get; set; }
            public TcpClient? Tcp_client { get; set; }
            public NetworkStream? Stream { get; set; }

            public void ReceiveMessages(Action<string> funck)
            {
                byte[] buffer = new byte[1024];
                int bytesRead;
                if (Stream == null) return;
                try
                {
                    while ((bytesRead = Stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Debug.WriteLine(receivedMessage);
                        funck(receivedMessage);
                    }
                }
                catch (Exception ex)
                {
                    if (Tcp_client != null)
                    {
                        Application.Current?.Dispatcher.Dispatch(async () =>
                        {
                            var currentPage = Application.Current?.Windows[0].Page;
                            if (currentPage != null) await currentPage.DisplayAlert("", ex.Message, "ok");

                        });
                    }
                }
            }
            public void SendMessage(string message)
            {
                try
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        Stream?.Write(data, 0, data.Length);

                        G.ObservableCollection_Messages.Add(Vuvu.AddTo(message, "Sent"));

                    }
                }
                catch (Exception ex)
                {
                    if (Tcp_client != null)
                    {

                        Application.Current?.Dispatcher.Dispatch(async () =>
                        {
                            var currentPage = Application.Current?.Windows[0].Page;
                            if (currentPage != null)
                            {
                                await currentPage.DisplayAlert("", ex.Message, "ok");
                            }
                        });
                    }
                }
            }
            public void CloseConnection()
            {
                Stream?.Close();
                Tcp_client?.Close();
                Tcp_server?.Stop();
                G.Initiate_server = null;
            }

        }
    }

    public class B
    {
        public async static void DownloadShare(string? imageUrl)
        {

            if (imageUrl == null) return;
            // Get the Downloads directory for Android




            string folderPath = "/storage/emulated/0/Documents";


            using HttpClient client = new();
            try
            {
                var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                Debug.WriteLine($"Permission status: {status}");
                if (await Permissions.RequestAsync<Permissions.StorageWrite>() != PermissionStatus.Granted)
                {
                    Debug.WriteLine("Storage permission is required to save the file.");
                    return;
                }
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string downloadsPath = Path.Combine(folderPath, H.RandomString(26) + "_" + Path.GetFileName(imageUrl));
#if WINDOWS
            downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString(), H.RandomString(26) + "_"+ Path.GetFileName(imageUrl));
#endif



                if (!File.Exists(downloadsPath))
                {
                    byte[] data = await client.GetByteArrayAsync(imageUrl);
                    await File.WriteAllBytesAsync(downloadsPath, data);
                }


                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Share Image",
                    File = new ShareFile(downloadsPath, "image/png")
                });
            }
            catch (Exception ex)
            {
                Application.Current?.Dispatcher.Dispatch(async () =>
                {
                    var currentPage = Application.Current?.Windows[0].Page;
                    if (currentPage != null) await currentPage.DisplayAlert("", $"Error: {ex.Message}", "ok");

                });
            }
        }

        public static   string TakeScreenshotAsync(string folderLocation)
        {
            string filename = "";
#if WINDOWS
            var screenSize = DeviceDisplay.Current.MainDisplayInfo;
            var screenDimension = new System.Drawing.Size((int)screenSize.Width,
                                        (int)screenSize.Height);
            System.Drawing.Bitmap bitmap = new(screenDimension.Width, screenDimension.Height);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
            }

            filename = $"screenshot_{Guid.NewGuid():N}.png";
            string fullPath = Path.Combine(folderLocation, filename);
            bitmap.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
               
#endif
            Debug.WriteLine(filename);
            return filename;
        }


    }
}
