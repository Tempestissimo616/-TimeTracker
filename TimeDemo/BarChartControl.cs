using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TimeDemo
{
    public class BarChartControl : Control
    {
        public List<(string ProcessName, int Duration)> Data { get; set; } = new List<(string, int)>();

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Data == null || Data.Count == 0) return;

            int totalDuration = 1;
            foreach (var item in Data)
                totalDuration += item.Duration;

            int barHeight = 40;
            int spacing = 10;
            int y = 10;

            foreach (var item in Data)
            {
                int barMaxWidth = Width - 180;
                float percent = item.Duration / (float)totalDuration;
                int barWidth = (int)(barMaxWidth * percent);

                // 图标占位框（可用你自己的图）
                e.Graphics.FillRectangle(Brushes.LightGray, 10, y, 32, 32);

                // 应用名
                e.Graphics.DrawString(item.ProcessName, Font, Brushes.White, 50, y + 8);

                // 🌿绿色渐变进度条
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(150, y + 8, barWidth, 24),
                    Color.FromArgb(120, 220, 90),     // 上：亮绿
                    Color.FromArgb(60, 130, 50),      // 下：深绿
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(brush, 150, y + 8, barWidth, 24);
                }

                // 时间
                e.Graphics.DrawString($"{item.Duration}秒", Font, Brushes.LightGray, 160 + barWidth, y + 8);

                // 可选：底部虚线
                using (var pen = new Pen(Color.DimGray) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                {
                    e.Graphics.DrawLine(pen, 150, y + 35, 150 + barMaxWidth, y + 35);
                }

                y += barHeight + spacing;
            }
        }
    }
}