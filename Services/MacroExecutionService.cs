using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MacroApp.Models;

namespace MacroApp.Services
{
    public class MacroExecutionService
    {
        // Windows API imports
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // Mouse event flags
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

        // Keyboard event flags
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private readonly Dictionary<string, CancellationTokenSource> _runningMacros;
        private readonly object _lockObject = new object();

        public MacroExecutionService()
        {
            _runningMacros = new Dictionary<string, CancellationTokenSource>();
        }

        public async Task ExecuteKeySequenceTriggerAsync(KeySequenceTrigger trigger)
        {
            if (!trigger.IsEnabled)
                return;

            try
            {
                // Execute Pre-Hold Key
                if (!string.IsNullOrEmpty(trigger.PreHoldKey))
                {
                    PressKey(trigger.PreHoldKey);
                    await Task.Delay(trigger.Delay);
                }

                // Execute Hold Key
                if (!string.IsNullOrEmpty(trigger.HoldKey))
                {
                    PressKeyDown(trigger.HoldKey);
                    await Task.Delay(trigger.Delay);
                }

                // Execute Activation Key
                if (!string.IsNullOrEmpty(trigger.ActivationKey))
                {
                    PressKey(trigger.ActivationKey);
                    await Task.Delay(trigger.Delay);
                }

                // Release Hold Key
                if (!string.IsNullOrEmpty(trigger.HoldKey))
                {
                    ReleaseKey(trigger.HoldKey);
                    await Task.Delay(trigger.Delay);
                }

                // Execute Release Key
                if (!string.IsNullOrEmpty(trigger.ReleaseKey))
                {
                    PressKey(trigger.ReleaseKey);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error executing key sequence trigger: {ex.Message}", ex);
            }
        }

        public async Task ExecuteKeyboardToMouseMacroAsync(KeyboardToMouseMacro macro, string macroId)
        {
            if (!macro.IsEnabled)
                return;

            lock (_lockObject)
            {
                if (_runningMacros.ContainsKey(macroId))
                {
                    _runningMacros[macroId].Cancel();
                    _runningMacros.Remove(macroId);
                }

                var cancellationTokenSource = new CancellationTokenSource();
                _runningMacros[macroId] = cancellationTokenSource;
            }

            try
            {
                var cancellationToken = _runningMacros[macroId].Token;

                switch (macro.Mode)
                {
                    case MacroMode.RepeatWhileHolding:
                        await ExecuteRepeatWhileHoldingAsync(macro, macroId, cancellationToken);
                        break;
                    case MacroMode.NoRepeat:
                        await ExecuteActionsOnceAsync(macro.Actions, cancellationToken);
                        break;
                    case MacroMode.Toggle:
                        await ExecuteToggleModeAsync(macro, macroId, cancellationToken);
                        break;
                    case MacroMode.Sequence:
                        await ExecuteSequenceModeAsync(macro.Actions, cancellationToken);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // Macro was cancelled, this is expected
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error executing keyboard to mouse macro: {ex.Message}", ex);
            }
            finally
            {
                lock (_lockObject)
                {
                    if (_runningMacros.ContainsKey(macroId))
                        _runningMacros.Remove(macroId);
                }
            }
        }

        public async Task ExecuteMouseToKeyboardMacroAsync(MouseToKeyboardMacro macro, string macroId)
        {
            if (!macro.IsEnabled)
                return;

            lock (_lockObject)
            {
                if (_runningMacros.ContainsKey(macroId))
                {
                    _runningMacros[macroId].Cancel();
                    _runningMacros.Remove(macroId);
                }

                var cancellationTokenSource = new CancellationTokenSource();
                _runningMacros[macroId] = cancellationTokenSource;
            }

            try
            {
                var cancellationToken = _runningMacros[macroId].Token;

                switch (macro.Mode)
                {
                    case MacroMode.RepeatWhileHolding:
                        await ExecuteRepeatWhileHoldingMouseAsync(macro, macroId, cancellationToken);
                        break;
                    case MacroMode.NoRepeat:
                        await ExecuteActionsOnceAsync(macro.Actions, cancellationToken);
                        break;
                    case MacroMode.Toggle:
                        await ExecuteToggleMouseModeAsync(macro, macroId, cancellationToken);
                        break;
                    case MacroMode.Sequence:
                        await ExecuteSequenceModeAsync(macro.Actions, cancellationToken);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // Macro was cancelled, this is expected
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error executing mouse to keyboard macro: {ex.Message}", ex);
            }
            finally
            {
                lock (_lockObject)
                {
                    if (_runningMacros.ContainsKey(macroId))
                        _runningMacros.Remove(macroId);
                }
            }
        }

        private async Task ExecuteRepeatWhileHoldingAsync(KeyboardToMouseMacro macro, string macroId, CancellationToken cancellationToken)
        {
            var keyCode = GetKeyCode(macro.TriggerKey);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsKeyPressed(keyCode))
                {
                    await ExecuteActionsOnceAsync(macro.Actions, cancellationToken);
                }
                else
                {
                    break; // Key released, stop execution
                }
                
                await Task.Delay(1, cancellationToken); // Small delay to prevent excessive CPU usage
            }
        }

        private async Task ExecuteRepeatWhileHoldingMouseAsync(MouseToKeyboardMacro macro, string macroId, CancellationToken cancellationToken)
        {
            var mouseButton = GetMouseButtonCode(macro.MouseButton);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsMouseButtonPressed(mouseButton))
                {
                    await ExecuteActionsOnceAsync(macro.Actions, cancellationToken);
                }
                else
                {
                    break; // Button released, stop execution
                }
                
                await Task.Delay(1, cancellationToken);
            }
        }

