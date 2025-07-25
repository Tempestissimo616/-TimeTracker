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


namespace Wallet_Payment
{
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

    }
}