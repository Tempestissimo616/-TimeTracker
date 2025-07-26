using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class WebSocketServer
{
    private HttpListener _httpListener;
    
    public async Task StartAsync()
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add("http://localhost:9000/");
        _httpListener.Start();
        
        while (true)
        {
            var context = await _httpListener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                _ = Task.Run(() => HandleWebSocketAsync(context));
            }
        }
    }
    
    private async Task HandleWebSocketAsync(HttpListenerContext context)
    {
        var webSocketContext = await context.AcceptWebSocketAsync(null);
        var webSocket = webSocketContext.WebSocket;
        
        var buffer = new byte[4096];
        
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var data = JsonConvert.DeserializeObject<TimeTrackingData>(json);
                
                // 处理接收到的时间数据
                OnTimeDataReceived?.Invoke(data);
            }
        }
    }
    
    public event Action<TimeTrackingData> OnTimeDataReceived;
}