        private async Task ExecuteToggleModeAsync(KeyboardToMouseMacro macro, string macroId, CancellationToken cancellationToken)
        {
            bool isRunning = false;
            var keyCode = GetKeyCode(macro.TriggerKey);
            bool wasPressed = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                bool isPressed = IsKeyPressed(keyCode);
                
                if (isPressed && !wasPressed) // Key just pressed
                {
                    isRunning = !isRunning; // Toggle state
                }
                
                wasPressed = isPressed;

                if (isRunning)
                {
                    await ExecuteActionsOnceAsync(macro.Actions, cancellationToken);
                }

                await Task.Delay(10, cancellationToken);
            }
        }

        private async Task ExecuteToggleMouseModeAsync(MouseToKeyboardMacro macro, string macroId, CancellationToken cancellationToken)
        {
            bool isRunning = false;
            var mouseButton = GetMouseButtonCode(macro.MouseButton);
            bool wasPressed = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                bool isPressed = IsMouseButtonPressed(mouseButton);
                
                if (isPressed && !wasPressed) // Button just pressed
                {
                    isRunning = !isRunning; // Toggle state
                }
                
                wasPressed = isPressed;

                if (isRunning)
                {
                    await ExecuteActionsOnceAsync(macro.Actions, cancellationToken);
                }

                await Task.Delay(10, cancellationToken);
            }
        }

        private async Task ExecuteSequenceModeAsync(IEnumerable<MacroAction> actions, CancellationToken cancellationToken)
        {
            await ExecuteActionsOnceAsync(actions, cancellationToken);
        }

        private async Task ExecuteActionsOnceAsync(IEnumerable<MacroAction> actions, CancellationToken cancellationToken)
        {
            foreach (var action in actions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                await ExecuteActionAsync(action, cancellationToken);
            }
        }

        private async Task ExecuteActionAsync(MacroAction action, CancellationToken cancellationToken)
        {
            switch (action.ActionType)
            {
                case ActionType.MouseLeftDown:
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    if (action.Duration > 0)
                    {
                        await Task.Delay(action.Duration, cancellationToken);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    }
                    break;

                case ActionType.MouseLeftUp:
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    break;

                case ActionType.MouseRightDown:
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                    if (action.Duration > 0)
                    {
                        await Task.Delay(action.Duration, cancellationToken);
                        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                    }
                    break;

                case ActionType.MouseRightUp:
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                    break;

                case ActionType.MouseMiddleDown:
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                    if (action.Duration > 0)
                    {
                        await Task.Delay(action.Duration, cancellationToken);
                        mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                    }
                    break;

                case ActionType.MouseMiddleUp:
                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                    break;

                case ActionType.KeyDown:
                    if (!string.IsNullOrEmpty(action.KeyCode))
                    {
                        var keyCode = GetKeyCode(action.KeyCode);
                        keybd_event((byte)keyCode, 0, 0, UIntPtr.Zero);
                        if (action.Duration > 0)
                        {
                            await Task.Delay(action.Duration, cancellationToken);
                            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        }
                    }
                    break;

                case ActionType.KeyUp:
                    if (!string.IsNullOrEmpty(action.KeyCode))
                    {
                        var keyCode = GetKeyCode(action.KeyCode);
                        keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    }
                    break;

                case ActionType.Delay:
                    await Task.Delay(action.Delay, cancellationToken);
                    break;
            }

            if (action.Delay > 0)
            {
                await Task.Delay(action.Delay, cancellationToken);
            }
        }

        private void PressKey(string key)
        {
            var keyCode = GetKeyCode(key);
            keybd_event((byte)keyCode, 0, 0, UIntPtr.Zero);
            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private void PressKeyDown(string key)
        {
            var keyCode = GetKeyCode(key);
            keybd_event((byte)keyCode, 0, 0, UIntPtr.Zero);
        }

        private void ReleaseKey(string key)
        {
            var keyCode = GetKeyCode(key);
            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private bool IsKeyPressed(int keyCode)
        {
            return (GetAsyncKeyState(keyCode) & 0x8000) != 0;
        }

        private bool IsMouseButtonPressed(int buttonCode)
        {
            return (GetAsyncKeyState(buttonCode) & 0x8000) != 0;
        }

        private int GetKeyCode(string key)
        {
            if (string.IsNullOrEmpty(key))
                return 0;

            // Handle disabled keys
            if (key == "معطل")
                return 0;

            key = key.ToUpper();
            
            return key switch
            {
                "A" => 0x41,
                "B" => 0x42,
                "C" => 0x43,
                "D" => 0x44,
                "E" => 0x45,
                "F" => 0x46,
                "G" => 0x47,
                "H" => 0x48,
                "I" => 0x49,
                "J" => 0x4A,
                "K" => 0x4B,
                "L" => 0x4C,
                "M" => 0x4D,
                "N" => 0x4E,
                "O" => 0x4F,
                "P" => 0x50,
                "Q" => 0x51,
                "R" => 0x52,
                "S" => 0x53,
                "T" => 0x54,
                "U" => 0x55,
                "V" => 0x56,
                "W" => 0x57,
                "X" => 0x58,
                "Y" => 0x59,
                "Z" => 0x5A,
                "SPACE" => 0x20,
                "ENTER" => 0x0D,
                "SHIFT" => 0x10,
                "CTRL" => 0x11,
                "ALT" => 0x12,
                _ => 0
            };
        }

        private int GetMouseButtonCode(string button)
        {
            if (string.IsNullOrEmpty(button))
                return 0;

            return button.ToUpper() switch
            {
                "LEFT" => 0x01,
                "RIGHT" => 0x02,
                "MIDDLE" => 0x04,
                "G4" => 0x05,
                "G5" => 0x06,
                _ => 0
            };
        }

        public async Task StartMacro(MacroConfiguration configuration)
        {
            try
            {
                // إيقاف أي ماكرو يعمل حالياً
                StopAllMacros();
                
                // بدء مراقبة الأزرار والمفاتيح في مهمة منفصلة
                _ = Task.Run(() => MonitorInputs(configuration));
                
                System.Diagnostics.Debug.WriteLine("Macro monitoring started successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting macro: {ex.Message}");
                throw;
            }
        }

        private async Task MonitorInputs(MacroConfiguration configuration)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var macroId = "main_macro";
            
            lock (_lockObject)
            {
                _runningMacros[macroId] = cancellationTokenSource;
            }

            try
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    // مراقبة KeyboardToMouse
                    if (configuration.KeyboardToMouse.IsEnabled)
                    {
                        var triggerKey = GetKeyCode(configuration.KeyboardToMouse.TriggerKey);
                        if (triggerKey > 0 && IsKeyPressed(triggerKey))
                        {
                            if (configuration.KeyboardToMouse.PrimaryClick)
                                await ExecuteMouseAction("LEFT_CLICK");
                            if (configuration.KeyboardToMouse.SecondaryClick)
                                await ExecuteMouseAction("RIGHT_CLICK");
                            await Task.Delay(50); // منع التكرار السريع
                        }
                    }

                    // مراقبة MouseToKeyboard
                    if (configuration.MouseToKeyboard.IsEnabled)
                    {
                        var mouseButton = GetMouseButtonCode(configuration.MouseToKeyboard.MouseButton);
                        if (mouseButton > 0 && IsMouseButtonPressed(mouseButton))
                        {
                            await ExecuteKeyAction(configuration.MouseToKeyboard.TargetKey);
                            await Task.Delay(50); // منع التكرار السريع
                        }
                    }

                    // مراقبة KeySequence
                    if (configuration.KeySequence.IsEnabled)
                    {
                        var triggerKey = GetKeyCode(configuration.KeySequence.ActivationKey);
                        if (triggerKey > 0 && IsKeyPressed(triggerKey))
                        {
                            await ExecuteKeySequenceAsync(configuration.KeySequence);
                            await Task.Delay(configuration.KeySequence.Delay);
                        }
                    }

                    await Task.Delay(10); // تقليل استهلاك المعالج
                }
            }
            catch (OperationCanceledException)
            {
                // تم إلغاء العملية بشكل طبيعي
            }
            finally
            {
                lock (_lockObject)
                {
                    _runningMacros.Remove(macroId);
                }
            }
        }

        private async Task ExecuteMouseAction(string mouseAction)
        {
            switch (mouseAction?.ToUpper())
            {
                case "LEFT_CLICK":
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    await Task.Delay(10);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    break;
                case "RIGHT_CLICK":
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                    await Task.Delay(10);
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                    break;
                case "MIDDLE_CLICK":
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                    await Task.Delay(10);
                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                    break;
            }
        }

        private async Task ExecuteKeyAction(string key)
        {
            var keyCode = GetKeyCode(key);
            if (keyCode > 0)
            {
                keybd_event((byte)keyCode, 0, 0, UIntPtr.Zero); // Key down
                await Task.Delay(10);
                keybd_event((byte)keyCode, 0, 2, UIntPtr.Zero); // Key up
            }
        }

        private async Task ExecuteKeySequenceAsync(KeySequenceTrigger keySequence)
        {
            // تنفيذ تسلسل المفاتيح
            if (!string.IsNullOrEmpty(keySequence.PreHoldKey))
            {
                await ExecuteKeyAction(keySequence.PreHoldKey);
                await Task.Delay(keySequence.Delay);
            }
            
            if (!string.IsNullOrEmpty(keySequence.HoldKey))
            {
                var keyCode = GetKeyCode(keySequence.HoldKey);
                if (keyCode > 0)
                {
                    keybd_event((byte)keyCode, 0, 0, UIntPtr.Zero); // Key down
                    await Task.Delay(keySequence.Delay);
                }
            }
            
            if (!string.IsNullOrEmpty(keySequence.ActivationKey))
            {
                await ExecuteKeyAction(keySequence.ActivationKey);
                await Task.Delay(keySequence.Delay);
            }
            
            if (!string.IsNullOrEmpty(keySequence.HoldKey))
            {
                var keyCode = GetKeyCode(keySequence.HoldKey);
                if (keyCode > 0)
                {
                    keybd_event((byte)keyCode, 0, 2, UIntPtr.Zero); // Key up
                    await Task.Delay(keySequence.Delay);
                }
            }
            
            if (!string.IsNullOrEmpty(keySequence.ReleaseKey))
            {
                await ExecuteKeyAction(keySequence.ReleaseKey);
            }
        }

        public void StopMacro(string macroId)
        {
            lock (_lockObject)
            {
                if (_runningMacros.ContainsKey(macroId))
                {
                    _runningMacros[macroId].Cancel();
                    _runningMacros.Remove(macroId);
                }
            }
        }

        public void StopAllMacros()
        {
            lock (_lockObject)
            {
                foreach (var cancellationTokenSource in _runningMacros.Values)
                {
                    cancellationTokenSource.Cancel();
                }
                _runningMacros.Clear();
            }
        }

        public void Dispose()
        {
            StopAllMacros();
        }
    }
}