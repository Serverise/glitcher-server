using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GlitcherWPF
{
    class GlitcherAPI
    {
        public static class GlitcherModule
        {
            public class ClientInfo
            {
                public int id { get; set; }

                public string name { get; set; }

                public string version { get; set; }

                public int state { get; set; }
            }

            public enum APISetting
            {
                AutoAttach = 1
            }
            private const string RobloxProcess = "RobloxPlayerBeta";
            private const int GameMemoryThreshold = 500 * 1024 * 1024; // 500MB

            private static string _currentUserLevel = "3";

            private static bool Initialized = false;

            private static string _attachNotifyTitle = "[GlitcherAPI]";

            private static string _attachNotifyText = "Succesfully Attached!";

            [DllImport("bin\\Xeno.dll", CallingConvention = CallingConvention.Cdecl)]
            private static extern void Initialize(bool useConsole);

            [DllImport("bin\\Xeno.dll", CallingConvention = CallingConvention.Cdecl)]
            private static extern void Attach();

            [DllImport("bin\\Xeno.dll", CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr GetClients();

            [DllImport("bin\\Xeno.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern void SetSettings(APISetting settingID, int value);

            [DllImport("bin\\Xeno.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void Execute(byte[] scriptSource, int[] PIDs, int numUsers);

            private static bool _useCustomNotifier = false;

            public static void OldSetInjectionNotification(string title, string text)
            {
                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(text))
                {
                    return;
                }
                _attachNotifyTitle = title;
                _attachNotifyText = text;
            }
            private static string _customNotifierTitle = "GlitcherAPI Injected";
            private static string _customNotifierSubtext = "Injection Succeeded";
            public static void SetDefualtNotifier(bool useCustom, string title, string Subtext)
            {
                _useCustomNotifier = useCustom;
                _customNotifierTitle = title;
                _customNotifierSubtext = Subtext;
            }

            // Added for logging support
            public static event Action<string, string> OnLogMessage;

            public static async Task<bool> InjectAsync()
            {
                try
                {
                    OnLogMessage?.Invoke("Initializing injection...", "Info");
                    if (!Initialized)
                    {
                        Initialize(useConsole: false);
                        Initialized = true;
                    }
                    Attach();
                    await Task.Delay(1000);

                    for (int i = 0; i < 50; i++)
                    {
                        if (GetClientsList().Count != 0)
                        {
                            break;
                        }
                        await Task.Delay(100);
                    }

                    if (GetClientsList().Count > 0)
                    {
                        await Task.Delay(3000);
                        if (_useCustomNotifier)
                        {
                            UseCustomnNotifier(_customNotifierTitle, _customNotifierSubtext);
                        }
                        else
                        {
                            UseInjectionNotifier();
                        }
                        OnLogMessage?.Invoke("Injection completed successfully", "Success");
                        return true;
                    }
                    OnLogMessage?.Invoke("No clients found after injection attempt", "Error");
                    return false;
                }
                catch (Exception ex)
                {
                    OnLogMessage?.Invoke($"Injection failed: {ex.Message}", "Error");
                    return false;
                }
            }

            public static void Inject()
            {
                if (!Initialized)
                {
                    Initialize(useConsole: false);
                    Initialized = true;
                }
                Attach();
                Thread.Sleep(1000);
                for (int i = 0; i < 50; i++)
                {
                    if (GetClientsList().Count != 0)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
                if (GetClientsList().Count > 0)
                {
                    Thread.Sleep(3000);
                }
                if (_useCustomNotifier)
                {
                    UseCustomnNotifier(_customNotifierTitle, _customNotifierSubtext);
                }
                else
                {
                    UseInjectionNotifier();
                }
            }

            public static void UseInjectionNotifier()
            {
                string script = @"
local player = game:GetService(""Players"").LocalPlayer
local TweenService = game:GetService(""TweenService"")

local container = player.PlayerGui:FindFirstChild(""NotificationContainer"")
if not container then
    container = Instance.new(""Frame"")
    container.Name = ""NotificationContainer""
    container.Parent = player.PlayerGui
    container.Size = UDim2.new(1, 0, 1, 0)
    container.BackgroundTransparency = 1
    container.Position = UDim2.new(0, 0, 0, 0)
end
task.wait(0.05)
local function Checkframes()
    local count = 0
    for _, gui in ipairs(container:GetChildren()) do
        if gui:IsA(""ScreenGui"") and gui:GetAttribute(""Active"") then
            count += 1
        end
    end
    return count
end
local existing = Checkframes()

local gui = Instance.new(""ScreenGui"")
gui.Name = ""SimpleNotify""
gui.ResetOnSpawn = false
gui.Parent = container
gui:SetAttribute(""Active"", true) 

local mainFrame = Instance.new(""Frame"")
mainFrame.Name = ""MainFrame""
mainFrame.Parent = gui
mainFrame.BackgroundColor3 = Color3.new(0, 0, 0)
mainFrame.BackgroundTransparency = 0.15
local offset = -80 - ((existing - 1) * 70) 
mainFrame.Position = UDim2.new(1, 0, 1, offset)
mainFrame.Size = UDim2.new(0, 260, 0, 60) 
mainFrame.AnchorPoint = Vector2.new(1, 1) 

local title = Instance.new(""TextLabel"")
title.Name = ""GlitcherAPI""
title.Parent = mainFrame
title.BackgroundTransparency = 1
title.Position = UDim2.new(0, 12, 0, 8)
title.Size = UDim2.new(1, -24, 0, 24)
title.Font = Enum.Font.GothamBold
title.Text = ""INJECTION SUCCEEDED""
title.TextColor3 = Color3.new(1, 1, 1)
title.TextSize = 16
title.TextXAlignment = Enum.TextXAlignment.Left

local message = Instance.new(""TextLabel"")
message.Name = ""GlitcherAPI Injected Correctly""
message.Parent = mainFrame
message.BackgroundTransparency = 1
message.Position = UDim2.new(0, 12, 0, 32)
message.Size = UDim2.new(1, -24, 0, 20)
message.Font = Enum.Font.Gotham
message.Text = ""Have fun exploiting!""
message.TextColor3 = Color3.new(0.8, 0.8, 0.8)
message.TextSize = 14
message.TextXAlignment = Enum.TextXAlignment.Left

local slideIn = TweenService:Create(
    mainFrame,
    TweenInfo.new(0.7, Enum.EasingStyle.Quad, Enum.EasingDirection.Out),
    {Position = UDim2.new(1, -20, 1, offset)}
)

slideIn:Play()

wait(2.5) 

local slideOut = TweenService:Create(
    mainFrame,
    TweenInfo.new(0.4, Enum.EasingStyle.Quad),
    {Position = UDim2.new(1, 300, 1, offset)}
)
slideOut:Play()

slideOut.Completed:Wait()
gui:SetAttribute(""Active"", false)
gui:Destroy()";

                ExecuteScript(script);
            }

            public static void UseExecutionNotifier()
            {
                string script = @"
local player = game:GetService(""Players"").LocalPlayer
local TweenService = game:GetService(""TweenService"")

local container = player.PlayerGui:FindFirstChild(""NotificationContainer"")
if not container then
    container = Instance.new(""Frame"")
    container.Name = ""NotificationContainer""
    container.Parent = player.PlayerGui
    container.Size = UDim2.new(1, 0, 1, 0)
    container.BackgroundTransparency = 1
    container.Position = UDim2.new(0, 0, 0, 0)
end
task.wait(0.05)
local function Checkframes()
    local count = 0
    for _, gui in ipairs(container:GetChildren()) do
        if gui:IsA(""ScreenGui"") and gui:GetAttribute(""Active"") then
            count += 1
        end
    end
    return count
end
local existing = Checkframes()

local gui = Instance.new(""ScreenGui"")
gui.Name = ""SimpleNotify""
gui.ResetOnSpawn = false
gui.Parent = container
gui:SetAttribute(""Active"", true) 

local mainFrame = Instance.new(""Frame"")
mainFrame.Name = ""MainFrame""
mainFrame.Parent = gui
mainFrame.BackgroundColor3 = Color3.new(0, 0, 0)
mainFrame.BackgroundTransparency = 0.15
local offset = -80 - ((existing - 1) * 70) 
mainFrame.Position = UDim2.new(1, 0, 1, offset)
mainFrame.Size = UDim2.new(0, 260, 0, 60) 
mainFrame.AnchorPoint = Vector2.new(1, 1) 

local title = Instance.new(""TextLabel"")
title.Name = ""GlitcherAPI""
title.Parent = mainFrame
title.BackgroundTransparency = 1
title.Position = UDim2.new(0, 12, 0, 8)
title.Size = UDim2.new(1, -24, 0, 24)
title.Font = Enum.Font.GothamBold
title.Text = ""EXECUTION SUCCEEDED""
title.TextColor3 = Color3.new(1, 1, 1)
title.TextSize = 16
title.TextXAlignment = Enum.TextXAlignment.Left

local message = Instance.new(""TextLabel"")
message.Name = ""GlitcherAPI Injected""
message.Parent = mainFrame
message.BackgroundTransparency = 1
message.Position = UDim2.new(0, 12, 0, 32)
message.Size = UDim2.new(1, -24, 0, 20)
message.Font = Enum.Font.Gotham
message.Text = ""Script Executed Succesfully!""
message.TextColor3 = Color3.new(0.8, 0.8, 0.8)
message.TextSize = 14
message.TextXAlignment = Enum.TextXAlignment.Left

local slideIn = TweenService:Create(
    mainFrame,
    TweenInfo.new(0.7, Enum.EasingStyle.Quad, Enum.EasingDirection.Out),
    {Position = UDim2.new(1, -20, 1, offset)}
)

slideIn:Play()

wait(2.5) 

local slideOut = TweenService:Create(
    mainFrame,
    TweenInfo.new(0.4, Enum.EasingStyle.Quad),
    {Position = UDim2.new(1, 300, 1, offset)}
)
slideOut:Play()

slideOut.Completed:Wait()
gui:SetAttribute(""Active"", false)
gui:Destroy()";

                ExecuteScript(script);
            }
            public static void ExecuteScript(string scriptSource)
            {
                List<ClientInfo> clientsList = GetClientsList();
                int[] array = clientsList.Select((ClientInfo c) => c.id).ToArray();
                Execute(Encoding.UTF8.GetBytes(scriptSource + "\0"), array, array.Length);
            }

            public static void UseCustomnNotifier(string MainText, string Subtext)
            {
                string s = $@"
local player = game:GetService(""Players"").LocalPlayer
local TweenService = game:GetService(""TweenService"")

local container = player.PlayerGui:FindFirstChild(""NotificationContainer"")
if not container then
    container = Instance.new(""Frame"")
    container.Name = ""NotificationContainer""
    container.Parent = player.PlayerGui
    container.Size = UDim2.new(1, 0, 1, 0)
    container.BackgroundTransparency = 1
    container.Position = UDim2.new(0, 0, 0, 0)
end
task.wait(0.05)
local function Checkframes()
	local count = 0
	for _, gui in ipairs(container:GetChildren()) do
		if gui:IsA(""ScreenGui"") and gui:GetAttribute(""Active"") then
			count += 1
		end
	end
	return count
end
local existing = Checkframes()

local gui = Instance.new(""ScreenGui"")
gui.Name = ""SimpleNotify""
gui.ResetOnSpawn = false
gui.Parent = container
gui:SetAttribute(""Active"", true) 

local mainFrame = Instance.new(""Frame"")
mainFrame.Name = ""MainFrame""
mainFrame.Parent = gui
mainFrame.BackgroundColor3 = Color3.new(0, 0, 0)
mainFrame.BackgroundTransparency = 0.15
local offset = -80 - ((existing - 1) * 70) 
mainFrame.Position = UDim2.new(1, 0, 1, offset)
mainFrame.Size = UDim2.new(0, 260, 0, 60) 
mainFrame.AnchorPoint = Vector2.new(1, 1) 

local title = Instance.new(""TextLabel"")
title.Name = ""GlitcherAPI""
title.Parent = mainFrame
title.BackgroundTransparency = 1
title.Position = UDim2.new(0, 12, 0, 8)
title.Size = UDim2.new(1, -24, 0, 24)
title.Font = Enum.Font.GothamBold
title.Text = ""{MainText}""
title.TextColor3 = Color3.new(1, 1, 1)
title.TextSize = 16
title.TextXAlignment = Enum.TextXAlignment.Left

local message = Instance.new(""TextLabel"")
message.Name = ""GlitcherAPI Injected""
message.Parent = mainFrame
message.BackgroundTransparency = 1
message.Position = UDim2.new(0, 12, 0, 32)
message.Size = UDim2.new(1, -24, 0, 20)
message.Font = Enum.Font.Gotham
message.Text = ""{Subtext}""
message.TextColor3 = Color3.new(0.8, 0.8, 0.8)
message.TextSize = 14
message.TextXAlignment = Enum.TextXAlignment.Left

local slideIn = TweenService:Create(
	mainFrame,
	TweenInfo.new(0.7, Enum.EasingStyle.Quad, Enum.EasingDirection.Out),
	{{Position = UDim2.new(1, -20, 1, offset)}}
)

slideIn:Play()

wait(2.5) 

local slideOut = TweenService:Create(
    mainFrame,
    TweenInfo.new(0.4, Enum.EasingStyle.Quad),
    {{Position = UDim2.new(1, 300, 1, offset)}}
)
slideOut:Play()

slideOut.Completed:Wait()
gui:SetAttribute(""Active"", false)
gui:Destroy()";

                ExecuteScript(s);
            }

            private static bool _LetBroFree = true;
            private static bool NOWERESDIDDYEnableAutoInject = false;
            private static Thread _monitorThread;

            public static void UseAutoInject(bool enable)
            {
                NOWERESDIDDYEnableAutoInject = enable;

                if (enable && (_monitorThread == null || !_monitorThread.IsAlive))
                {
                    _monitorThread = new Thread(() =>
                    {
                        while (NOWERESDIDDYEnableAutoInject)
                        {
                            try
                            {
                                var proc = Process.GetProcessesByName("RobloxPlayerBeta").FirstOrDefault();
                                if (proc != null)
                                {
                                    bool isInGame = proc.WorkingSet64 > 450 * 1024 * 1024;

                                    if (isInGame && _LetBroFree)
                                    {
                                        Inject();
                                        _LetBroFree = false;
                                        OnLogMessage?.Invoke("Auto-injected successfully", "Success");
                                    }
                                    else if (!isInGame && !_LetBroFree)
                                    {
                                        _LetBroFree = true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                OnLogMessage?.Invoke($"Auto-inject error: {ex.Message}", "Error");
                            }

                            Thread.Sleep(1000);
                        }
                    })
                    { IsBackground = true };

                    _monitorThread.Start();
                    OnLogMessage?.Invoke("Auto-injector started", "Info");
                }
                else if (!enable && _monitorThread != null)
                {
                    OnLogMessage?.Invoke("Auto-injector stopped", "Info");
                }
            }

            public static List<ClientInfo> GetClientsList()
            {
                List<ClientInfo> list = new List<ClientInfo>();
                IntPtr clients = GetClients();
                if (clients == IntPtr.Zero)
                {
                    return list;
                }
                string text = Marshal.PtrToStringAnsi(clients);
                if (string.IsNullOrEmpty(text))
                {
                    return list;
                }
                try
                {
                    MatchCollection matchCollection = Regex.Matches(text, "\\[\\s*(\\d+),\\s*\"(.*?)\",\\s*\"(.*?)\",\\s*(\\d+)\\s*\\]");
                    foreach (Match item in matchCollection)
                    {
                        list.Add(new ClientInfo
                        {
                            id = int.Parse(item.Groups[1].Value),
                            name = item.Groups[2].Value,
                            version = item.Groups[3].Value,
                            state = int.Parse(item.Groups[4].Value)
                        });
                    }
                }
                catch (Exception ex)
                {
                    OnLogMessage?.Invoke($"Error parsing clients: {ex.Message}", "Error");
                }
                return list;
            }

            public static int InjectionCheck()
            {
                try
                {
                    List<ClientInfo> clientsList = GetClientsList();
                    return (clientsList.Count > 0) ? 1 : 0;
                }
                catch
                {
                    return 0;
                }
            }

            public static void SetLevel(string userLevel)
            {
                _currentUserLevel = userLevel.Replace("\"", "\\\"");
            }
        }

        public static class Utilities
        {
            [DllImport("user32.dll")]
            private static extern bool SetForegroundWindow(IntPtr hWnd);

            public static bool FocusRoblox()
            {
                var roblox = Process.GetProcessesByName("RobloxPlayerBeta").FirstOrDefault();
                if (roblox == null) return false;

                return SetForegroundWindow(roblox.MainWindowHandle);
            }
            public static bool IsRobloxRunning() =>
                Process.GetProcessesByName("RobloxPlayerBeta").Length > 0;

            public static void CloseRoblox()
            {
                if (IsRobloxRunning())
                {
                    Process[] processesByName = Process.GetProcessesByName("RobloxPlayerBeta");
                    Process[] array = processesByName;
                    foreach (Process process in array)
                    {
                        process.Kill();
                    }
                }
            }
        }
    }
}