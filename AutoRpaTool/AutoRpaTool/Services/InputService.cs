using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoRpaTool.Services
{
    /// <summary>
    /// Mô phỏng input chuột và bàn phím qua Win32 P/Invoke.
    /// </summary>
    public class InputService
    {
        #region P/Invoke definitions

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx, dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        #endregion

        /// <summary>Di chuyển cursor và click chuột trái tại (x, y).</summary>
        public void Click(int x, int y)
        {
            SetCursorPos(x, y);
            Thread.Sleep(100); // Đợi cursor dịch chuyển
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, IntPtr.Zero);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, IntPtr.Zero);
        }

        /// <summary>Gõ một chuỗi văn bản bằng SendInput Unicode.</summary>
        public void Type(string text)
        {
            foreach (char c in text)
            {
                var inputs = new INPUT[]
                {
                    new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        u = new INPUTUNION
                        {
                            ki = new KEYBDINPUT
                            {
                                wScan = c,
                                dwFlags = KEYEVENTF_UNICODE
                            }
                        }
                    },
                    new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        u = new INPUTUNION
                        {
                            ki = new KEYBDINPUT
                            {
                                wScan = c,
                                dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
                            }
                        }
                    }
                };
                SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
                Thread.Sleep(20);
            }
        }
    }
}
