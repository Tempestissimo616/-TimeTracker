using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using TimeDemo.Properties;

namespace TimeDemo
{
    public partial class MainForm : Form
    {
        private Panel navPanel;
        private Panel topTabPanel;
        private FlowLayoutPanel appListPanel;
        private Label todayTab;
        private System.Windows.Forms.Timer refreshTimer;
        private UsageTracker tracker;

        public MainForm()
        {
            InitializeComponent();
            DatabaseHelper.Init();
            tracker = new UsageTracker();
            tracker.Start();

            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(730, 530);
            this.BackColor = Color.FromArgb(20, 20, 20); // 深背景

            // 左侧导航栏
            navPanel = new Panel
            {
                BackColor = Color.FromArgb(25, 25, 25),
                Dock = DockStyle.Left,
                Width = 50
            };
            this.Controls.Add(navPanel);

            var navHome = new PictureBox
            {
                Image = Properties.Resources.dataDiagram,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(10, 30),
                Size = new Size(30, 30),
                BackColor = Color.Transparent
            };
            navPanel.Controls.Add(navHome);

            // 顶部Tab
            topTabPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(35, 35, 35)
            };
            this.Controls.Add(topTabPanel);

            todayTab = new Label
            {
                Text = "今日",
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 255, 100), // 酸绿色
                Location = new Point(110, 25),
                AutoSize = true,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            topTabPanel.Controls.Add(todayTab);

            // 主内容区
            appListPanel = new DoubleBufferedFlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                AutoScroll = true,
                Padding = new Padding(30, 90, 30, 20)
            };
            this.Controls.Add(appListPanel);
            appListPanel.BringToFront();
            topTabPanel.BringToFront();

            // 定时刷新
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5000;
            refreshTimer.Tick += (s, e) => RefreshUsage();
            refreshTimer.Start();

            RefreshUsage();
        }

        private void RefreshUsage()
        {
            var usageList = DatabaseHelper.GetTodayUsage();
            ShowUsage(usageList);
        }

        private void ShowUsage(List<(string ProcessName, int Duration)> usageList)
        {
            appListPanel.SuspendLayout();
            appListPanel.Controls.Clear();

            if (usageList.Count == 0)
            {
                appListPanel.ResumeLayout();
                return;
            }

            int totalSeconds = usageList.Sum(x => x.Duration);
            int fullBarWidth = 250;

            foreach (var item in usageList)
            {
                Image icon = GetIconForApp(item.ProcessName);
                var card = new AppUsageCard(icon, item.ProcessName, item.Duration, totalSeconds, fullBarWidth);
                appListPanel.Controls.Add(card);
            }

            appListPanel.ResumeLayout();
        }

        private Image GetIconForApp(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    var exePath = processes[0].MainModule.FileName;
                    Icon icon = Icon.ExtractAssociatedIcon(exePath);
                    return icon.ToBitmap();
                }
            }
            catch
            {
                // 无权限或错误
            }
            return SystemIcons.Application.ToBitmap();
        }

        private class AppUsageCard : UserControl
        {
            public AppUsageCard(Image icon, string appName, int seconds, int totalSeconds, int fullBarWidth)
            {
                this.Width = 420;
                this.Height = 60;
                this.BackColor = Color.FromArgb(30, 30, 30);
                this.Margin = new Padding(0, 0, 0, 18);
                this.Cursor = Cursors.Hand;

                this.MouseEnter += (s, e) =>
                {
                    this.BackColor = Color.FromArgb(50, 50, 50);
                    this.BorderStyle = BorderStyle.FixedSingle;
                };
                this.MouseLeave += (s, e) =>
                {
                    this.BackColor = Color.FromArgb(30, 30, 30);
                    this.BorderStyle = BorderStyle.None;
                };

                var pic = new PictureBox
                {
                    Image = icon,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Location = new Point(16, 10),
                    Size = new Size(40, 40),
                    BackColor = Color.Transparent
                };
                this.Controls.Add(pic);

                var nameLabel = new Label
                {
                    Text = appName,
                    Font = new Font("微软雅黑", 12, FontStyle.Bold),
                    Location = new Point(70, 10),
                    ForeColor = Color.White,
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                this.Controls.Add(nameLabel);

                // 灰色背景条
                var barBg = new Panel
                {
                    BackColor = Color.FromArgb(70, 70, 70),
                    Location = new Point(70, 35),
                    Size = new Size(fullBarWidth, 10)
                };
                this.Controls.Add(barBg);

                // 动态前景条：绿色渐变
                int minBar = 10;
                int barLength = totalSeconds > 0
                    ? Math.Max(minBar, (int)(fullBarWidth * (seconds / (float)totalSeconds)))
                    : 0;

                var barFg = new PictureBox
                {
                    Location = new Point(70, 35),
                    Size = new Size(barLength, 10),
                    BackColor = Color.Transparent
                };

                Bitmap bmp = new Bitmap(barLength, 10);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(0, 0, barLength, 10),
                        Color.FromArgb(0, 220, 100),
                        Color.FromArgb(0, 140, 60),
                        System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                    {
                        g.FillRectangle(brush, 0, 0, barLength, 10);
                    }
                }
                barFg.Image = bmp;
                this.Controls.Add(barFg);
                this.Controls.Add(barBg);

                // 时间标签
                var timeLabel = new Label
                {
                    Text = FormatTime(seconds),
                    Font = new Font("微软雅黑", 10),
                    Location = new Point(70 + fullBarWidth + 10, 28),
                    ForeColor = Color.White,
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                this.Controls.Add(timeLabel);

                // 圆角边框
                this.BorderStyle = BorderStyle.None;
                this.Region = System.Drawing.Region.FromHrgn(
                    NativeMethods.CreateRoundRectRgn(0, 0, this.Width, this.Height, 16, 16));
            }

            private string FormatTime(int seconds)
            {
                if (seconds >= 3600)
                    return $"{seconds / 3600}小时{(seconds % 3600) / 60}分";
                if (seconds >= 60)
                    return $"{seconds / 60}分钟{seconds % 60}秒";
                return $"{seconds}秒";
            }
        }

        internal class NativeMethods
        {
            [DllImport("gdi32.dll", SetLastError = true)]
            public static extern IntPtr CreateRoundRectRgn(
                int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
        }

        public class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
        {
            public DoubleBufferedFlowLayoutPanel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.SetStyle(ControlStyles.UserPaint, true);
                this.UpdateStyles();
            }
        }
    }
}
