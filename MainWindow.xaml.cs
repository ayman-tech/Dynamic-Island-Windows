using Microsoft.Win32;
using Serilog;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;

namespace DynamicIsland
{
    public partial class MainWindow : Window
    {
        // P/Invoke declarations
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hWnd);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;
        UserControl[] ucArr;
        private uint activeUC;
        private readonly string curTag = "Main".PadRight(10);

        public MainWindow()
        {
            InitializeComponent();
            this.Topmost = true;
            PositionWindow();

            // Set window to topmost on source initialized
            this.SourceInitialized += MainWindow_SourceInitialized;
            // Handle the StateChanged event
            this.StateChanged += MainWindow_StateChanged;

            UserControl ucTimer = new Timer();
            UserControl ucMedia = new MediaControl();
            ucArr = [ucTimer, ucMedia];
            activeUC = 0;
            UserControlMainWindow.Content = ucArr[0];

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() // set minimum level to log
                .WriteTo.File("C:\\Users\\Aym_s\\source\\repos\\Logs\\DynamicIsland.log",
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
        private void PositionWindow()
        {

            // Position the window at the bottom left corner, just above the taskbar
            var desktopWorkingArea = SystemParameters.WorkArea;
            Ilog.Info(curTag, "Positioning window");
            this.Left = desktopWorkingArea.Left;
            this.Top = desktopWorkingArea.Bottom - this.Height + 50; // Adjust the offset as needed
        }
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.WindowState == WindowState.Minimized)
                {
                    Ilog.Info(curTag, "Window Minimized");
                    this.WindowState = WindowState.Normal;
                    var hWnd = new WindowInteropHelper(this).Handle;
                    SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    PositionWindow();
                }
            } catch(Exception ex)
            {
                Log.Error(curTag, ex.Message);
            }
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            try { 
                var hWnd = new WindowInteropHelper(this).Handle;
                SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            }
            catch (Exception ex)
            {
                Log.Error(curTag, ex.Message);
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

        private void Window_Closed(object sender, EventArgs e)
        {
            Ilog.Info(curTag, "MainWindow Closed");
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
