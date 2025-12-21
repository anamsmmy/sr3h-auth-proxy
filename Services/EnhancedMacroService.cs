using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MacroApp.Models;

namespace MacroApp.Services
{
    public class EnhancedMacroService
    {
        // Windows API imports
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Keyboard event flags
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        // Hook constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private volatile bool _isRunning = false;
        private volatile bool _isSequenceRunning = false;
        private volatile bool _isKeyCurrentlyPressed = false;
        private volatile bool _hasExecutedStartSequence = false;
        private volatile bool _isHoldKeyPressed = false;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly object _lockObject = new object();

        // Hook variables
        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;
        private byte _currentActivationKey = 0;
        private volatile bool _shouldBlockActivationKey = false;
        private KeySequenceTrigger _currentKeySequence;

        public EnhancedMacroService()
        {
            _proc = HookCallback;
        }

        ~EnhancedMacroService()
        {
            UninstallHook();
        }

        // Key mapping for common keys
        private readonly Dictionary<string, byte> _keyMap = new Dictionary<string, byte>
        {
            // Letters
            {"A", 0x41}, {"B", 0x42}, {"C", 0x43}, {"D", 0x44}, {"E", 0x45},
            {"F", 0x46}, {"G", 0x47}, {"H", 0x48}, {"I", 0x49}, {"J", 0x4A},
            {"K", 0x4B}, {"L", 0x4C}, {"M", 0x4D}, {"N", 0x4E}, {"O", 0x4F},
            {"P", 0x50}, {"Q", 0x51}, {"R", 0x52}, {"S", 0x53}, {"T", 0x54},
            {"U", 0x55}, {"V", 0x56}, {"W", 0x57}, {"X", 0x58}, {"Y", 0x59}, {"Z", 0x5A},
            
            // Numbers
            {"0", 0x30}, {"1", 0x31}, {"2", 0x32}, {"3", 0x33}, {"4", 0x34},
            {"5", 0x35}, {"6", 0x36}, {"7", 0x37}, {"8", 0x38}, {"9", 0x39},
            
            // Function keys
            {"F1", 0x70}, {"F2", 0x71}, {"F3", 0x72}, {"F4", 0x73}, {"F5", 0x74},
            {"F6", 0x75}, {"F7", 0x76}, {"F8", 0x77}, {"F9", 0x78}, {"F10", 0x79},
            {"F11", 0x7A}, {"F12", 0x7B},
            
            // Special keys
            {"Space", 0x20}, {"Enter", 0x0D}, {"Escape", 0x1B}, {"Tab", 0x09},
            {"Backspace", 0x08}, {"Delete", 0x2E}, {"Insert", 0x2D},
            {"Home", 0x24}, {"End", 0x23}, {"PageUp", 0x21}, {"PageDown", 0x22},
            
            // Arrow keys
            {"Up", 0x26}, {"Down", 0x28}, {"Left", 0x25}, {"Right", 0x27},
            
            // Modifiers
            {"Shift", 0x10}, {"Ctrl", 0x11}, {"Alt", 0x12},
            
            // NumPad
            {"NumPad0", 0x60}, {"NumPad1", 0x61}, {"NumPad2", 0x62}, {"NumPad3", 0x63},
            {"NumPad4", 0x64}, {"NumPad5", 0x65}, {"NumPad6", 0x66}, {"NumPad7", 0x67},
            {"NumPad8", 0x68}, {"NumPad9", 0x69}
        };

                private void ReportStatus(string code, string detail = null)
        {
            StatusChanged?.Invoke(this, new MacroStatusEventArgs(code, detail));
        }

public bool IsRunning => _isRunning;

        public event EventHandler<MacroStatusEventArgs> StatusChanged;

        private void InstallHook()
        {
            if (_hookID == IntPtr.Zero)
            {
                using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
                using (var curModule = curProcess.MainModule)
                {
                    _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        private void UninstallHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _shouldBlockActivationKey)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                
                // Check if this is our activation key
                if (vkCode == _currentActivationKey)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                    {
                        // First key press - allow it and trigger macro
                        if (!_isKeyCurrentlyPressed)
                        {
                            _isKeyCurrentlyPressed = true;
                            // Allow the first key press to go through
                            // Then trigger the rest of the sequence asynchronously
                            Task.Run(async () =>
                            {
                                try
                                {
                                    // Small delay to let the first key register
                                    await Task.Delay(10);
                                    await ExecuteRestOfSequenceAsync(_currentKeySequence, _cancellationTokenSource?.Token ?? CancellationToken.None);
                                }
                                catch (Exception ex)
                                {
                ReportStatus("error.sequence.rest", ex.Message);
                                }
                            });
                            
                            // Allow the first key press to go through
                            return CallNextHookEx(_hookID, nCode, wParam, lParam);
                        }
                        else
                        {
                            // Block auto-repeat events for the activation key while it is held
                            return (IntPtr)1;
                        }
                    }
                    else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                    {
                        // Key released - trigger end sequence
                        if (_isKeyCurrentlyPressed)
                        {
                            _isKeyCurrentlyPressed = false;
                            // Trigger end sequence asynchronously
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await ExecuteEndSequenceAsync(_currentKeySequence, _cancellationTokenSource?.Token ?? CancellationToken.None);
                                }
                                catch (Exception ex)
                                {
                ReportStatus("error.sequence.end", ex.Message);
                                }
                            });
                        }
                        
