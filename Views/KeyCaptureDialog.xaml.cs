using System;
using System.Windows;
using System.Windows.Input;

namespace MacroApp.Views
{
    public partial class KeyCaptureDialog : Window
    {
        public string SelectedKey { get; private set; }
        public bool IsMouseButton { get; private set; }
        public bool AllowEmpty { get; private set; }

        public KeyCaptureDialog(bool allowEmpty = false)
        {
            InitializeComponent();
            AllowEmpty = allowEmpty;
            
            if (AllowEmpty)
            {
                ClearButton.Visibility = Visibility.Visible;
            }
            
            this.Loaded += (s, e) => this.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // تجاهل مفاتيح النظام
            if (e.Key == Key.Tab || e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || e.Key == Key.LeftShift ||
                e.Key == Key.RightShift || e.Key == Key.LWin || e.Key == Key.RWin)
            {
                return;
            }

            // إلغاء بـ Escape
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                return;
            }

            var keyName = GetKeyName(e.Key);
            if (!string.IsNullOrEmpty(keyName))
            {
                SelectedKey = keyName;
                IsMouseButton = false;
                
                DetectedKeyText.Text = $"🎹 {keyName}";
                InstructionText.Text = "تم تحديد الزر!";
                OkButton.IsEnabled = true;
                
                e.Handled = true;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string buttonName = "";
            
            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    buttonName = "Left Click";
                    break;
                case MouseButton.Right:
                    buttonName = "Right Click";
                    break;
                case MouseButton.Middle:
                    buttonName = "Middle Click";
                    break;
                case MouseButton.XButton1:
                    buttonName = "Mouse X1";
                    break;
                case MouseButton.XButton2:
                    buttonName = "Mouse X2";
                    break;
            }

            if (!string.IsNullOrEmpty(buttonName))
            {
                SelectedKey = buttonName;
                IsMouseButton = true;
                
                DetectedKeyText.Text = $"🖱️ {buttonName}";
                InstructionText.Text = "تم تحديد زر الماوس!";
                OkButton.IsEnabled = true;
                
                e.Handled = true;
            }
        }

        private string GetKeyName(Key key)
        {
            switch (key)
            {
                // أرقام
                case Key.D0: return "0";
                case Key.D1: return "1";
                case Key.D2: return "2";
                case Key.D3: return "3";
                case Key.D4: return "4";
                case Key.D5: return "5";
                case Key.D6: return "6";
                case Key.D7: return "7";
                case Key.D8: return "8";
                case Key.D9: return "9";

                // أرقام NumPad
                case Key.NumPad0: return "NumPad0";
                case Key.NumPad1: return "NumPad1";
                case Key.NumPad2: return "NumPad2";
                case Key.NumPad3: return "NumPad3";
                case Key.NumPad4: return "NumPad4";
                case Key.NumPad5: return "NumPad5";
                case Key.NumPad6: return "NumPad6";
                case Key.NumPad7: return "NumPad7";
                case Key.NumPad8: return "NumPad8";
                case Key.NumPad9: return "NumPad9";

                // حروف
                case Key.A: return "A";
                case Key.B: return "B";
                case Key.C: return "C";
                case Key.D: return "D";
                case Key.E: return "E";
                case Key.F: return "F";
                case Key.G: return "G";
                case Key.H: return "H";
                case Key.I: return "I";
                case Key.J: return "J";
                case Key.K: return "K";
                case Key.L: return "L";
                case Key.M: return "M";
                case Key.N: return "N";
                case Key.O: return "O";
                case Key.P: return "P";
                case Key.Q: return "Q";
                case Key.R: return "R";
                case Key.S: return "S";
                case Key.T: return "T";
                case Key.U: return "U";
                case Key.V: return "V";
                case Key.W: return "W";
                case Key.X: return "X";
                case Key.Y: return "Y";
                case Key.Z: return "Z";

                // مفاتيح الوظائف
                case Key.F1: return "F1";
                case Key.F2: return "F2";
                case Key.F3: return "F3";
                case Key.F4: return "F4";
                case Key.F5: return "F5";
                case Key.F6: return "F6";
                case Key.F7: return "F7";
                case Key.F8: return "F8";
                case Key.F9: return "F9";
                case Key.F10: return "F10";
                case Key.F11: return "F11";
                case Key.F12: return "F12";

                // مفاتيح خاصة
                case Key.Space: return "Space";
                case Key.Enter: return "Enter";
                case Key.Back: return "Backspace";
                case Key.Delete: return "Delete";
                case Key.Insert: return "Insert";
                case Key.Home: return "Home";
                case Key.End: return "End";
                case Key.PageUp: return "PageUp";
                case Key.PageDown: return "PageDown";

                // أسهم
                case Key.Up: return "Up";
                case Key.Down: return "Down";
                case Key.Left: return "Left";
                case Key.Right: return "Right";

                // رموز
                case Key.OemMinus: return "-";
                case Key.OemPlus: return "=";
                case Key.OemOpenBrackets: return "[";
                case Key.OemCloseBrackets: return "]";
                case Key.OemSemicolon: return ";";
                case Key.OemQuotes: return "'";
                case Key.OemComma: return ",";
                case Key.OemPeriod: return ".";
                case Key.OemQuestion: return "/";
                case Key.OemTilde: return "`";
                case Key.OemBackslash: return "\\";

                default:
                    return key.ToString();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedKey = "معطل";
            IsMouseButton = false;
            
            DetectedKeyText.Text = "➖ معطل";
            InstructionText.Text = "تم تعيين الزر كـ معطل!";
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}