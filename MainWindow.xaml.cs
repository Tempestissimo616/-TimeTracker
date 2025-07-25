using Social_Blade_Dashboard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Wallet_Payment;
using Microsoft.Win32; // 文件顶部
using System.IO;


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
            return null;
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
            var usage = DatabaseHelper.GetTodayUsage();
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
            focusSecondsLeft = focusMinutes * 60; // 这里要乘60，单位是秒
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
                MessageBox.Show($"专注完成！本次专注 {currentSessionMinutes} 分钟");
                DrawFocusStatsChart();
            }
        }





        private readonly string[] focusModes = { "阅读", "工作", "电影", "游戏", "其他" };

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
    }
}