                        // Allow the key release to go through
                        return CallNextHookEx(_hookID, nCode, wParam, lParam);
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        public async Task StartMacroAsync(KeySequenceTrigger keySequence)
        {
            if (_isRunning)
            {
                ReportStatus("info.macro.alreadyRunning");
                return;
            }

            lock (_lockObject)
            {
                if (_isRunning) return;
                _isRunning = true;
                _cancellationTokenSource = new CancellationTokenSource();
            }

            // Setup hook for activation key
            _currentActivationKey = GetVirtualKeyCode(keySequence.ActivationKey);
            _currentKeySequence = keySequence;
            _shouldBlockActivationKey = true;
            InstallHook();

            ReportStatus("info.macro.waitingForActivation");

            try
            {
                // Hook will handle everything, just wait for cancellation
                await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            ReportStatus("info.macro.stopped");
            }
            catch (Exception ex)
            {
            ReportStatus("error.macro.loop", ex.Message);
            }
            finally
            {
                lock (_lockObject)
                {
                    // ط¥ظپظ„ط§طھ ط£ظٹ ظ…ظپط§طھظٹط­ ظ…ط¶ط؛ظˆط·ط© ظ‚ط¨ظ„ ط§ظ„طھظ†ط¸ظٹظپ
                    if (_isHoldKeyPressed && _currentKeySequence != null && !string.IsNullOrEmpty(_currentKeySequence.HoldKey))
                    {
                        var holdKey = GetVirtualKeyCode(_currentKeySequence.HoldKey);
                        if (holdKey != 0)
                        {
                            ReleaseKey(holdKey);
                        }
                    }
                    
                    _isRunning = false;
                    _isKeyCurrentlyPressed = false;
                    _hasExecutedStartSequence = false;
                    _isHoldKeyPressed = false;
                    _shouldBlockActivationKey = false;
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
                
                // Remove hook
                UninstallHook();
            }
        }

        public void StopMacro()
        {
            lock (_lockObject)
            {
                if (!_isRunning) return;
                
                // ط¥ظپظ„ط§طھ ط£ظٹ ظ…ظپط§طھظٹط­ ظ…ط¶ط؛ظˆط·ط© ظ‚ط¨ظ„ ط§ظ„ط¥ظٹظ‚ط§ظپ
                if (_isHoldKeyPressed && _currentKeySequence != null && !string.IsNullOrEmpty(_currentKeySequence.HoldKey))
                {
                    var holdKey = GetVirtualKeyCode(_currentKeySequence.HoldKey);
                    if (holdKey != 0)
                    {
                        ReleaseKey(holdKey);
                        _isHoldKeyPressed = false;
                        ReportStatus("info.hold.released", _currentKeySequence.HoldKey);
                    }
                }
                
                _cancellationTokenSource?.Cancel();
                ReportStatus("info.macro.stopping");
            }
        }

        private async Task RunMacroLoopAsync(KeySequenceTrigger keySequence, CancellationToken cancellationToken)
        {
            var activationKey = GetVirtualKeyCode(keySequence.ActivationKey);
            if (activationKey == 0)
            {
            ReportStatus("error.activationKey.invalid");
                return;
            }

            ReportStatus("info.macro.ready", keySequence.ActivationKey);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    bool isCurrentlyPressed = IsKeyPressed(activationKey);
                    
                    // Key just pressed (transition from not pressed to pressed)
                    if (isCurrentlyPressed && !_isKeyCurrentlyPressed)
                    {
                        _isKeyCurrentlyPressed = true;
                        _hasExecutedStartSequence = false;
                        
                        // Execute start sequence (PreHold + Hold)
                        if (!_isSequenceRunning)
                        {
                            await ExecuteStartSequenceAsync(keySequence, cancellationToken);
                        }
                    }
                    // Key just released (transition from pressed to not pressed)
                    else if (!isCurrentlyPressed && _isKeyCurrentlyPressed)
                    {
                        _isKeyCurrentlyPressed = false;
                        
                        // Execute end sequence (Release)
                        if (_hasExecutedStartSequence)
                        {
                            await ExecuteEndSequenceAsync(keySequence, cancellationToken);
                        }
                    }

                    // Small delay to prevent excessive CPU usage
                    await Task.Delay(10, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ReportStatus("error.macro.loop", ex.Message);
                    await Task.Delay(100, cancellationToken);
                }
            }
        }

