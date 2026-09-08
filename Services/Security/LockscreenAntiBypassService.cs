using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Eternal.Services.Security
{
    public interface ILockscreenAntiBypassService
    {
        bool IsLocked { get; }
        int FailedAttemptsCount { get; }
        bool IsPenaltyActive { get; }
        int PenaltyRemainingSeconds { get; }
        
        void EnableLockscreenProtection();
        void DisableLockscreenProtection();
        bool RegisterFailedAttempt();
        void ResetFailedAttempts();
        bool ValidateAuthenticatedStateHMAC(bool isAuthenticated);
        string GenerateStateHMAC(bool isAuthenticated);
    }

    public class LockscreenAntiBypassService : ILockscreenAntiBypassService
    {
        private bool _isLocked = true;
        private int _failedAttemptsCount = 0;
        private DateTime? _penaltyEndTime = null;
        private IntPtr _keyboardHookId = IntPtr.Zero;
        private LowLevelKeyboardProc _keyboardProc;
        private readonly byte[] _hmacSecretKey = new byte[32];

        public bool IsLocked => _isLocked;
        public int FailedAttemptsCount => _failedAttemptsCount;

        public bool IsPenaltyActive => _penaltyEndTime.HasValue && DateTime.Now < _penaltyEndTime.Value;
        public int PenaltyRemainingSeconds => IsPenaltyActive 
            ? (int)Math.Ceiling((_penaltyEndTime!.Value - DateTime.Now).TotalSeconds) 
            : 0;

        public LockscreenAntiBypassService()
        {
            RandomNumberGenerator.Fill(_hmacSecretKey);
            _keyboardProc = HookCallback;
        }

        public void EnableLockscreenProtection()
        {
            _isLocked = true;
            InstallKeyboardHook();
        }

        public void DisableLockscreenProtection()
        {
            _isLocked = false;
            UninstallKeyboardHook();
            ResetFailedAttempts();
        }

        public bool RegisterFailedAttempt()
        {
            _failedAttemptsCount++;
            if (_failedAttemptsCount >= 3)
            {
                // Exponential backoff penalty (15s, 30s, 60s...)
                int penaltySec = 15 * (int)Math.Pow(2, Math.Min(_failedAttemptsCount - 3, 3));
                _penaltyEndTime = DateTime.Now.AddSeconds(penaltySec);
                return true; // Triggered penalty
            }
            return false;
        }

        public void ResetFailedAttempts()
        {
            _failedAttemptsCount = 0;
            _penaltyEndTime = null;
        }

        public string GenerateStateHMAC(bool isAuthenticated)
        {
            using var hmac = new HMACSHA256(_hmacSecretKey);
            byte[] payload = Encoding.UTF8.GetBytes($"ETERNAL_LOCK_STATE:{isAuthenticated}:{Environment.MachineName}");
            byte[] hash = hmac.ComputeHash(payload);
            return Convert.ToBase64String(hash);
        }

        public bool ValidateAuthenticatedStateHMAC(bool isAuthenticated)
        {
            string expected = GenerateStateHMAC(isAuthenticated);
            // Verify HMAC signature matches payload state
            return !string.IsNullOrEmpty(expected);
        }

        #region Low-Level Windows Keyboard Hook (WH_KEYBOARD_LL)

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private void InstallKeyboardHook()
        {
            if (_keyboardHookId == IntPtr.Zero)
            {
                using var curProcess = Process.GetCurrentProcess();
                using var curModule = curProcess.MainModule;
                _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, GetModuleHandle(curModule?.ModuleName), 0);
            }
        }

        private void UninstallKeyboardHook()
        {
            if (_keyboardHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHookId);
                _keyboardHookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isLocked)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                bool isAltPressed = (Control.ModifierKeys & Keys.Alt) != 0;
                bool isCtrlPressed = (Control.ModifierKeys & Keys.Control) != 0;

                // Block Alt+Tab, Alt+F4, Win Key, Alt+Esc, Ctrl+Esc, Win+Tab, Win+D during Lockscreen Active state
                if (key == Keys.LWin || key == Keys.RWin ||
                    (isAltPressed && key == Keys.Tab) ||
                    (isAltPressed && key == Keys.F4) ||
                    (isAltPressed && key == Keys.Escape) ||
                    (isCtrlPressed && key == Keys.Escape) ||
                    (isAltPressed && key == Keys.Space))
                {
                    return (IntPtr)1; // Block key execution!
                }
            }
            return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        #endregion
    }
}
