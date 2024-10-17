using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Raylib_cs;
using Brushes = System.Windows.Media.Brushes;
using Color = Raylib_cs.Color;
using Panel = System.Windows.Forms.Panel;

namespace RaylibWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public unsafe partial class MainWindow : Window
    {
        private IntPtr _raylibHandle;
        private bool _windowAttached;
        private WindowsFormsHost _formsHost;
        private Panel _raylibPanel;
        private DispatcherTimer _runloopTimer = new DispatcherTimer(DispatcherPriority.Send);

        #region WinAPI Entry Points

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        private const int GWL_STYLE = -16;
        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOSIZE = 0x0001;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // Create WindowsFormsHost
            _formsHost = new WindowsFormsHost();

            // Create WinForms Panel inside the WindowsFormsHost
            _raylibPanel = new Panel
            {
                Width = (int)RaylibGrid.Width,
                Height = (int)RaylibGrid.Height,
                Location = new System.Drawing.Point(0, 0),
            };

            // Add the WinForms Panel to the WindowsFormsHost
            _formsHost.Child = _raylibPanel;

            // Add the WindowsFormsHost to the dedicated Grid
            RaylibGrid.Children.Add(_formsHost);

            // Attach Raylib window when the app is loaded
            Loaded += MainWindow_Loaded;

            // Close Raylib when app is closing
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_windowAttached)
            {
                AttachRaylibWindow();
                _runloopTimer.Interval = TimeSpan.FromSeconds(1 / 120.0);
                _runloopTimer.Tick += RaylibTick;
                _runloopTimer.Start();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_windowAttached)
            {
                Raylib.CloseWindow();
                _runloopTimer.Stop();
            }
        }

        private void AttachRaylibWindow()
        {
            // Initialize Raylib without showing the window
            Raylib.SetConfigFlags(ConfigFlags.UndecoratedWindow);
            Raylib.InitWindow((int)RaylibGrid.Width, (int)RaylibGrid.Height, "Raylib in WPF");
            Raylib.SetTargetFPS(60);
            _raylibHandle = (nint)Raylib.GetWindowHandle();

            // Modify the Raylib window to become a child window
            int style = GetWindowLong(_raylibHandle, GWL_STYLE);
            SetWindowLong(_raylibHandle, GWL_STYLE, style | WS_CHILD | WS_VISIBLE);

            // Set the Raylib window as a child of the WinForms Panel
            SetParent(_raylibHandle, _raylibPanel.Handle);

            // Ensure the Raylib window is positioned at (0, 0) within the Panel
            SetWindowPos(_raylibHandle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOSIZE);
            
            // Show the Raylib window
            ShowWindow(_raylibHandle, 1);
            _windowAttached = true;
        }

        private void RaylibTick(object? sender, EventArgs e)
        {
            if (_windowAttached && !Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                int textMeasure = Raylib.MeasureText("Hi, I'm Raylib!", 24);
                Raylib.DrawText("Hi, I'm Raylib!", 400 - textMeasure / 2, 80, 24, Color.White);

                Raylib.DrawFPS(3, 3);

                Raylib.EndDrawing();
            }
            else if (_windowAttached)
            {
                Raylib.CloseWindow();
                _windowAttached = false;
                _runloopTimer.Stop();
            }
        }
    }
}