        private async Task ExecuteStartSequenceAsync(KeySequenceTrigger sequence, CancellationToken cancellationToken)
        {
            if (!sequence.IsEnabled)
                return;

            _isSequenceRunning = true;

            try
            {
                ReportStatus("info.sequence.starting");

                // Validate delay
                var delay = Math.Max(1, sequence.Delay);

                // ActivationKey will be handled by Hook, no need to press it here

                // Step 1: Pre-Hold Key
                if (!string.IsNullOrEmpty(sequence.PreHoldKey))
                {
                    var preHoldKey = GetVirtualKeyCode(sequence.PreHoldKey);
                    if (preHoldKey != 0)
                    {
                        PressKey(preHoldKey);
                        ReportStatus("info.sequence.prehold", sequence.PreHoldKey);
                        await Task.Delay(delay, cancellationToken);
                    }
                }

                // Step 2: Hold Key (only once, no repetition)
                if (!string.IsNullOrEmpty(sequence.HoldKey))
                {
                    var holdKey = GetVirtualKeyCode(sequence.HoldKey);
                    if (holdKey != 0)
                    {
                        PressKey(holdKey);
                        ReportStatus("info.sequence.holdPressed", sequence.HoldKey);
                        await Task.Delay(delay, cancellationToken);
                    }
                }

                _hasExecutedStartSequence = true;
                ReportStatus("info.sequence.started");
            }
            catch (OperationCanceledException)
            {
                ReportStatus("info.sequence.cancelled");
            }
            catch (Exception ex)
            {
                ReportStatus("error.sequence.start", ex.Message);
            }
            finally
            {
                _isSequenceRunning = false;
            }
        }

        private async Task ExecuteRestOfSequenceAsync(KeySequenceTrigger sequence, CancellationToken cancellationToken)
        {
            if (!sequence.IsEnabled)
                return;

            _isSequenceRunning = true;

            try
            {
                ReportStatus("info.sequence.running");

                // Validate delay
                var delay = Math.Max(1, sequence.Delay);

                // Step 1: Pre-Hold Key (Q) - ط¶ط؛ط· ظˆط¥ظپظ„ط§طھ ظپظˆط±ظٹ
                if (!string.IsNullOrEmpty(sequence.PreHoldKey))
                {
                    var preHoldKey = GetVirtualKeyCode(sequence.PreHoldKey);
                    if (preHoldKey != 0)
                    {
                        PressKey(preHoldKey);
                        ReportStatus("info.sequence.prehold", sequence.PreHoldKey);
                        await Task.Delay(delay, cancellationToken);
                    }
                }

                // Step 2: Hold Key (P) - ط¶ط؛ط· ظ…ط³طھظ…ط± ط¨ط¯ظˆظ† ط¥ظپظ„ط§طھ
                if (!string.IsNullOrEmpty(sequence.HoldKey))
                {
                    var holdKey = GetVirtualKeyCode(sequence.HoldKey);
                    if (holdKey != 0)
                    {
                        PressKeyDown(holdKey); // ط¶ط؛ط· ط¨ط¯ظˆظ† ط¥ظپظ„ط§طھ
                        _isHoldKeyPressed = true;
                        ReportStatus("info.sequence.holdLocked", sequence.HoldKey);
                        await Task.Delay(delay, cancellationToken);
                    }
                }

                _hasExecutedStartSequence = true;
                ReportStatus("info.sequence.combination", $"{sequence.ActivationKey} + {sequence.HoldKey}");
            }
            catch (OperationCanceledException)
            {
                ReportStatus("info.sequence.completed");
            }
            catch (Exception ex)
            {
                ReportStatus("error.sequence.execution", ex.Message);
            }
            finally
            {
                _isSequenceRunning = false;
            }
        }

