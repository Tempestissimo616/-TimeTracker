using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Wallet_Payment
{
    public class WebSocketTestClient
    {
        public static async Task TestConnection(string serverUrl = "ws://localhost:9000/")
        {
            try
            {
                using (var client = new ClientWebSocket())
                {
                    Console.WriteLine($"🔗 尝试连接到: {serverUrl}");
                    Console.WriteLine($"⏱️ 连接开始时间: {DateTime.Now:HH:mm:ss}");

                    await client.ConnectAsync(new Uri(serverUrl), CancellationToken.None);
                    Console.WriteLine("✅ 连接成功！");
                    Console.WriteLine($"⏱️ 连接完成时间: {DateTime.Now:HH:mm:ss}");

                    // 发送ping测试
                    var pingMessage = "ping";
                    var pingBuffer = Encoding.UTF8.GetBytes(pingMessage);
                    await client.SendAsync(new ArraySegment<byte>(pingBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    Console.WriteLine("📤 发送ping消息");

                    // 接收响应
                    var buffer = new byte[1024];
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"📥 收到响应: {response}");

                    // 发送测试数据
                    var testData = new TimeTrackingData
                    {
                        Url = "https://www.example.com",
                        Title = "测试网页",
                        Domain = "example.com",
                        StartTime = DateTime.Now.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss"),
                        EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Duration = "300000"
                    };

                    var jsonData = JsonSerializer.Serialize(testData);
                    Console.WriteLine($"📤 准备发送JSON数据: {jsonData}");
                    var dataBuffer = Encoding.UTF8.GetBytes(jsonData);
                    await client.SendAsync(new ArraySegment<byte>(dataBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    Console.WriteLine("📤 测试数据发送完成");

                    // 等待一下再关闭
                    await Task.Delay(1000);
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "测试完成", CancellationToken.None);
                    Console.WriteLine("🔌 连接已关闭");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 连接失败: {ex.Message}");
                Console.WriteLine($"🔍 异常类型: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"🔍 内部异常: {ex.InnerException.Message}");
                }
            }
        }
    }
}