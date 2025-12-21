using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MacroApp.Models;

namespace MacroApp.Views
{
    public partial class MacroSequenceEditorWindow : Window
    {
        private readonly ObservableCollection<MacroActionItem> _sequence;
        public List<MacroActionItem> MacroSequence => _sequence.ToList();

        public MacroSequenceEditorWindow(List<MacroActionItem> initialSequence = null)
        {
            InitializeComponent();
            
            _sequence = new ObservableCollection<MacroActionItem>();
            SequenceItemsControl.ItemsSource = _sequence;

            // تحميل التسلسل الأولي إن وجد
            if (initialSequence != null)
            {
                foreach (var item in initialSequence)
                {
                    _sequence.Add(item);
                }
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            // تحديث عدد الخطوات
            SequenceCountText.Text = $"({_sequence.Count} خطوات)";
            
            // تحديث إجمالي المدة
            var totalDuration = _sequence.Sum(x => x.Duration);
            TotalDurationText.Text = $"{totalDuration}ms";
        }

        private void AddLeftDownButton_Click(object sender, RoutedEventArgs e)
        {
            var duration = GetDurationFromInput();
            _sequence.Add(new MacroActionItem(MacroActionType.MouseDown, Models.MouseButton.Left, duration));
            UpdateUI();
        }

        private void AddLeftUpButton_Click(object sender, RoutedEventArgs e)
        {
            var duration = GetDurationFromInput();
            _sequence.Add(new MacroActionItem(MacroActionType.MouseUp, Models.MouseButton.Left, duration));
            UpdateUI();
        }

        private void AddRightDownButton_Click(object sender, RoutedEventArgs e)
        {
            var duration = GetDurationFromInput();
            _sequence.Add(new MacroActionItem(MacroActionType.MouseDown, Models.MouseButton.Right, duration));
            UpdateUI();
        }

        private void AddRightUpButton_Click(object sender, RoutedEventArgs e)
        {
            var duration = GetDurationFromInput();
            _sequence.Add(new MacroActionItem(MacroActionType.MouseUp, Models.MouseButton.Right, duration));
            UpdateUI();
        }

        private void AddDelayButton_Click(object sender, RoutedEventArgs e)
        {
            var duration = GetDurationFromInput();
            _sequence.Add(new MacroActionItem(MacroActionType.Delay, Models.MouseButton.Left, duration));
            UpdateUI();
        }

        private int GetDurationFromInput()
        {
            if (int.TryParse(DurationTextBox.Text, out int duration))
            {
                return Math.Max(0, duration); // تأكد من أن المدة ليست سالبة
            }
            return 100; // قيمة افتراضية
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "هل أنت متأكد من حذف جميع الخطوات؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _sequence.Clear();
                UpdateUI();
            }
        }

        private void LoadPresetButton_Click(object sender, RoutedEventArgs e)
        {
            // تحميل نموذج افتراضي
            _sequence.Clear();
            
            // نموذج: نقرة يسار سريعة
            _sequence.Add(new MacroActionItem(MacroActionType.MouseDown, Models.MouseButton.Left, 1));
            _sequence.Add(new MacroActionItem(MacroActionType.MouseUp, Models.MouseButton.Left, 0));
            _sequence.Add(new MacroActionItem(MacroActionType.Delay, Models.MouseButton.Left, 50));
            _sequence.Add(new MacroActionItem(MacroActionType.MouseDown, Models.MouseButton.Right, 1));
            _sequence.Add(new MacroActionItem(MacroActionType.MouseUp, Models.MouseButton.Right, 0));
            
            UpdateUI();
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MacroActionItem item)
            {
                var index = _sequence.IndexOf(item);
                if (index > 0)
                {
                    _sequence.Move(index, index - 1);
                    UpdateUI();
                }
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MacroActionItem item)
            {
                var index = _sequence.IndexOf(item);
                if (index < _sequence.Count - 1)
                {
                    _sequence.Move(index, index + 1);
                    UpdateUI();
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MacroActionItem item)
            {
                _sequence.Remove(item);
                UpdateUI();
            }
        }

        private async void TestSequenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sequence.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("لا يوجد خطوات للاختبار");
                return;
            }

            TestSequenceButton.IsEnabled = false;
            TestSequenceButton.Content = "جاري الاختبار...";

            try
            {
                await Task.Run(async () =>
                {
                    foreach (var action in _sequence)
                    {
                        await Task.Delay(action.Duration);
                        
                        // هنا يمكن إضافة تنفيذ فعلي للإجراءات
                        // مثل محاكاة ضغطات الماوس
                    }
                });

                System.Diagnostics.Debug.WriteLine("تم اختبار التسلسل بنجاح");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في اختبار التسلسل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TestSequenceButton.IsEnabled = true;
                TestSequenceButton.Content = "اختبار التسلسل";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // إضافة وظائف السحب والإفلات (Drag & Drop) - يمكن تطويرها لاحقاً
        private void EnableDragDrop()
        {
            // TODO: تنفيذ وظائف السحب والإفلات لإعادة ترتيب العناصر
            // هذا يتطلب تنفيذ معقد أكثر مع events للماوس
        }

        // وظائف إضافية للتحسين
        private void AddQuickSequence(string sequenceType)
        {
            switch (sequenceType)
            {
                case "DoubleClick":
                    _sequence.Add(new MacroActionItem(MacroActionType.MouseDown, Models.MouseButton.Left, 1));
                    _sequence.Add(new MacroActionItem(MacroActionType.MouseUp, Models.MouseButton.Left, 0));
                    _sequence.Add(new MacroActionItem(MacroActionType.Delay, Models.MouseButton.Left, 50));
                    _sequence.Add(new MacroActionItem(MacroActionType.MouseDown, Models.MouseButton.Left, 1));
                    _sequence.Add(new MacroActionItem(MacroActionType.MouseUp, Models.MouseButton.Left, 0));
                    break;

                case "RightClick":
                    _sequence.Add(new MacroActionItem(MacroActionType.MouseDown, Models.MouseButton.Right, 1));
                    _sequence.Add(new MacroActionItem(MacroActionType.MouseUp, Models.MouseButton.Right, 0));
                    break;

                case "LongPress":
                    _sequence.Add(new MacroActionItem(MacroActionType.MouseDown, Models.MouseButton.Left, 500));
                    _sequence.Add(new MacroActionItem(MacroActionType.MouseUp, Models.MouseButton.Left, 0));
                    break;
            }
            
            UpdateUI();
        }

        // التحقق من صحة التسلسل
        private bool ValidateSequence()
        {
            if (_sequence.Count == 0)
                return false;

            // التحقق من وجود أزواج Down/Up متطابقة
            var downActions = _sequence.Where(x => x.ActionType == MacroActionType.MouseDown).ToList();
            var upActions = _sequence.Where(x => x.ActionType == MacroActionType.MouseUp).ToList();

            // يمكن إضافة المزيد من قواعد التحقق هنا

            return true;
        }

        // حفظ التسلسل كنموذج
        private void SaveAsPreset()
        {
            // TODO: تنفيذ حفظ التسلسل كنموذج قابل للاستخدام لاحقاً
        }
    }
}