using Microsoft.Win32; // 文件顶部
using Social_Blade_Dashboard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Wallet_Payment;


namespace Wallet_Payment
{
    public class AppItem
    {
        public string Name { get; set; }
        public ImageSource Icon { get; set; }
    }
    public partial class MainWindow : Window
    {
        private UsageTracker tracker;
        private DispatcherTimer refreshTimer;
        private WebSocketService webSocketService;
        public class UsageItem
        {
            public string ProcessName { get; set; }
            public int Duration { get; set; }
            public System.Windows.Media.ImageSource Icon { get; set; }
            public string DurationText
            {
                get
                {
                    int hours = Duration / 3600;
                    int minutes = (Duration % 3600) / 60;
                    int seconds = Duration % 60;
                    if (hours > 0)
                        return $"{hours}小时{minutes}分{seconds}秒";
                    else if (minutes > 0)
                        return $"{minutes}分{seconds}秒";
                    else
                        return $"{seconds}秒";
                }
            }
        }

        public int MaxDuration { get; set; }

        public MainWindow()
        {
            DatabaseHelper.Init();
            InitializeComponent();
            DrawFocusStatsChart();
            this.Loaded += Window_Loaded;
            tracker = new UsageTracker();
            tracker.Start();

            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromSeconds(5);
            refreshTimer.Tick += (s, e) => LoadUsageData();
            refreshTimer.Start();

            // 启动WebSocket服务
            InitializeWebSocketService();
        }




        private void Border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUsageData();
            LoadWebTrackingData();
        }

