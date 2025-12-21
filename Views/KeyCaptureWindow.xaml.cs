using System.Windows;
using System.Windows.Input;

namespace MacroApp.Views
{
    public partial class KeyCaptureWindow : Window
    {
        public string CapturedKey { get; private set; } = "";

        public KeyCaptureWindow()
        {
            InitializeComponent();
            
            // تعيين الثقافة للأرقام الإنجليزية
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
            
            Loaded += (s, e) => Focus(); // التأكد من أن النافذة تحصل على التركيز
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // تجاهل مفاتيح النظام
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                return;
            }

            if (e.Key == Key.Enter)
            {
                if (!string.IsNullOrEmpty(CapturedKey))
                {
                    DialogResult = true;
                }
                return;
            }

            // التقاط المفتاح
            string keyName = GetKeyName(e.Key);
            if (!string.IsNullOrEmpty(keyName))
            {
                CapturedKey = keyName;
                KeyDisplayText.Text = keyName;
                
                // تفعيل زر الموافق
                OkButton.IsEnabled = true;
                
                // تحديث النص التوضيحي
                // تحديث النص فقط
                KeyDisplayText.Text = keyName;
            }

            e.Handled = true;
        }

        private string GetKeyName(Key key)
        {
            // تحويل المفاتيح إلى أسماء مفهومة
            return key switch
            {
                Key.A => "A",
                Key.B => "B",
                Key.C => "C",
                Key.D => "D",
                Key.E => "E",
                Key.F => "F",
                Key.G => "G",
                Key.H => "H",
                Key.I => "I",
                Key.J => "J",
                Key.K => "K",
                Key.L => "L",
                Key.M => "M",
                Key.N => "N",
                Key.O => "O",
                Key.P => "P",
                Key.Q => "Q",
                Key.R => "R",
                Key.S => "S",
                Key.T => "T",
                Key.U => "U",
                Key.V => "V",
                Key.W => "W",
                Key.X => "X",
                Key.Y => "Y",
                Key.Z => "Z",
                Key.D0 => "0",
                Key.D1 => "1",
                Key.D2 => "2",
                Key.D3 => "3",
                Key.D4 => "4",
                Key.D5 => "5",
                Key.D6 => "6",
                Key.D7 => "7",
                Key.D8 => "8",
                Key.D9 => "9",
                Key.Space => "SPACE",
                Key.LeftShift => "LSHIFT",
                Key.RightShift => "RSHIFT",
                Key.LeftCtrl => "LCTRL",
                Key.RightCtrl => "RCTRL",
                Key.LeftAlt => "LALT",
                Key.RightAlt => "RALT",
                Key.Tab => "TAB",
                Key.CapsLock => "CAPS",
                Key.F1 => "F1",
                Key.F2 => "F2",
                Key.F3 => "F3",
                Key.F4 => "F4",
                Key.F5 => "F5",
                Key.F6 => "F6",
                Key.F7 => "F7",
                Key.F8 => "F8",
                Key.F9 => "F9",
                Key.F10 => "F10",
                Key.F11 => "F11",
                Key.F12 => "F12",
                Key.NumPad0 => "NUM0",
                Key.NumPad1 => "NUM1",
                Key.NumPad2 => "NUM2",
                Key.NumPad3 => "NUM3",
                Key.NumPad4 => "NUM4",
                Key.NumPad5 => "NUM5",
                Key.NumPad6 => "NUM6",
                Key.NumPad7 => "NUM7",
                Key.NumPad8 => "NUM8",
                Key.NumPad9 => "NUM9",
                Key.Up => "UP",
                Key.Down => "DOWN",
                Key.Left => "LEFT",
                Key.Right => "RIGHT",
                Key.Home => "HOME",
                Key.End => "END",
                Key.PageUp => "PGUP",
                Key.PageDown => "PGDN",
                Key.Insert => "INS",
                Key.Delete => "DEL",
                _ => key.ToString().ToUpper()
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CapturedKey))
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("يرجى اختيار مفتاح أولاً\nPlease select a key first", 
                               "تحذير - Warning", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Warning);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            CapturedKey = "";
            KeyDisplayText.Text = "...";
            OkButton.IsEnabled = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}