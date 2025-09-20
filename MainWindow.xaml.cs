using Microsoft.Win32;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DynamicIsland
{
    public partial class MainWindow : Window
    {
        // P/Invoke
        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax,
            IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc,
            uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);


        delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        IntPtr _foregroundHook;
        WinEventDelegate _winEventHandler;
        DispatcherTimer _heartbeat;
        IntPtr _myHwnd;

        // WinEvent hook constants
        const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        // SetWindowPos constants
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_SHOWWINDOW = 0x0040;

        // Owner lookup
        const uint GW_OWNER = 4;

        private UserControl[] ucArr;
        private uint activeUC;
        private readonly string curTag = "Main".PadRight(10);

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                PositionWindow();

                SourceInitialized += MainWindow_SourceInitialized;
                Closed += (s, e) =>
                {
                    UnhookWinEvent(_foregroundHook);
                    _heartbeat?.Stop();
                };

                SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;


                UserControl ucTimer = new Timer();
                UserControl ucMedia = new MediaControl();
                ucArr = [ucTimer, ucMedia];
                activeUC = 0;
                UserControlMainWindow.Content = ucArr[0];

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug() // set minimum level to log
                    .WriteTo.File("C:\\Users\\Aym_s\\Downloads\\DynamicIsland.log",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Tag} : {Message}{NewLine}{Exception}")
                    .CreateLogger();

                // Handle unhandled exceptions
                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    Log.ForContext("Tag", curTag).Fatal(args.ExceptionObject as Exception, "Unhandled exception");
                    Log.CloseAndFlush();
                };
            }
            catch(Exception ex){
                File.WriteAllText(@"C:\Users\Aym_s\Downloads\crashlog.txt", ex.ToString());
            }
        }

        private async void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                Ilog.Info(curTag, "Device Logged in");
                await Task.Delay(TimeSpan.FromSeconds(2));
                PositionWindow();
            }
        }

        private void PositionWindow()
        {
            // Position the window at the bottom left corner, just above the taskbar
            var desktopWorkingArea = SystemParameters.WorkArea;
            Ilog.Info(curTag, "Positioning window");
            this.Left = desktopWorkingArea.Left;
            this.Top = desktopWorkingArea.Bottom - this.Height + 50; // Adjust the offset as needed
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            try
            {
                Ilog.Info(curTag, "Source Init");
                _myHwnd = new WindowInteropHelper(this).Handle;

                // 1) Elevate your window and its hidden owner
                ElevateWindow(_myHwnd);

                // 2) Hook foreground changes
                _winEventHandler = WinEventProc;
                _foregroundHook = SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND,
                    EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero,
                    _winEventHandler,
                    0, 0,
                    WINEVENT_OUTOFCONTEXT);

                // 3) Heartbeat: reassert topmost every second
                _heartbeat = new DispatcherTimer(
                    TimeSpan.FromSeconds(1),
                    DispatcherPriority.Background,
                    (s2, e2) => ElevateWindow(_myHwnd),
                    Dispatcher);
                
                _heartbeat.Start();
            }
            catch (Exception ex)
            {
                Log.Error(curTag, ex.Message);
            }
        }
        private void WinEventProc(
        IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // If the taskbar just became the foreground window, re-elevate
            const string ShellClass = "Shell_TrayWnd";
            if (eventType == EVENT_SYSTEM_FOREGROUND)
            {
                // figure out what just came to front
                var cls = new StringBuilder(64);
                GetClassName(hwnd, cls, cls.Capacity);
                GetWindowThreadProcessId(hwnd, out uint pid);
                string proc = Process.GetProcessById((int)pid).ProcessName;
                // if any shell UI (taskbar, Start or Search hosts) opens, re-elevate
                if (cls.ToString() == ShellClass || proc.Equals("SearchUI", StringComparison.OrdinalIgnoreCase)
                    || proc.Equals("StartMenuExperienceHost", StringComparison.OrdinalIgnoreCase)
                    || proc.Equals("ShellExperienceHost", StringComparison.OrdinalIgnoreCase))
                {
                    // delay until after the animation—ApplicationIdle runs once UI settles
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ElevateWindow(_myHwnd);
                    }), DispatcherPriority.ApplicationIdle);
                }
            }
        }

        private void ElevateWindow(IntPtr hwnd)
        {
            // Elevate main window
            SetWindowPos(
                hwnd,
                HWND_TOPMOST,
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

            // Elevate hidden owner window (WPF quirk when ShowInTaskbar=false)
            var owner = GetWindow(hwnd, GW_OWNER);
            if (owner != IntPtr.Zero)
            {
                SetWindowPos(
                    owner,
                    HWND_TOPMOST,
                    0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
        }

        private void ChangeBtn_Click(object sender, RoutedEventArgs e)
        {
            try { 
                activeUC = (activeUC + 1) % 2;
                Ilog.Info(curTag, "UC changed to " + activeUC);
                UserControlMainWindow.Content = ucArr[activeUC];
            }
            catch (Exception ex)
            {
                Log.Error(curTag, ex.Message);
            }
        }
    }

    public static class Ilog
    {
        public static void Info(string tag, string msg)
        {
            Log.ForContext("Tag", tag).Information(msg);
        }
        public static void Error(string tag, string msg)
        {
            Log.ForContext("Tag", tag).Error(msg);
        }
        public static void Debug(string tag, string msg)
        {
            Log.ForContext("Tag", tag).Debug(msg);
        }
    }
}
