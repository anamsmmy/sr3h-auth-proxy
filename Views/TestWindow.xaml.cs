using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MacroApp.Views
{
    public partial class TestWindow : Window
    {
        private int _keyPressCount = 0;
        private Stopwatch _responseTimer = new Stopwatch();
        private List<string> _keyLog = new List<string>();

        public TestWindow()
        {
            InitializeComponent();
            
            // تعيين الثقافة للأرقام الإنجليزية
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
            
            // تفعيل التقاط الأحداث على مستوى النافذة
            this.KeyDown += TestWindow_KeyDown;
            this.KeyUp += TestWindow_KeyUp;
            this.MouseDown += TestWindow_MouseDown;
            this.MouseUp += TestWindow_MouseUp;
            this.PreviewKeyDown += TestWindow_PreviewKeyDown;
            this.PreviewKeyUp += TestWindow_PreviewKeyUp;
            
            // ضمان التركيز والتقاط الأحداث
            this.Focusable = true;
            this.Focus();
            this.Loaded += (s, e) => this.Focus();
            
            _responseTimer.Start();
            
            // إضافة رسالة ترحيب
            KeyLogListBox.Items.Add("=== بدء اختبار الأزرار والمفاتيح ===");
            KeyLogListBox.Items.Add("اضغط على أي مفتاح أو زر ماوس للاختبار");
        }

        private void TestWindow_KeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyEvent(e.Key.ToString(), "ضغط مفتاح", "Keyboard");
        }

        private void TestWindow_KeyUp(object sender, KeyEventArgs e)
        {
            HandleKeyEvent(e.Key.ToString(), "إفلات مفتاح", "Keyboard");
        }

        private void TestWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyEvent(e.Key.ToString(), "ضغط مفتاح (Preview)", "Keyboard");
            e.Handled = false; // السماح للحدث بالمرور
        }

        private void TestWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            HandleKeyEvent(e.Key.ToString(), "إفلات مفتاح (Preview)", "Keyboard");
            e.Handled = false; // السماح للحدث بالمرور
        }

        private void TestWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string buttonName = GetMouseButtonName(e.ChangedButton);
            HandleKeyEvent(buttonName, "ضغط ماوس", "Mouse");
        }

        private void TestWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            string buttonName = GetMouseButtonName(e.ChangedButton);
            HandleKeyEvent(buttonName, "إفلات ماوس", "Mouse");
        }

        private string GetMouseButtonName(MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => "الزر الأيسر",
                MouseButton.Right => "الزر الأيمن",
                MouseButton.Middle => "الزر الأوسط",
                MouseButton.XButton1 => "الزر الإضافي 1",
                MouseButton.XButton2 => "الزر الإضافي 2",
                _ => "زر غير معروف"
            };
        }

        private void HandleKeyEvent(string keyName, string eventType, string deviceType)
        {
            _keyPressCount++;
            var responseTime = _responseTimer.ElapsedMilliseconds;
            _responseTimer.Restart();

            // تحديث العرض
            Dispatcher.Invoke(() =>
            {
                KeyDisplayText.Text = keyName;
                KeyTypeText.Text = eventType;
                KeyTimeText.Text = DateTime.Now.ToString("HH:mm:ss.fff");
                KeyCountText.Text = _keyPressCount.ToString();

                // حساب سرعة الاستجابة (كلما قل الوقت، زادت السرعة)
                var speedPercentage = Math.Max(0, 100 - (responseTime / 10.0));
                ResponseSpeedBar.Value = Math.Min(100, speedPercentage);
                ResponseSpeedText.Text = $"{responseTime} ms";

                // إضافة إلى السجل
                var logEntry = $"[{DateTime.Now:HH:mm:ss}] {eventType}: {keyName} ({deviceType}) - {responseTime}ms";
                _keyLog.Add(logEntry);
                KeyLogListBox.Items.Add(logEntry);

                // التمرير إلى آخر عنصر
                if (KeyLogListBox.Items.Count > 0)
                {
                    KeyLogListBox.ScrollIntoView(KeyLogListBox.Items[KeyLogListBox.Items.Count - 1]);
                }

                // الحد الأقصى للسجل
                if (_keyLog.Count > 100)
                {
                    _keyLog.RemoveAt(0);
                    KeyLogListBox.Items.RemoveAt(0);
                }
            });
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            _keyLog.Clear();
            KeyLogListBox.Items.Clear();
            _keyPressCount = 0;
            KeyCountText.Text = "0";
            KeyDisplayText.Text = "اضغط على أي مفتاح...";
            KeyTypeText.Text = "-";
            KeyTimeText.Text = "-";
            ResponseSpeedBar.Value = 0;
            ResponseSpeedText.Text = "0 ms";
            _responseTimer.Restart();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.Focus();
        }
    }
}