        private async Task ExecuteEndSequenceAsync(KeySequenceTrigger sequence, CancellationToken cancellationToken)
        {
            if (!sequence.IsEnabled)
                return;

            _isSequenceRunning = true;

            try
            {
                ReportStatus("info.sequence.ending", sequence.ActivationKey);

                // Validate delay
                var delay = Math.Max(1, sequence.Delay);

                // Step 1: ط¥ظپظ„ط§طھ Hold Key ط£ظˆظ„ط§ظ‹ ط¥ط°ط§ ظƒط§ظ† ظ…ط¶ط؛ظˆط·ط§ظ‹
                if (_isHoldKeyPressed && !string.IsNullOrEmpty(sequence.HoldKey))
                {
                    var holdKey = GetVirtualKeyCode(sequence.HoldKey);
                    if (holdKey != 0)
                    {
                        ReleaseKey(holdKey);
                        _isHoldKeyPressed = false;
                        ReportStatus("info.hold.released", sequence.HoldKey);
                        await Task.Delay(delay, cancellationToken);
                    }
                }

                // Step 2: ط¶ط؛ط· Release Key
                if (!string.IsNullOrEmpty(sequence.ReleaseKey))
                {
                    var releaseKey = GetVirtualKeyCode(sequence.ReleaseKey);
                    if (releaseKey != 0)
                    {
                        PressKey(releaseKey);
                        ReportStatus("info.release.pressed", sequence.ReleaseKey);
                        await Task.Delay(delay, cancellationToken);
                    }
                }

                ReportStatus("info.sequence.ended");
            }
            catch (OperationCanceledException)
            {
                ReportStatus("info.sequence.cancelled");
            }
            catch (Exception ex)
            {
                ReportStatus("error.sequence.execution", ex.Message);
            }
            finally
            {
                _isSequenceRunning = false;
                _hasExecutedStartSequence = false;
                _isHoldKeyPressed = false;
            }
        }

        private void PressKey(byte virtualKey)
        {
            try
            {
                // Get scan code
                uint scanCode = MapVirtualKey(virtualKey, 0);
                
                // Press key down
                keybd_event(virtualKey, (byte)scanCode, 0, UIntPtr.Zero);
                
                // Small delay between press and release
                Thread.Sleep(1);
                
                // Release key
                keybd_event(virtualKey, (byte)scanCode, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                ReportStatus("error.input.pressKey", $"{virtualKey}: {ex.Message}");
            }
        }

        private void PressKeyDown(byte virtualKey)
        {
            try
            {
                // Get scan code
                uint scanCode = MapVirtualKey(virtualKey, 0);
                
                // Press key down only (don't release)
                keybd_event(virtualKey, (byte)scanCode, 0, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                ReportStatus("error.input.releaseKey", $"{virtualKey}: {ex.Message}");
            }
        }

        private void ReleaseKey(byte virtualKey)
        {
            try
            {
                // Get scan code
                uint scanCode = MapVirtualKey(virtualKey, 0);
                
                // Release key only
                keybd_event(virtualKey, (byte)scanCode, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                ReportStatus("error.sequence.general", ex.Message);
            }
        }

        private bool IsKeyPressed(byte virtualKey)
        {
            try
            {
                return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
            }
            catch
            {
                return false;
            }
        }

        private byte GetVirtualKeyCode(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                return 0;

            // Handle disabled keys
            if (keyName == "معطل")
                return 0;

            // Try direct mapping first
            if (_keyMap.TryGetValue(keyName, out byte virtualKey))
                return virtualKey;

            // Try single character keys
            if (keyName.Length == 1)
            {
                char c = char.ToUpper(keyName[0]);
                if (c >= 'A' && c <= 'Z')
                    return (byte)c;
                if (c >= '0' && c <= '9')
                    return (byte)c;
            }

            // Handle mouse buttons (return 0 as they're not keyboard keys)
            if (keyName.Contains("Click") || keyName.Contains("Mouse"))
                return 0;

            return 0;
        }

        public void Dispose()
        {
            StopMacro();
        }

        // Test method for validation
        public async Task<bool> TestKeySequenceAsync(KeySequenceTrigger sequence)
        {
            try
            {
                ReportStatus("info.test.running");

                // Validate all keys
                var activationKey = GetVirtualKeyCode(sequence.ActivationKey);
                var preHoldKey = GetVirtualKeyCode(sequence.PreHoldKey);
                var holdKey = GetVirtualKeyCode(sequence.HoldKey);
                var releaseKey = GetVirtualKeyCode(sequence.ReleaseKey);

                if (activationKey == 0)
                {
                    ReportStatus("error.test.activationKey");
                    return false;
                }

                // Validate delay
                if (sequence.Delay < 1 || sequence.Delay > 10000)
                {
                    ReportStatus("error.test.delay");
                    return false;
                }

                ReportStatus("info.test.success");
                return true;
            }
            catch (Exception ex)
            {
                ReportStatus("error.test.exception", ex.Message);
                return false;
            }
        }
    }
}



