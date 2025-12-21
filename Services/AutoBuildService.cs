using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MacroApp.Models;

namespace MacroApp.Services
{
    public class AutoBuildService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_XDOWN = 0x0080;
        private const uint MOUSEEVENTF_XUP = 0x0100;
        private const int RELEASE_CONFIRMATION_THRESHOLD = 5;
        private const int DEBOUNCE_FRAMES = 4;

        private AutoBuildConfiguration _configuration;
        private bool _isRunning = false;
        private Dictionary<string, byte> _keyMap;
        private Dictionary<string, string> _mouseButtonMap;
        private volatile bool _isWallPressed = false;
        private volatile bool _isStairsPressed = false;
        private volatile bool _isFloorPressed = false;
        private volatile bool _isRoofPressed = false;
        private volatile bool _isPlaceKeyHeld = false;
        private volatile int _releaseConfirmationCounter = 0;
        private volatile int _lastValidBuildingKeyFrame = 0;
        private volatile int _currentFrame = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _monitoringTask;

        public AutoBuildService(AutoBuildConfiguration configuration)
        {
            _configuration = configuration ?? new AutoBuildConfiguration();
            InitializeKeyMap();
        }

        private void InitializeKeyMap()
        {
            _keyMap = new Dictionary<string, byte>
            {
                { "A", 0x41 }, { "B", 0x42 }, { "C", 0x43 }, { "D", 0x44 },
                { "E", 0x45 }, { "F", 0x46 }, { "G", 0x47 }, { "H", 0x48 },
                { "I", 0x49 }, { "J", 0x4A }, { "K", 0x4B }, { "L", 0x4C },
                { "M", 0x4D }, { "N", 0x4E }, { "O", 0x4F }, { "P", 0x50 },
                { "Q", 0x51 }, { "R", 0x52 }, { "S", 0x53 }, { "T", 0x54 },
                { "U", 0x55 }, { "V", 0x56 }, { "W", 0x57 }, { "X", 0x58 },
                { "Y", 0x59 }, { "Z", 0x5A },
                { "0", 0x30 }, { "1", 0x31 }, { "2", 0x32 }, { "3", 0x33 },
                { "4", 0x34 }, { "5", 0x35 }, { "6", 0x36 }, { "7", 0x37 },
                { "8", 0x38 }, { "9", 0x39 },
                { "NUMPAD0", 0x60 }, { "NUMPAD1", 0x61 }, { "NUMPAD2", 0x62 },
                { "NUMPAD3", 0x63 }, { "NUMPAD4", 0x64 }, { "NUMPAD5", 0x65 },
                { "NUMPAD6", 0x66 }, { "NUMPAD7", 0x67 }, { "NUMPAD8", 0x68 },
                { "NUMPAD9", 0x69 },
                { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 },
                { "F5", 0x74 }, { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 },
                { "F9", 0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B },
                { "SPACE", 0x20 }, { "ENTER", 0x0D }, { "TAB", 0x09 },
                { "SHIFT", 0x10 }, { "CTRL", 0x11 }, { "ALT", 0x12 },
                { "UP", 0x26 }, { "DOWN", 0x28 }, { "LEFT", 0x25 }, { "RIGHT", 0x27 }
            };

            _mouseButtonMap = new Dictionary<string, string>
            {
                { "MOUSE X1", "X1" },
                { "MOUSE X2", "X2" },
                { "LEFT CLICK", "LEFT" },
                { "RIGHT CLICK", "RIGHT" },
                { "MIDDLE CLICK", "MIDDLE" }
            };
        }

        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitorKeys(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            _isRunning = false;
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            try
            {
                _monitoringTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch { }
        }

        private void MonitorKeys(CancellationToken cancellationToken)
        {
            string wallKey = _configuration.Wall;
            string stairsKey = _configuration.Stairs;
            string floorKey = _configuration.Floor;
            string roofKey = _configuration.Roof;
            string placeKey = _configuration.PlaceBuilding;

            int delayMs = (int)Math.Max(0, _configuration.Delay);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _currentFrame++;

                    bool wallCurrently = !string.IsNullOrEmpty(wallKey) && wallKey != "معطل" && IsKeyPressed(wallKey);
                    bool stairsCurrently = !string.IsNullOrEmpty(stairsKey) && stairsKey != "معطل" && IsKeyPressed(stairsKey);
                    bool floorCurrently = !string.IsNullOrEmpty(floorKey) && floorKey != "معطل" && IsKeyPressed(floorKey);
                    bool roofCurrently = !string.IsNullOrEmpty(roofKey) && roofKey != "معطل" && IsKeyPressed(roofKey);

                    bool anyBuildingKeyPressed = wallCurrently || stairsCurrently || floorCurrently || roofCurrently;

                    if (anyBuildingKeyPressed)
                    {
                        _lastValidBuildingKeyFrame = _currentFrame;

                        if (!_isPlaceKeyHeld)
                        {
                            _isPlaceKeyHeld = true;
                            _releaseConfirmationCounter = 0;
                            HoldKey(placeKey);
                        }
                        else
                        {
                            _releaseConfirmationCounter = 0;
                        }
                    }
                    else if (_isPlaceKeyHeld)
                    {
                        int frameDifference = _currentFrame - _lastValidBuildingKeyFrame;

                        if (frameDifference >= DEBOUNCE_FRAMES)
                        {
                            _releaseConfirmationCounter++;
                            if (_releaseConfirmationCounter >= RELEASE_CONFIRMATION_THRESHOLD)
                            {
                                _isPlaceKeyHeld = false;
                                _releaseConfirmationCounter = 0;
                                ReleaseKey(placeKey);
                            }
                        }
                    }

                    if (wallCurrently && !_isWallPressed)
                    {
                        _isWallPressed = true;
                    }
                    else if (!wallCurrently && _isWallPressed)
                    {
                        _isWallPressed = false;
                    }

                    if (stairsCurrently && !_isStairsPressed)
                    {
                        _isStairsPressed = true;
                    }
                    else if (!stairsCurrently && _isStairsPressed)
                    {
                        _isStairsPressed = false;
                    }

                    if (floorCurrently && !_isFloorPressed)
                    {
                        _isFloorPressed = true;
                    }
                    else if (!floorCurrently && _isFloorPressed)
                    {
                        _isFloorPressed = false;
                    }

                    if (roofCurrently && !_isRoofPressed)
                    {
                        _isRoofPressed = true;
                    }
                    else if (!roofCurrently && _isRoofPressed)
                    {
                        _isRoofPressed = false;
                    }

                    System.Threading.Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"خطأ في AutoBuildService.MonitorKeys: {ex.Message}");
                }
            }

            if (_isPlaceKeyHeld)
            {
                _isPlaceKeyHeld = false;
                ReleaseKey(placeKey);
            }
        }

        private byte GetKeyCode(string keyName)
        {
            if (string.IsNullOrEmpty(keyName) || keyName == "معطل")
                return 0;

            if (_keyMap.TryGetValue(keyName.ToUpper(), out byte keyCode))
                return keyCode;

            if (keyName.Length == 1 && byte.TryParse(keyName, out byte singleByte))
                return singleByte;

            return 0;
        }

        private bool IsKeyPressed(string keyName)
        {
            if (string.IsNullOrEmpty(keyName) || keyName == "معطل")
                return false;

            if (_mouseButtonMap.ContainsKey(keyName.ToUpper()))
            {
                return IsMouseButtonPressed(keyName.ToUpper());
            }

            byte keyCode = GetKeyCode(keyName);
            if (keyCode == 0)
                return false;

            try
            {
                return (GetAsyncKeyState(keyCode) & 0x8000) != 0;
            }
            catch
            {
                return false;
            }
        }

        private bool IsMouseButtonPressed(string mouseName)
        {
            mouseName = mouseName.ToUpper();

            try
            {
                if (mouseName == "MOUSE X1")
                    return (GetAsyncKeyState(0x05) & 0x8000) != 0;
                else if (mouseName == "MOUSE X2")
                    return (GetAsyncKeyState(0x06) & 0x8000) != 0;
                else if (mouseName == "LEFT CLICK")
                    return (GetAsyncKeyState(0x01) & 0x8000) != 0;
                else if (mouseName == "RIGHT CLICK")
                    return (GetAsyncKeyState(0x02) & 0x8000) != 0;
                else if (mouseName == "MIDDLE CLICK")
                    return (GetAsyncKeyState(0x04) & 0x8000) != 0;
            }
            catch
            {
            }

            return false;
        }

        private void HoldKey(string keyName)
        {
            if (string.IsNullOrEmpty(keyName) || keyName == "معطل")
                return;

            if (_mouseButtonMap.ContainsKey(keyName.ToUpper()))
            {
                HoldMouseButton(keyName.ToUpper());
            }
            else
            {
                byte keyCode = GetKeyCode(keyName);
                if (keyCode != 0)
                {
                    HoldKeyboard(keyCode);
                }
            }
        }

        private void ReleaseKey(string keyName)
        {
            if (string.IsNullOrEmpty(keyName) || keyName == "معطل")
                return;

            if (_mouseButtonMap.ContainsKey(keyName.ToUpper()))
            {
                ReleaseMouseButton(keyName.ToUpper());
            }
            else
            {
                byte keyCode = GetKeyCode(keyName);
                if (keyCode != 0)
                {
                    ReleaseKeyboard(keyCode);
                }
            }
        }

        private void HoldKeyboard(byte virtualKey)
        {
            try
            {
                uint scanCode = MapVirtualKey(virtualKey, 0);
                keybd_event(virtualKey, (byte)scanCode, 0, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في HoldKeyboard: {ex.Message}");
            }
        }

        private void ReleaseKeyboard(byte virtualKey)
        {
            try
            {
                uint scanCode = MapVirtualKey(virtualKey, 0);
                keybd_event(virtualKey, (byte)scanCode, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في ReleaseKeyboard: {ex.Message}");
            }
        }

        private void HoldMouseButton(string buttonName)
        {
            try
            {
                buttonName = buttonName.ToUpper();
                if (buttonName == "LEFT CLICK")
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                else if (buttonName == "RIGHT CLICK")
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                else if (buttonName == "MIDDLE CLICK")
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                else if (buttonName == "MOUSE X1" || buttonName == "MOUSE X2")
                {
                    uint xButton = (buttonName == "MOUSE X1") ? 1u : 2u;
                    mouse_event(MOUSEEVENTF_XDOWN, 0, 0, xButton, UIntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في HoldMouseButton: {ex.Message}");
            }
        }

        private void ReleaseMouseButton(string buttonName)
        {
            try
            {
                buttonName = buttonName.ToUpper();
                if (buttonName == "LEFT CLICK")
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                else if (buttonName == "RIGHT CLICK")
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                else if (buttonName == "MIDDLE CLICK")
                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                else if (buttonName == "MOUSE X1" || buttonName == "MOUSE X2")
                {
                    uint xButton = (buttonName == "MOUSE X1") ? 1u : 2u;
                    mouse_event(MOUSEEVENTF_XUP, 0, 0, xButton, UIntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في ReleaseMouseButton: {ex.Message}");
            }
        }

        public void UpdateConfiguration(AutoBuildConfiguration configuration)
        {
            _configuration = configuration ?? _configuration;
        }

        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
        }
    }
}
