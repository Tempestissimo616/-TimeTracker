using Social_Blade_Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Social_Blade_Dashboard;

namespace Wallet_Payment
{
    public class WebSocketService
    {
        private HttpListener listener;
        private bool isRunning = false;
        private CancellationTokenSource cancellationTokenSource;

        // 事件，用于通知主线程收到新的时间追踪数据
        public event Action<TimeTrackingData> OnTimeTrackingDataReceived;

        public async Task StartServerAsync()
        {
            if (isRunning) return;

            try
            {
                // 使用9000端口
                string port = "9000";
                bool started = false;

                try
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add($"http://localhost:{port}/");
                    listener.Start();
                    isRunning = true;
                    cancellationTokenSource = new CancellationTokenSource();

                    Console.WriteLine($"✅ WebSocket 服务器已启动，监听端口 {port}");
                    Console.WriteLine($"🌐 Chrome扩展请连接到: ws://localhost:{port}/");
                    started = true;
                }
                catch (HttpListenerException ex)
                {
                    Console.WriteLine($"❌ 端口 {port} 启动失败: {ex.Message}");
                    Console.WriteLine("💡 请确保端口9000没有被其他程序占用");
                    listener?.Close();
                    listener = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 端口 {port} 启动失败: {ex.Message}");
                    listener?.Close();
                    listener = null;
                }

                if (!started)
                {
                    Console.WriteLine("❌ 所有端口都无法启动，WebSocket服务将不可用");
                    return;
                }

                while (isRunning && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        Console.WriteLine("🔄 等待WebSocket连接...");
                        HttpListenerContext context = await listener.GetContextAsync();
                        Console.WriteLine($"📡 收到连接请求: {context.Request.RemoteEndPoint}");

                        if (context.Request.IsWebSocketRequest)
                        {
                            Console.WriteLine("✅ 确认是WebSocket请求，开始处理...");
                            _ = HandleConnectionAsync(context); // Fire-and-forget
                        }
                        else
                        {
                            Console.WriteLine("❌ 不是WebSocket请求，返回400错误");
                            context.Response.StatusCode = 400;
                            context.Response.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ WebSocket 服务器错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 启动 WebSocket 服务器失败: {ex.Message}");
                isRunning = false;
            }
        }

        public void StopServer()
        {
            isRunning = false;
            cancellationTokenSource?.Cancel();
            listener?.Stop();
            listener?.Close();
        }

        public bool IsRunning()
        {
            return isRunning;
        }

        public string GetServerStatus()
        {
            if (!isRunning)
                return "❌ WebSocket服务器未运行";

            try
            {
                var prefixes = listener?.Prefixes;
                if (prefixes != null && prefixes.Count > 0)
                {
                    string url = prefixes.First();
                    return $"✅ WebSocket服务器运行中 - {url}";
                }
                return "❌ WebSocket服务器状态未知";
            }
            catch
            {
                return "❌ WebSocket服务器状态检查失败";
            }
        }

        private async Task HandleConnectionAsync(HttpListenerContext context)
        {
            WebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
            WebSocket webSocket = wsContext.WebSocket;
            Console.WriteLine($"📡 客户端已连接: {context.Request.RemoteEndPoint}");

            byte[] buffer = new byte[4096];

            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", cancellationTokenSource.Token);
                        Console.WriteLine("🔌 客户端断开连接");
                    }
                    else
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        if (message == "ping")
                        {
                            await webSocket.SendAsync(Encoding.UTF8.GetBytes("pong"), WebSocketMessageType.Text, true, cancellationTokenSource.Token);
                            Console.WriteLine("↔️ 收到 ping，回复 pong");
                        }
                        else
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(message);
                                var root = doc.RootElement;

                                var timeTrackingData = new TimeTrackingData
                                {
                                    Url = root.GetPropertyOrDefault("url"),
                                    Title = root.GetPropertyOrDefault("title"),
                                    Domain = root.GetPropertyOrDefault("domain"),
                                    StartTime = root.GetPropertyOrDefault("startTime"),
                                    EndTime = root.GetPropertyOrDefault("endTime"),
                                    Duration = root.GetPropertyOrDefault("duration"),
                                    Icon = root.GetPropertyOrDefault("icon") // 新增
                                };

                                Console.WriteLine(new string('=', 50));
                                Console.WriteLine("📩 收到时间追踪数据:");
                                Console.WriteLine($"URL: {timeTrackingData.Url}");
                                Console.WriteLine($"标题: {timeTrackingData.Title}");
                                Console.WriteLine($"域名: {timeTrackingData.Domain}");
                                Console.WriteLine($"开始时间: {timeTrackingData.StartTime}");
                                Console.WriteLine($"结束时间: {timeTrackingData.EndTime}");
                                Console.WriteLine($"持续时间: {timeTrackingData.Duration}ms");
                                Console.WriteLine(new string('=', 50));

                                // 在UI线程中触发事件
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    OnTimeTrackingDataReceived?.Invoke(timeTrackingData);
                                });
                            }
                            catch (JsonException)
                            {
                                Console.WriteLine($"⚠️ 收到非JSON消息: {message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 处理客户端时出错: {ex.Message}");
            }
        }
    }

    public class TimeTrackingData
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public string Domain { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Icon { get; set; }
        public string Duration { get; set; }

        public string DurationDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(Duration)) return "0秒";
                if (!int.TryParse(Duration, out int seconds)) return Duration;

                if (seconds < 60)
                    return $"{seconds}秒";
                else if (seconds < 3600)
                    return $"{seconds / 60}分{seconds % 60}秒";
                else
                    return $"{seconds / 3600}小时{(seconds % 3600) / 60}分{seconds % 60}秒";
            }
        }
    }



    public static class JsonElementExtensions
    {
        public static string GetPropertyOrDefault(this JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) ? value.ToString() : "N/A";
        }
    }


}