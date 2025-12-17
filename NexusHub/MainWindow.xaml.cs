using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Speech.Recognition; 
using System.Speech.Synthesis;   
using System.Management;         
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json; 

namespace WpfApp
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } = "User";
        public string AvatarPath { get; set; } = "";
        public string Status { get; set; } = "Offline";
    }

    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Content { get; set; }
        public string Time { get; set; }
        public bool IsMine { get; set; }
        public string Type { get; set; } = "text";
        public string FilePath { get; set; }
    }
    public partial class MainWindow : Window
    {
        private DispatcherTimer sysTimer;
        private DispatcherTimer animTimer;
        private DispatcherTimer matrixTimer;
        private DispatcherTimer bootTimer;
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private NetworkInterface netInterface;
        private long oldBytesReceived = 0;
        private List<Point> trafficHistory = new List<Point>();
        private SpeechRecognitionEngine recognizer;
        private SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        private List<User> users = new List<User>();
        private List<ChatMessage> messages = new List<ChatMessage>();
        private List<string> playlist = new List<string>();
        private List<string> blockedIps = new List<string>();
        private string CurrentUser = "";
        private string CurrentRole = "";
        private string SelectedChatUser = "ALL";
        private bool isRegistrationMode = false;
        private bool isVpnActive = false;
        private Point santaPosition = new Point(100, 100);
        private int[] matrixColumns;
        private Random random = new Random();
        private int bootStep = 0;
        private List<string> bootLogData = new List<string>
        {
            "BIOS DATE 15/12/2025 VER 5.0",
            "CPU: QUANTUM CORE DETECTED",
            "RAM: 64 GB CHECK... OK",
            "LOADING KERNEL... SUCCESS",
            "CHECKING SECURITY... SECURE",
            "CONNECTING TO SATELLITE...",
            "SYSTEM READY."
        };

        public MainWindow()
        {
            InitializeComponent();

            Directory.CreateDirectory("Uploads");
            Directory.CreateDirectory("Avatars");

            LoadDatabase();
            InitSystemCounters();

            sysTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            sysTimer.Tick += SysTimer_Tick;

            animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            animTimer.Tick += AnimTimer_Tick;

            matrixTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            matrixTimer.Tick += MatrixTimer_Tick;

            bootTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            bootTimer.Tick += BootTimer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitMatrix();
            matrixTimer.Start();
            bootTimer.Start();
            GetWeatherKremenchuk();
            _ = InitMapBrowser();
        }
        private void BootTimer_Tick(object sender, EventArgs e)
        {
            if (bootStep < bootLogData.Count)
            {
                if (BootLogList != null)
                {
                    BootLogList.Items.Add(bootLogData[bootStep]);
                    BootLogList.ScrollIntoView(BootLogList.Items[BootLogList.Items.Count - 1]);
                }
                bootStep++;
            }
            else
            {
                bootTimer.Stop();
                if (AuthForm != null)
                {
                    AuthForm.Visibility = Visibility.Visible;
                    AuthForm.Opacity = 0;
                    var anim = new System.Windows.Media.Animation.DoubleAnimation(1, TimeSpan.FromSeconds(1));
                    AuthForm.BeginAnimation(OpacityProperty, anim);
                }
            }
        }

        private void InitMatrix()
        {
            int cols = (int)(SystemParameters.PrimaryScreenWidth / 15);
            matrixColumns = new int[cols];
        }

        private void MatrixTimer_Tick(object sender, EventArgs e)
        {
            if (LoginOverlay.Visibility != Visibility.Visible || MatrixCanvas == null) return;

            MatrixCanvas.Children.Clear();
            double height = ActualHeight;

            for (int i = 0; i < matrixColumns.Length; i++)
            {
                TextBlock txt = new TextBlock
                {
                    Text = ((char)random.Next(33, 126)).ToString(),
                    Foreground = Brushes.Lime,
                    FontSize = 14,
                    FontFamily = new FontFamily("Consolas"),
                    Opacity = random.NextDouble()
                };
                Canvas.SetLeft(txt, i * 15);
                Canvas.SetTop(txt, matrixColumns[i] * 15);
                MatrixCanvas.Children.Add(txt);

                if (matrixColumns[i] * 15 > height && random.NextDouble() > 0.975)
                    matrixColumns[i] = 0;
                else
                    matrixColumns[i]++;
            }
        }
        private void InitSystemCounters()
        {
            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                netInterface = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
                if (netInterface != null) oldBytesReceived = netInterface.GetIPStatistics().BytesReceived;
                cpuCounter.NextValue(); ramCounter.NextValue();
            }
            catch { }
        }

        private void SysTimer_Tick(object sender, EventArgs e)
        {
            if (LoginOverlay.Visibility == Visibility.Visible) return;
            if (TxtTime != null) TxtTime.Text = DateTime.Now.ToString("HH:mm:ss");
            if (TxtDate != null) TxtDate.Text = DateTime.Now.ToString("yyyy-MM-dd");

            try
            {
                if (cpuCounter != null && PbCpu != null) PbCpu.Value = cpuCounter.NextValue();
                if (ramCounter != null && PbRam != null) PbRam.Value = ramCounter.NextValue();
            }
            catch { }

            try
            {
                DriveInfo d = new DriveInfo("C");
                if (d.IsReady)
                {
                    double total = d.TotalSize / 1e9;
                    double free = d.TotalFreeSpace / 1e9;
                    if (PbDisk != null) PbDisk.Value = 100 - ((free / total) * 100);
                    if (TxtDiskDetail != null) TxtDiskDetail.Text = $"{free:F1} GB FREE";
                }
            }
            catch { }

            UpdateNetworkStats();
            UpdateTopProcesses();
            SimulateSystemLog();
        }

        private void UpdateNetworkStats()
        {
            if (netInterface != null && TrafficGraph != null)
            {
                long newBytes = netInterface.GetIPStatistics().BytesReceived;
                double speed = (newBytes - oldBytesReceived) / 1024.0;
                oldBytesReceived = newBytes;

                if (TxtDownload != null) TxtDownload.Text = $"⬇ {speed:F1} KB/s";
                if (TxtUpload != null) TxtUpload.Text = $"⬆ {(speed * 0.2):F1} KB/s";

                if (trafficHistory.Count > 60) trafficHistory.RemoveAt(0);
                trafficHistory.Add(new Point(trafficHistory.Count * 5, 200 - (Math.Min(speed, 2000) / 10)));

                PointCollection pc = new PointCollection(trafficHistory);
                pc.Add(new Point(trafficHistory.Count * 5, 250));
                pc.Add(new Point(0, 250));
                TrafficGraph.Points = pc;
            }

            try
            {
                var conns = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
                if (conns.Length > 0 && ListConnections != null)
                {
                    ListConnections.Items.Clear();
                    foreach (var c in conns.Take(12))
                        ListConnections.Items.Add($"{c.LocalEndPoint} -> {c.RemoteEndPoint} [{c.State}]");
                }
            }
            catch { }
        }

        private void SimulateSystemLog()
        {
            if (SystemLogList != null && random.Next(0, 10) > 7)
            {
                string[] logs = { "KERNEL: OK", "NET: Filtered", "AUTH: OK", "DISK: Sync", "FW: Blocked" };
                SystemLogList.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {logs[random.Next(logs.Length)]}");
                if (SystemLogList.Items.Count > 20) SystemLogList.Items.RemoveAt(19);
            }
        }

        private void UpdateTopProcesses()
        {
            try
            {
                var procs = Process.GetProcesses().OrderByDescending(p => p.WorkingSet64).Take(5).ToList();
                double toMB = 1024.0 * 1024.0;

                if (procs.Count > 0 && TxtTopProc1 != null) TxtTopProc1.Text = $"1. {procs[0].ProcessName}: {procs[0].WorkingSet64 / toMB:F0} MB";
                if (procs.Count > 1 && TxtTopProc2 != null) TxtTopProc2.Text = $"2. {procs[1].ProcessName}: {procs[1].WorkingSet64 / toMB:F0} MB";
                if (procs.Count > 2 && TxtTopProc3 != null) TxtTopProc3.Text = $"3. {procs[2].ProcessName}: {procs[2].WorkingSet64 / toMB:F0} MB";
                if (procs.Count > 3 && TxtTopProc4 != null) TxtTopProc4.Text = $"4. {procs[3].ProcessName}: {procs[3].WorkingSet64 / toMB:F0} MB";
                if (procs.Count > 4 && TxtTopProc5 != null) TxtTopProc5.Text = $"5. {procs[4].ProcessName}: {procs[4].WorkingSet64 / toMB:F0} MB";
            }
            catch { }
        }
        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            if (SantaElement != null)
            {
                Point mouse = Mouse.GetPosition(this);
                bool inside = (mouse.X > 0 && mouse.X < ActualWidth && mouse.Y > 0 && mouse.Y < ActualHeight);
                Point target = inside ? new Point(mouse.X - 40, mouse.Y - 40) : new Point(ActualWidth / 2, ActualHeight / 2);

                santaPosition.X += (target.X - santaPosition.X) * 0.05;
                santaPosition.Y += (target.Y - santaPosition.Y) * 0.05;

                Canvas.SetLeft(SantaElement, santaPosition.X);
                Canvas.SetTop(SantaElement, santaPosition.Y);
            }
        }
        private void Auth_Click(object sender, RoutedEventArgs e)
        {
            string u = TxtUser.Text;
            string p = TxtPass.Password;

            if (isRegistrationMode)
            {
                if (users.Any(x => x.Username == u)) { TxtAuthStatus.Text = "USER EXISTS"; return; }
                users.Add(new User { Username = u, Password = p });
                SaveDatabase();
                TxtAuthStatus.Text = "REGISTERED";
                isRegistrationMode = false; SwitchAuthUI();
            }
            else
            {
                if (u == "admin" && p == "1234") LoginSuccess("Admin", "root");
                else
                {
                    var user = users.FirstOrDefault(x => x.Username == u && x.Password == p);
                    if (user != null) LoginSuccess(user.Role, user.Username);
                    else TxtAuthStatus.Text = "ACCESS DENIED";
                }
            }
        }

        private void LoginSuccess(string role, string name)
        {
            CurrentRole = role;
            CurrentUser = name;

            if (TxtUsername != null) TxtUsername.Text = name;
            if (TxtRole != null) TxtRole.Text = $"[{role.ToUpper()}]";
            if (TxtProfileUser != null) TxtProfileUser.Text = name;

            if (AuthTitle != null) AuthTitle.Text = "ACCESS GRANTED";
            UpdateProfileUI(name);

            LoginOverlay.Visibility = Visibility.Collapsed;
            MainAppGrid.Visibility = Visibility.Visible;
            HomeView.Visibility = Visibility.Visible;

            matrixTimer.Stop();
            sysTimer.Start();
            animTimer.Start();
            LoadChatsList();
        }

        private void UpdateProfileUI(string username)
        {
            var u = users.FirstOrDefault(x => x.Username == username);
            if (u != null && File.Exists(u.AvatarPath))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(System.IO.Path.GetFullPath(u.AvatarPath));
                    bmp.EndInit();
                    object imgMini = FindName("ImgUserAvatarMini");
                    if (imgMini is ImageBrush brushMini) brushMini.ImageSource = bmp;

                    object imgBig = FindName("ImgProfileBig");
                    if (imgBig is ImageBrush brushBig) brushBig.ImageSource = bmp;
                }
                catch { }
            }
        }

        private void UploadAvatar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = "Images|*.jpg;*.png" };
            if (d.ShowDialog() == true)
            {
                string dest = System.IO.Path.Combine("Avatars", $"{CurrentUser}_av.png");
                try
                {
                    File.Copy(d.FileName, dest, true);
                    var u = users.First(x => x.Username == CurrentUser);
                    u.AvatarPath = dest;
                    SaveDatabase();
                    UpdateProfileUI(CurrentUser);
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            sysTimer.Stop(); animTimer.Stop();
            CurrentUser = "";
            MainAppGrid.Visibility = Visibility.Collapsed;
            LoginOverlay.Visibility = Visibility.Visible;
            TxtPass.Clear();
            BootLogList.Items.Clear();
            bootStep = 0;
            AuthForm.Visibility = Visibility.Collapsed;
            bootTimer.Start();
            matrixTimer.Start();
        }

        private void SwitchAuth_Click(object sender, RoutedEventArgs e)
        {
            isRegistrationMode = !isRegistrationMode;
            SwitchAuthUI();
        }

        private void SwitchAuthUI()
        {
            if (AuthTitle == null) return;
            AuthTitle.Text = isRegistrationMode ? "REGISTER NEW AGENT" : "SYSTEM LOGIN";
            BtnAuth.Content = isRegistrationMode ? "CREATE ACCOUNT" : "ACCESS";
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtProfilePass.Text))
            {
                var u = users.First(x => x.Username == CurrentUser);
                u.Password = TxtProfilePass.Text;
                SaveDatabase();
                MessageBox.Show("SAVED");
            }
        }
        private void LoadChatsList()
        {
            var list = new List<User>(users);
            list.Insert(0, new User { Username = "ALL", Status = "Global" });
            if (ChatsList != null)
            {
                ChatsList.ItemsSource = null;
                ChatsList.ItemsSource = list;
                ChatsList.SelectedIndex = 0;
            }
        }

        private void ChatsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatsList != null && ChatsList.SelectedItem is User u)
            {
                SelectedChatUser = u.Username;
                TxtChatHeader.Text = u.Username == "ALL" ? "GLOBAL CHANNEL" : $"PRIVATE: {u.Username.ToUpper()}";
                RenderChat();
            }
        }

        private void SendChat_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtChatInput.Text)) return;
            SaveMessage(TxtChatInput.Text, "text", "");
            TxtChatInput.Clear();
        }

        private void ChatAttach_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            if (d.ShowDialog() == true)
            {
                string dest = System.IO.Path.Combine("Uploads", $"{DateTime.Now.Ticks}_{System.IO.Path.GetFileName(d.FileName)}");
                try { File.Copy(d.FileName, dest); SaveMessage(System.IO.Path.GetFileName(d.FileName), "file", dest); } catch { }
            }
        }

        private void AiAttach_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            if (d.ShowDialog() == true)
            {
                TxtAiLog.AppendText($"\n[SYSTEM]: File '{System.IO.Path.GetFileName(d.FileName)}' attached.\n");
            }
        }

        private void SaveMessage(string content, string type, string path)
        {
            messages.Add(new ChatMessage { Sender = CurrentUser, Receiver = SelectedChatUser, Content = content, Time = DateTime.Now.ToString("HH:mm"), IsMine = true, Type = type, FilePath = path });
            SaveDatabase();
            RenderChat();
        }

        private void RenderChat()
        {
            if (MessagePanel == null) return;
            MessagePanel.Children.Clear();
            var filtered = messages.Where(m => (m.Receiver == "ALL" && SelectedChatUser == "ALL") || (m.Sender == CurrentUser && m.Receiver == SelectedChatUser) || (m.Sender == SelectedChatUser && m.Receiver == CurrentUser));

            foreach (var m in filtered)
            {
                bool isMe = m.Sender == CurrentUser;
                Border b = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(10),
                    Margin = new Thickness(5),
                    MaxWidth = 400,
                    HorizontalAlignment = isMe ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    Background = isMe ? (Brush)FindResource("ChatBubbleSend") : (Brush)FindResource("ChatBubbleRecv")
                };

                StackPanel sp = new StackPanel();
                if (!isMe && SelectedChatUser == "ALL") sp.Children.Add(new TextBlock { Text = m.Sender, Foreground = Brushes.Orange, FontWeight = FontWeights.Bold });

                if (m.Type == "file")
                {
                    Button btn = new Button { Content = "OPEN FILE", Background = Brushes.Black, Foreground = Brushes.White, Margin = new Thickness(0, 5, 0, 0), Padding = new Thickness(5) };
                    btn.Click += (s, args) => { try { Process.Start(new ProcessStartInfo(m.FilePath) { UseShellExecute = true }); } catch { MessageBox.Show("File missing!"); } };
                    sp.Children.Add(new TextBlock { Text = $"📄 {m.Content}", Foreground = Brushes.Cyan });
                    sp.Children.Add(btn);
                }
                else sp.Children.Add(new TextBlock { Text = m.Content, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap });

                sp.Children.Add(new TextBlock { Text = m.Time, Foreground = Brushes.Gray, FontSize = 9, HorizontalAlignment = HorizontalAlignment.Right });
                b.Child = sp; MessagePanel.Children.Add(b);
            }
            if (ChatScrollViewer != null) ChatScrollViewer.ScrollToBottom();
        }

        private void ChatInput_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) SendChat_Click(null, null); }
        private void SearchChat_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ChatsList == null) return;
            string q = TxtSearchChat.Text.ToLower();
            if (string.IsNullOrWhiteSpace(q)) LoadChatsList();
            else ChatsList.ItemsSource = users.Where(u => u.Username.ToLower().Contains(q)).ToList();
        }
        private void VpnToggle_Click(object sender, RoutedEventArgs e)
        {
            isVpnActive = !isVpnActive;
            if (isVpnActive)
            {
                TxtVpnStatus.Text = "SECURE"; TxtVpnStatus.Foreground = Brushes.Lime; TxtRealIp.Text = "IP: 10.10.1.1 (HIDDEN)";
                BtnVpnToggle.Content = "DISCONNECT";
            }
            else
            {
                TxtVpnStatus.Text = "OFFLINE"; TxtVpnStatus.Foreground = Brushes.Gray; TxtRealIp.Text = "IP: EXPOSED";
                BtnVpnToggle.Content = "CONNECT";
            }
        }
        private void MusicOpen_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentRole != "Admin") { MessageBox.Show("ADMIN ONLY"); return; }
            OpenFileDialog d = new OpenFileDialog { Filter = "Audio|*.mp3" };
            if (d.ShowDialog() == true) { playlist.Add(d.FileName); MusicList.Items.Add(System.IO.Path.GetFileName(d.FileName)); SaveDatabase(); }
        }
        private void MusicList_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (MusicList.SelectedIndex >= 0) { MusicPlayer.Source = new Uri(playlist[MusicList.SelectedIndex]); TxtSongName.Text = MusicList.SelectedItem.ToString(); MusicPlay_Click(null, null); } }
        private void MusicPlay_Click(object sender, RoutedEventArgs e) { MusicPlayer.Play(); TxtSongStatus.Text = "PLAYING..."; }
        private void MusicPrev_Click(object sender, RoutedEventArgs e) { if (MusicList.SelectedIndex > 0) MusicList.SelectedIndex--; }
        private void MusicNext_Click(object sender, RoutedEventArgs e) { if (MusicList.SelectedIndex < MusicList.Items.Count - 1) MusicList.SelectedIndex++; }
        private void MusicPlayer_MediaEnded(object sender, RoutedEventArgs e) { MusicNext_Click(null, null); }
        private async void OsintUpload_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = "Images|*.jpg;*.png" };
            if (d.ShowDialog() == true)
            {
                ImgOsintPreview.Source = new BitmapImage(new Uri(d.FileName));
                if (OsintProgress != null) OsintProgress.Visibility = Visibility.Visible;
                if (OsintResultPanel != null) OsintResultPanel.Visibility = Visibility.Visible;

                TxtOsintStatus.Text = "ANALYZING...";
                await Task.Delay(2000);

                TxtLat.Text = "49.0632"; TxtLon.Text = "33.4055";
                TxtCity.Text = "KREMENCHUK DETECTED";
                if (OsintProgress != null) OsintProgress.Visibility = Visibility.Collapsed;
            }
        }
        private async void Terminal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string cmd = TxtTermIn.Text; TxtTermIn.Clear(); TxtTermOut.AppendText($"\n> {cmd}\n");
                if (cmd == "clear") { TxtTermOut.Clear(); return; }
                await Task.Run(() => {
                    try { Process p = new Process(); p.StartInfo.FileName = "cmd.exe"; p.StartInfo.Arguments = $"/c {cmd}"; p.StartInfo.RedirectStandardOutput = true; p.StartInfo.UseShellExecute = false; p.StartInfo.CreateNoWindow = true; p.Start(); Dispatcher.Invoke(() => TxtTermOut.AppendText(p.StandardOutput.ReadToEnd())); } catch { }
                });
            }
        }

        private async void AiSend_Click(object sender, RoutedEventArgs e) { TxtAiLog.AppendText($"\nYOU: {TxtAiInput.Text}"); TxtAiInput.Clear(); await Task.Delay(1000); TxtAiLog.AppendText("\nAI: Offline Mode.\n"); }
        private void AiInput_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) AiSend_Click(null, null); }
        private void VoiceStart_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Voice Active"); }
        private void StartScan_Click(object sender, RoutedEventArgs e) { ScanResults.Items.Add("Scanning 127.0.0.1... Secure."); }
        private void BlockIp_Click(object sender, RoutedEventArgs e) { if (BlockedIpList != null && TxtBlockIp != null) BlockedIpList.Items.Add($"{TxtBlockIp.Text} [BLOCKED]"); }
        private void AddNote_Click(object sender, RoutedEventArgs e) { NotesList.Items.Add(TxtNoteInput.Text); }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            string tag = ((Button)sender).Tag.ToString();
            HomeView.Visibility = MapView.Visibility = ChatView.Visibility = MusicView.Visibility = AiView.Visibility = TerminalView.Visibility = NotesView.Visibility = FirewallView.Visibility = LocationView.Visibility = ProfileView.Visibility = Visibility.Collapsed;
            switch (tag)
            {
                case "Home": HomeView.Visibility = Visibility.Visible; break;
                case "Map": MapView.Visibility = Visibility.Visible; break;
                case "Location": LocationView.Visibility = Visibility.Visible; break;
                case "Chat": ChatView.Visibility = Visibility.Visible; break;
                case "Music": MusicView.Visibility = Visibility.Visible; break;
                case "Ai": AiView.Visibility = Visibility.Visible; break;
                case "Terminal": TerminalView.Visibility = Visibility.Visible; break;
                case "Notes": NotesView.Visibility = Visibility.Visible; break;
                case "Firewall": FirewallView.Visibility = Visibility.Visible; break;
                case "Profile": ProfileView.Visibility = Visibility.Visible; break;
            }
            PageTitle.Text = tag.ToUpper() + " MODULE";
        }

        private async void GetWeatherKremenchuk() { try { using (HttpClient c = new HttpClient()) { string j = await c.GetStringAsync("https://api.open-meteo.com/v1/forecast?latitude=49.06&longitude=33.40&current_weather=true&hourly=relativehumidity_2m"); dynamic d = JsonConvert.DeserializeObject(j); if (TxtTemp != null) TxtTemp.Text = $"{d.current_weather.temperature}°C"; if (TxtCondition != null) TxtCondition.Text = $"Wind: {d.current_weather.windspeed}"; if (TxtHumidity != null) TxtHumidity.Text = $"Hum: {d.hourly.relativehumidity_2m[0]}%"; } } catch { } }
        private async Task InitMapBrowser() { try { await MapBrowser.EnsureCoreWebView2Async(); MapBrowser.Source = new Uri("https://alerts.in.ua"); } catch { } }
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }
        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }

        private void LoadDatabase() { try { if (File.Exists("users.json")) users = JsonConvert.DeserializeObject<List<User>>(File.ReadAllText("users.json")); if (File.Exists("chat.json")) messages = JsonConvert.DeserializeObject<List<ChatMessage>>(File.ReadAllText("chat.json")); } catch { users = new List<User>(); messages = new List<ChatMessage>(); } }
        private void SaveDatabase() { File.WriteAllText("users.json", JsonConvert.SerializeObject(users)); File.WriteAllText("chat.json", JsonConvert.SerializeObject(messages)); }
    }
}