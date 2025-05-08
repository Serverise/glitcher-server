using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GlitcherServer
{
    public class Server
    {
        private readonly HttpListener _listener;
        private readonly string _url;
        private readonly GlitcherAPI _api;

        public Server(string url, GlitcherAPI api)
        {
            _url = url ?? throw new ArgumentNullException(nameof(url));
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _listener.Start();
                Console.WriteLine($"Server started at {_url}");
                _api.OnLogMessage?.Invoke($"Server started at {_url}");

                while (!cancellationToken.IsCancellationRequested)
                {
                    var context = await _listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        _ = ProcessWebSocketRequestAsync(context, cancellationToken);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _api.OnLogMessage?.Invoke($"Server error: {ex.Message}");
            }
        }

        private async Task ProcessWebSocketRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            HttpListenerWebSocketContext wsContext = null;
            try
            {
                wsContext = await context.AcceptWebSocketAsync(null);
                var webSocket = wsContext.WebSocket;

                var buffer = new byte[1024 * 4];
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var response = await _api.ProcessMessageAsync(message);
                        if (!string.IsNullOrEmpty(response))
                        {
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, cancellationToken);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _api.OnLogMessage?.Invoke($"WebSocket error: {ex.Message}");
            }
            finally
            {
                if (wsContext?.WebSocket != null)
                {
                    await wsContext.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
        }

        public async Task StopAsync()
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
                _api.OnLogMessage?.Invoke("Server stopped");
            }
        }
    }
}