        public static ImageSource GetAppIcon(string processName)
        {
            try
            {
                var proc = Process.GetProcessesByName(processName).FirstOrDefault();
                if (proc != null)
                {
                    string exePath = proc.MainModule.FileName;
                    return GetIconFromExePath(exePath);
                }
            }
            catch { }
            return new BitmapImage(new Uri("pack://application:,,,/MainWindowImage/slogan.png"));
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        public static ImageSource GetIconFromExePath(string exePath)
        {
            try
            {
                IntPtr hIcon = ExtractIcon(Process.GetCurrentProcess().Handle, exePath, 0);
                if (hIcon != IntPtr.Zero)
                {
                    var imageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(32, 32));
                    return imageSource;
                }
            }
            catch { }
            return null;
        }
        private void LoadUsageData()
        {
            // 获取当前进程名（不带.exe）
            string currentProcessName = Process.GetCurrentProcess().ProcessName;

            // 过滤掉本程序自己
            var usage = DatabaseHelper.GetTodayUsage()
                .Where(x => !string.Equals(x.ProcessName, currentProcessName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var all = usage.Select(x => new UsageItem
            {
                ProcessName = x.ProcessName,
                Duration = x.Duration,
                Icon = GetAppIcon(x.ProcessName)
            }).ToList();

            MaxDuration = all.Count > 0 ? all.Max(x => x.Duration) : 1;
            BarWidthConverter.MaxDuration = all.Count > 0 ? all.Max(x => x.Duration) : 1;
            UsageList.DataContext = this;
            UsageList.ItemsSource = all;
        }

        // 专注模式下拉
        private void FocusModeButton_Click(object sender, RoutedEventArgs e)
        {
            FocusModePopup.IsOpen = true;
        }

        private void FocusModeItem_Click(object sender, MouseButtonEventArgs e)
        {
            var tb = sender as TextBlock;
            FocusModeText.Text = tb.Text;
            FocusModePopup.IsOpen = false;
        }


        // 节奏下拉
        private void RhythmButton_Click(object sender, RoutedEventArgs e)
        {
            RhythmPopup.IsOpen = true;
        }

        private void RhythmItem_Click(object sender, MouseButtonEventArgs e)
        {
            var tb = sender as TextBlock;
            RhythmText.Text = tb.Text;
            RhythmPopup.IsOpen = false;
            // 根据选择更新时间
            if (tb.Text == "高效HIIT") { FocusTimeText.Text = "1"; RestTimeText.Text = "1"; }
            else if (tb.Text == "经典番茄钟") { FocusTimeText.Text = "25"; RestTimeText.Text = "5"; }
            else if (tb.Text == "超日节律") { FocusTimeText.Text = "90"; RestTimeText.Text = "20"; }
        }

        // 记录各模式累计专注时长（分钟）
        private Dictionary<string, int> focusStats = new Dictionary<string, int>
            {
                { "阅读", 0 },
                { "工作", 0 },
                { "电影", 0 },
                { "游戏", 0 },
                { "其他", 0 }
            };

        private string currentMode = "工作";
        private int currentSessionMinutes = 0;
        private DispatcherTimer focusTimer;
        private int focusSecondsLeft = 0;


        private bool focusActive = false; // 标记当前是否处于专注中
        private void StartFocusButton_Click(object sender, RoutedEventArgs e)
        {
            currentMode = FocusModeText.Text;
            int.TryParse(FocusTimeText.Text, out int focusMinutes);
            currentSessionMinutes = focusMinutes;
            focusSecondsLeft = focusMinutes; // 这里要乘60，单位是秒
            StartFocusButton.IsEnabled = false;
            StartFocusButton.Content = "专注中";
            focusActive = true; // 标记专注中

            focusTimer = new DispatcherTimer();
            focusTimer.Interval = TimeSpan.FromSeconds(1);
            focusTimer.Tick += FocusTimer_Tick;
            focusTimer.Start();

            DrawFocusStatsChart();

            outOfWhitelistSeconds = 0;
            monitorTimer = new DispatcherTimer();
            monitorTimer.Interval = TimeSpan.FromSeconds(1);
            monitorTimer.Tick += MonitorTimer_Tick;
            monitorTimer.Start();
        }

        private void FocusTimer_Tick(object sender, EventArgs e)
        {
            focusSecondsLeft--;
            if (focusSecondsLeft <= 0 && focusActive)
            {
                focusActive = false;
                focusTimer.Stop();
                monitorTimer.Stop();
                StartFocusButton.IsEnabled = true;
                StartFocusButton.Content = "开始专注";
                // 记录本次专注到数据库
                DatabaseHelper.AddFocusMinutes(currentMode, currentSessionMinutes);

                int focusMinutes = int.Parse(FocusTimeText.Text);
                int restMinutes = int.Parse(RestTimeText.Text);
                var drawCardWindow = new DrawCardWindow(restMinutes, focusMinutes);
                drawCardWindow.Show();
                DrawFocusStatsChart();
            }
        }





        private readonly string[] focusModes = { "阅读", "工作", "电影", "游戏", "自定义" };

        private void DrawFocusStatsChart()
        {
            var stats = DatabaseHelper.GetAllFocusStats();
            FocusStatsChart.Children.Clear();

            int max = stats.Values.Max();
            if (max == 0) max = 1;

            double barWidth = 24;
            double barSpacing = 36;
            double chartHeight = FocusStatsChart.Height;
            double baseLine = chartHeight - 40;

            for (int i = 0; i < focusModes.Length; i++)
            {
                string mode = focusModes[i];
                int value = stats.ContainsKey(mode) ? stats[mode] : 0;
                double height = (chartHeight - 60) * value / max;
                double x = 20 + i * (barWidth + barSpacing);

                // 柱子
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = barWidth,
                    Height = Math.Max(height, 6),
                    Fill = new LinearGradientBrush(
                        System.Windows.Media.Color.FromRgb(126, 217, 87),
                        System.Windows.Media.Color.FromRgb(60, 80, 40),
                        new System.Windows.Point(0.5, 0), new System.Windows.Point(0.5, 1)),
                    RadiusX = 6,
                    RadiusY = 6
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, baseLine - Math.Max(height, 6));
                FocusStatsChart.Children.Add(rect);

                // 上方分钟数
                var minText = new TextBlock
                {
                    Text = value.ToString(),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Width = barWidth + 4,
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(minText, x - 2);
                Canvas.SetTop(minText, baseLine - Math.Max(height, 6) - 22);
                FocusStatsChart.Children.Add(minText);

                // 下方模式名
                var label = new TextBlock
                {
                    Text = mode,
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 14,
                    Width = barWidth + barSpacing,
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(label, x - (barSpacing / 2));
                Canvas.SetTop(label, baseLine + 8);
                FocusStatsChart.Children.Add(label);
            }
        }

        public List<string> GetInstalledApps()
        {
            var apps = new List<string>();
            string[] registryKeys = {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    };

            foreach (string keyPath in registryKeys)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key == null) continue;
                    foreach (string subkeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                        {
                            var displayName = subkey?.GetValue("DisplayName") as string;
                            if (!string.IsNullOrEmpty(displayName) && !apps.Contains(displayName))
                                apps.Add(displayName);
                        }
                    }
                }
            }
            return apps;
        }

