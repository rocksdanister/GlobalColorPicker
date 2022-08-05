using H.Hooks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GlobalColorPicker
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        private readonly LowLevelMouseHook hook;

        public MainWindow()
        {
            this.InitializeComponent();

            hook = new LowLevelMouseHook()
            {
                GenerateMouseMoveEvents = true,
                Handling = true
            };
            hook.Move += (_, e) =>
            {
                SetPosAndForeground(e.Position.X + 25, e.Position.Y + 25);
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    SetPreviewColor(GetColorAt(e.Position.X, e.Position.Y));
                });
            };
            hook.Down += (_, e) =>
            {
                e.IsHandled = true; //Mouse click block
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    this.Close();
                });

            };
            hook.Start();

            this.Closed += (_, _) =>
            {
                hook?.Dispose();
            };

            this.Activated += (_, _) =>
            {
                //Remove white border but rounded corner stops working
                var styleCurrentWindowStandard = NativeMethods.GetWindowLongPtr(this.GetWindowHandle(), (int)NativeMethods.GWL.GWL_STYLE);
                var styleNewWindowStandard = styleCurrentWindowStandard.ToInt64() & ~((long)NativeMethods.WindowStyles.WS_THICKFRAME);
                if (NativeMethods.SetWindowLongPtr(new HandleRef(null, this.GetWindowHandle()), (int)NativeMethods.GWL.GWL_STYLE, (IntPtr)styleNewWindowStandard) == IntPtr.Zero)
                {
                    //fail
                }

                if (IsWindows11_OrGreater)
                {
                    //Bring back win11 rounded corner
                    var attribute = NativeMethods.DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
                    var preference = NativeMethods.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                    NativeMethods.DwmSetWindowAttribute(this.GetWindowHandle(), attribute, ref preference, sizeof(uint));

                    if (NativeMethods.GetCursorPos(out NativeMethods.POINT P))
                    {
                        //Force redraw and pos
                        NativeMethods.SetWindowPos(this.GetWindowHandle(), -1, P.X + 25, P.Y + 25, 1, 1, (int)NativeMethods.SetWindowPosFlags.SWP_SHOWWINDOW);
                    }
                }
            };
        }

        public void SetPosAndForeground(int x, int y)
        {
            NativeMethods.SetWindowPos(this.GetWindowHandle(), -1, x, y, 0, 0, (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE);
        }

        public void SetPreviewColor(Color color)
        {
            cBorder.Background = new SolidColorBrush(Color.FromArgb(
                              255,
                              color.R,
                              color.G,
                              color.B
                            ));
            cText.Text = $"rgb({color.R}, {color.G}, {color.B})";
        }

        #region helpers

        public static Color GetColorAt(int x, int y)
        {
            IntPtr desk = NativeMethods.GetDesktopWindow();
            IntPtr dc = NativeMethods.GetWindowDC(desk);
            try
            {
                int a = (int)NativeMethods.GetPixel(dc, x, y);
                return Color.FromArgb(255, (byte)((a >> 0) & 0xff), (byte)((a >> 8) & 0xff), (byte)((a >> 16) & 0xff));
            }
            finally
            {
                NativeMethods.ReleaseDC(desk, dc);
            }
        }

        public static bool IsWindows11_OrGreater => Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;

        #endregion //helpers
    }
}
