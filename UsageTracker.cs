﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Social_Blade_Dashboard
{
    public class UsageTracker
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private DispatcherTimer timer;
        private string lastProcess;
        private DateTime lastSwitchTime;

        public UsageTracker()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            lastProcess = GetActiveProcessName();
            lastSwitchTime = DateTime.Now;
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            string currentProcess = GetActiveProcessName();
            if (currentProcess != lastProcess)
            {
                int duration = (int)(DateTime.Now - lastSwitchTime).TotalSeconds;
                DatabaseHelper.AddUsage(lastProcess, DateTime.Today, duration);
                lastProcess = currentProcess;
                lastSwitchTime = DateTime.Now;
            }
        }

        private string GetActiveProcessName()
        {
            IntPtr hwnd = GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out uint pid);
            try
            {
                var proc = Process.GetProcessById((int)pid);
                return proc.ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
