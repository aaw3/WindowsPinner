using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsPinner
{
    public partial class MainForm : Form
    {
        //constants
        byte opacity = 255; //0-255


        [StructLayout(LayoutKind.Sequential)]
        internal struct PSIZE
        {
            public int x;
            public int y;
        }

        [DllImport("dwmapi.dll")]
        static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        [DllImport("dwmapi.dll")]
        static extern int DwmUnregisterThumbnail(IntPtr thumb);

        [DllImport("dwmapi.dll")]
        static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);

        [StructLayout(LayoutKind.Sequential)]
        internal struct DWM_THUMBNAIL_PROPERTIES
        {
            public int dwFlags;
            public Rect rcDestination;
            public Rect rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }

        [DllImport("dwmapi.dll")]
        static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            internal Rect(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private List<Window> windows;

        private void GetWindows()
        {
            windows = new List<Window>();

            EnumWindows(Callback, 0);

            lstWindows.Items.Clear();
            foreach (Window w in windows)
                lstWindows.Items.Add(w);
        }

        private bool Callback(IntPtr hwnd, int lParam)
        {
            if (this.Handle != hwnd && (GetWindowLongA(hwnd, GWL_STYLE) & TARGETWINDOW) == TARGETWINDOW)
            {
                StringBuilder sb = new StringBuilder(200);
                GetWindowText(hwnd, sb, sb.Capacity);
                Window t = new Window();
                t.Handle = hwnd;
                t.Title = sb.ToString();
                windows.Add(t);
            }

            return true; //continue enumeration
        }

        delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);

        [DllImport("user32.dll")]
        static extern int EnumWindows(EnumWindowsCallback lpEnumFunc, int lParam);

        [DllImport("user32.dll")]
        public static extern void GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern ulong GetWindowLongA(IntPtr hWnd, int nIndex);

        static readonly int GWL_STYLE = -16;

        static readonly ulong WS_VISIBLE = 0x10000000L;
        static readonly ulong WS_BORDER = 0x00800000L;
        static readonly ulong TARGETWINDOW = WS_BORDER | WS_VISIBLE;

        internal class Window
        {
            public string Title;
            public IntPtr Handle;

            public override string ToString()
            {
                return Title;
            }
        }

        private IntPtr thumb;
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (thumb != IntPtr.Zero)
                DwmUnregisterThumbnail(thumb);

            GetWindows();
        }

        private void lstWindows_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstWindows.SelectedIndex > -1)
            {

                Window w = (Window)lstWindows.SelectedItem;
                if (thumb != IntPtr.Zero)
                    DwmUnregisterThumbnail(thumb);

                int i = DwmRegisterThumbnail(this.Handle, w.Handle, out thumb);
                if (i == 0)
                    UpdateThumb();

                CurrentWindow = w;
            }
        }

        static readonly int DWM_TNP_VISIBLE = 0x8;
        static readonly int DWM_TNP_OPACITY = 0x4;
        static readonly int DWM_TNP_RECTDESTINATION = 0x1;

        private void UpdateThumb()
        {
            if (thumb != IntPtr.Zero)
            {
                PSIZE size;
                DwmQueryThumbnailSourceSize(thumb, out size);

                DWM_THUMBNAIL_PROPERTIES props = new DWM_THUMBNAIL_PROPERTIES();
                props.dwFlags = DWM_TNP_VISIBLE | DWM_TNP_RECTDESTINATION | DWM_TNP_OPACITY;

                props.fVisible = true;
                props.opacity = (byte)opacity; //opacity.Value;

                props.rcDestination = new Rect(image.Left, image.Top, image.Right, image.Bottom);
                if (size.x < image.Width)
                    props.rcDestination.Right = props.rcDestination.Left + size.x;
                if (size.y < image.Height)
                    props.rcDestination.Bottom = props.rcDestination.Top + size.y;

                DwmUpdateThumbnailProperties(thumb, ref props);
            }
        }


        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();


        // A Win32 constant
        const int WM_SETTEXT = 0x000C;
        const int WM_KEYDOWN = 0x0100;
        const int VK_RETURN = 0x0D;

        // An overload of the SendMessage function, this time taking in a string as the lParam.
        [DllImport("User32.dll")]
        public static extern Int32 SendMessage(int hWnd, int Msg, int wParam, string lParam);
        [DllImport("User32.Dll")]
        public static extern Int32 PostMessage(int hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        const int SW_MAXIMIZE = 3;
        const int SW_SHOW = 5;
        const int SW_HIDE = 0;
        const int SW_SHOWNORMAL = 1;
        const int SW_RESTORE = 9;

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        const int HWND_BOTTOM = 1;
        const int HWND_NOTOPMOST = -2;
        const int HWND_TOP = 0;
        const int HWND_TOPMOST = -1;

        const int SWP_ASYNCWINDOWPOS = 0x4000;
        const int SWP_DEFERERASE = 0x2000;
        const int SWP_DRAWFRAME = 0x0020;
        const int SWP_FRAMECHANGED = 0x0020;
        const int SWP_HIDEWINDOW = 0x0080;
        const int SWP_NOACTIVATE = 0x0010;
        const int SWP_NOCOPYBITS = 0x0100;
        const int SWP_NOMOVE = 0x0002;
        const int SWP_NOOWNERZORDER = 0x0200;
        const int SWP_NOREDRAW = 0x0008;
        const int SWP_NOREPOSITION = 0x0200;
        const int SWP_NOSENDCHANGING = 0x0400;
        const int SWP_NOSIZE = 0x0001;
        const int SWP_NOZORDER = 0x0004;
        const int SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        public MainForm()
        {
            InitializeComponent();
        }
        
        private void SetTopMost_Click(object sender, EventArgs e)
        {
            setTopMost(CurrentWindow.Handle);
            setTopMost(this.Handle);
        }

        private void SetNoTopMost_Click(object sender, EventArgs e)
        {
            setNoTopMost(CurrentWindow.Handle);
        }


        Window CurrentWindow;
        private void MainForm_Load(object sender, EventArgs e)
        {
            setTopMost(this.Handle);
            
            GetWindows();
        }

        public void setTopMost(IntPtr hWnd)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

        }

        public void setNoTopMost(IntPtr hWnd)
        {
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
    }
}