        public List<string> WhiteList = new List<string>();
        public List<AppItem> AllApps = new List<AppItem>();
        public List<AppItem> FilteredApps = new List<AppItem>();
        public string WhitelistDisplayText = "点击设置白名单"; // 可用属性绑定

        private void WhitelistBox_Click(object sender, MouseButtonEventArgs e)
        {
            if (AllApps.Count == 0)
                LoadInstalledApps();
            AppListBox.ItemsSource = null;
            AppListBox.ItemsSource = FilteredApps;
            WhitelistPopup.IsOpen = true;
        }

        private void WhitelistConfirm_Click(object sender, RoutedEventArgs e)
        {
            //WhiteList = AppListBox.SelectedItems.Cast<string>().ToList();
            //WhitelistPopup.IsOpen = false;
            //WhitelistTextBlock.Text = WhiteList.Count > 0 ? string.Join("、", WhiteList) : "点击设置白名单";

            var selected = AppListBox.SelectedItems.Cast<AppItem>().Select(a => a.Name).ToList();
            WhiteList = selected;
            WhitelistPopup.IsOpen = false;
            WhitelistTextBlock.Text = WhiteList.Count > 0 ? string.Join("、", WhiteList) : "点击设置白名单";
        }

        private DispatcherTimer monitorTimer;
        private int outOfWhitelistSeconds = 0;


        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            string activeApp = GetActiveWindowProcessName();
            if (WhiteList.Contains(activeApp))
            {
                outOfWhitelistSeconds = 0;
            }
            else
            {
                outOfWhitelistSeconds++;
                if (outOfWhitelistSeconds >= 30 && focusActive)
                {
                    focusActive = false;
                    monitorTimer.Stop();
                    if (focusTimer != null) focusTimer.Stop();
                    StartFocusButton.IsEnabled = true;
                    StartFocusButton.Content = "开始专注";
                    MessageBox.Show("已离开白名单应用超过60秒，专注已终止！");
                    // 不再统计本次专注时长
                }
            }
        }

        // 活跃窗口进程名
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public string GetActiveWindowProcessName()
        {
            IntPtr hwnd = GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out uint pid);
            try
            {
                var proc = Process.GetProcessById((int)pid);
                return proc.MainModule.ModuleName; // 或 proc.ProcessName
            }
            catch
            {
                return "";
            }
        }

        public void LoadInstalledApps()
        {
            AllApps.Clear();
            string[] registryKeys = {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    };

            foreach (string keyPath in registryKeys)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key == null) continue;
                    foreach (string subkeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                        {
                            var displayName = subkey?.GetValue("DisplayName") as string;
                            var exePath = subkey?.GetValue("DisplayIcon") as string;
                            if (!string.IsNullOrEmpty(displayName) && !AllApps.Any(a => a.Name == displayName))
                            {
                                ImageSource icon = null;
                                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                                {
                                    try
                                    {
                                        icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                            System.Drawing.Icon.ExtractAssociatedIcon(exePath).Handle,
                                            System.Windows.Int32Rect.Empty,
                                            System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(20, 20));
                                    }
                                    catch { }
                                }
                                AllApps.Add(new AppItem { Name = displayName, Icon = icon });
                            }
                        }
                    }
                }
            }
            FilteredApps = new List<AppItem>(AllApps);
        }

        private void AppSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = AppSearchBox.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(keyword))
                FilteredApps = new List<AppItem>(AllApps);
            else
                FilteredApps = AllApps.Where(a => a.Name.ToLower().Contains(keyword)).ToList();
            AppListBox.ItemsSource = FilteredApps;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }


        private async void InitializeWebSocketService()
        {
            try
            {
                webSocketService = new WebSocketService();
                webSocketService.OnTimeTrackingDataReceived += OnTimeTrackingDataReceived;
                await webSocketService.StartServerAsync();

                // 更新WebSocket状态显示
                UpdateWebSocketStatus();

                // 添加一些测试数据，确保UI能正常显示
                AddTestWebTrackingData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动WebSocket服务失败: {ex.Message}");
                // 不显示错误对话框，避免影响用户体验
                // 添加测试数据，确保UI能正常显示
                AddTestWebTrackingData();
                UpdateWebSocketStatus();
            }
        }

        private void UpdateWebSocketStatus()
        {
            try
            {
                if (webSocketService != null)
                {
                    string status = webSocketService.GetServerStatus();
                    WebSocketStatusText.Text = status;

                    if (status.Contains("✅"))
                    {
                        WebSocketStatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
                    }
                    else
                    {
                        WebSocketStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                    }
                }
                else
                {
                    WebSocketStatusText.Text = "❌ WebSocket服务未初始化";
                    WebSocketStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                WebSocketStatusText.Text = $"❌ 状态检查失败: {ex.Message}";
                WebSocketStatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
        }

        private void AddTestWebTrackingData()
        {
            try
            {
                // 添加一些测试数据，确保Web时间追踪区域能正常显示
                var testData = new TimeTrackingData
                {
                    Title = "测试网站",
                    Domain = "example.com",
                    Duration = "5"
                };

                DatabaseHelper.AddWebTimeTracking(testData);
                LoadWebTrackingData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加测试数据失败: {ex.Message}");
            }
        }

        private void OnTimeTrackingDataReceived(TimeTrackingData data)
        {
            try
            {
                // 将WebSocket接收到的数据保存到数据库
                DatabaseHelper.AddOrUpdateWebTimeTracking(data);

                // 刷新显示
                LoadUsageData();
                LoadWebTrackingData();

                // 可以在这里添加通知或其他处理逻辑
                Console.WriteLine($"✅ 已保存Web时间追踪数据: {data.Title} ({data.Domain})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 处理Web时间追踪数据时出错: {ex.Message}");
            }
        }

        private void LoadWebTrackingData()
        {
            try
            {
                var webTrackingData = DatabaseHelper.GetTodayWebTracking();
                Console.WriteLine($"📊 加载到 {webTrackingData.Count} 条Web时间追踪数据");

                if (webTrackingData.Count == 0)
                {
                    Console.WriteLine("📊 没有Web时间追踪数据，添加测试数据");
                    AddTestWebTrackingData();
                    webTrackingData = DatabaseHelper.GetTodayWebTracking();
                }

                WebTrackingList.ItemsSource = webTrackingData;
                Console.WriteLine($"✅ Web时间追踪数据已加载到UI");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 加载Web时间追踪数据时出错: {ex.Message}");
                // 如果出错，至少显示一些测试数据
                AddTestWebTrackingData();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 停止WebSocket服务
            webSocketService?.StopServer();
            base.OnClosed(e);
        }

        private async void TestWebSocketButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestWebSocketButton.IsEnabled = false;
                TestWebSocketButton.Content = "测试中...";

                // 获取当前WebSocket服务器URL
                string serverUrl = "ws://localhost:9000/";
                if (webSocketService != null && webSocketService.IsRunning())
                {
                    var status = webSocketService.GetServerStatus();
                    if (status.Contains("http://localhost:"))
                    {
                        var port = status.Split(':').Last().Split('/').First();
                        serverUrl = $"ws://localhost:{port}/";
                    }
                }

                await WebSocketTestClient.TestConnection(serverUrl);

                // 刷新状态
                UpdateWebSocketStatus();
                LoadWebTrackingData();

                TestWebSocketButton.Content = "测试完成";
                await Task.Delay(2000);
                TestWebSocketButton.Content = "测试连接";
            }
            catch (Exception ex)
            {
                TestWebSocketButton.Content = "测试失败";
                Console.WriteLine($"测试WebSocket连接失败: {ex.Message}");
            }
            finally
            {
                TestWebSocketButton.IsEnabled = true;
            }

        }

        private void AppStatsButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换到应用统计
            AppStatsButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7E, 0xD9, 0x57)); // #7ED957
            AppStatsButton.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x2E)); // #1A1A2E
            WebStatsButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2D, 0x2D, 0x2C)); // #2D2D2C
            WebStatsButton.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE0, 0xE0, 0xE0)); // #E0E0E0

            AppStatsContent.Visibility = Visibility.Visible;
            WebStatsContent.Visibility = Visibility.Collapsed;
        }

        private void WebStatsButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换到网站统计
            WebStatsButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7E, 0xD9, 0x57)); // #7ED957
            WebStatsButton.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x2E)); // #1A1A2E
            AppStatsButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2D, 0x2D, 0x2C)); // #2D2D2C
            AppStatsButton.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE0, 0xE0, 0xE0)); // #E0E0E0

            AppStatsContent.Visibility = Visibility.Collapsed;
            WebStatsContent.Visibility = Visibility.Visible;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {

        }
    }
}