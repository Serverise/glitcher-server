using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text.Json;
using GlitcherWPF;

namespace GlitcherServer
{
    class Program
    {
        static void Main(string[] args)
        {
            string port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
            var wssv = new WebSocketServer($"ws://0.0.0.0:{port}");
            wssv.AddWebSocketService<GlitcherService>("/glitcher");
            wssv.AddWebSocketService<HealthCheckService>("/healthz");
            wssv.Start();
            Console.WriteLine($"WebSocket server started at ws://0.0.0.0:{port}/glitcher");
            Console.ReadKey();
            wssv.Stop();
        }
    }

    public class GlitcherService : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            Console.WriteLine("Client connected");
            GlitcherAPI.GlitcherModule.OnLogMessage += (message, level) =>
            {
                Send(JsonSerializer.Serialize(new { type = "log", level, message }));
            };
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                var data = JsonSerializer.Deserialize<dynamic>(e.Data);
                string type = data.type;

                switch (type)
                {
                    case "inject":
                        if (GlitcherAPI.GlitcherModule.InjectionCheck() == 1)
                        {
                            Send(JsonSerializer.Serialize(new { type = "alreadyInjected" }));
                        }
                        else
                        {
                            GlitcherAPI.GlitcherModule.InjectAsync().ContinueWith(t =>
                            {
                                Send(JsonSerializer.Serialize(new
                                {
                                    type = "inject",
                                    success = t.Result,
                                    message = t.Result ? "Injected successfully" : "Injection failed"
                                }));
                            });
                        }
                        break;
                    case "execute":
                        string script = data.script;
                        try
                        {
                            GlitcherAPI.GlitcherModule.ExecuteScript(script);
                            Send(JsonSerializer.Serialize(new { type = "execute", success = true }));
                        }
                        catch (Exception ex)
                        {
                            Send(JsonSerializer.Serialize(new { type = "execute", success = false, message = ex.Message }));
                        }
                        break;
                    case "setAutoInject":
                        bool enabled = data.enabled;
                        GlitcherAPI.GlitcherModule.UseAutoInject(enabled);
                        break;
                    case "alreadyInjected":
                        Send(JsonSerializer.Serialize(new { type = "alreadyInjected" }));
                        break;
                    default:
                        Send(JsonSerializer.Serialize(new { type = "error", message = "Unknown command" }));
                        break;
                }
            }
            catch (Exception ex)
            {
                Send(JsonSerializer.Serialize(new { type = "error", message = ex.Message }));
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("Client disconnected");
        }
    }

    public class HealthCheckService : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            Send("OK");
            Close();
        }
    }
}
