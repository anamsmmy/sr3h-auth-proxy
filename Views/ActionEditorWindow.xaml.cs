using System;
using System.Windows;
using System.Windows.Controls;
using MacroApp.Models;

namespace MacroApp.Views
{
    public partial class ActionEditorWindow : Window
    {
        public MacroAction MacroAction { get; private set; }

        public ActionEditorWindow(MacroAction existingAction = null)
        {
            InitializeComponent();
            
            // Set RTL for Arabic text
            FlowDirection = FlowDirection.RightToLeft;
            
            if (existingAction != null)
            {
                LoadExistingAction(existingAction);
            }
            else
            {
                MacroAction = new MacroAction();
                ActionTypeComboBox.SelectedIndex = 0; // Default to MouseLeftDown
            }
            
            UpdatePreview();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            DurationTextBox.TextChanged += (s, e) => UpdatePreview();
            DelayTextBox.TextChanged += (s, e) => UpdatePreview();
            KeyCodeTextBox.TextChanged += (s, e) => UpdatePreview();
        }

        private void LoadExistingAction(MacroAction action)
        {
            MacroAction = action.Clone();
            
            // Set action type
            foreach (ComboBoxItem item in ActionTypeComboBox.Items)
            {
                if (item.Tag.ToString() == action.ActionType.ToString())
                {
                    ActionTypeComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // Set values
            DurationTextBox.Text = action.Duration.ToString();
            DelayTextBox.Text = action.Delay.ToString();
            KeyCodeTextBox.Text = action.KeyCode ?? "";
        }

        private void ActionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActionTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var actionTypeString = selectedItem.Tag.ToString();
                if (Enum.TryParse<ActionType>(actionTypeString, out ActionType actionType))
                {
                    if (MacroAction == null)
                        MacroAction = new MacroAction();
                        
                    MacroAction.ActionType = actionType;
                    
                    // Show/hide relevant panels
                    bool isKeyAction = actionType == ActionType.KeyDown || actionType == ActionType.KeyUp;
                    bool isDelayAction = actionType == ActionType.Delay;
                    
                    KeyCodePanel.Visibility = isKeyAction ? Visibility.Visible : Visibility.Collapsed;
                    DurationPanel.Visibility = isDelayAction ? Visibility.Collapsed : Visibility.Visible;
                    
                    if (isDelayAction)
                    {
                        DelayPanel.Visibility = Visibility.Visible;
                        var delayLabel = DelayPanel.Children[0] as TextBlock;
                        if (delayLabel != null)
                            delayLabel.Text = "مدة التأخير (بالملي ثانية):";
                    }
                    else
                    {
                        DelayPanel.Visibility = Visibility.Visible;
                        var delayLabel = DelayPanel.Children[0] as TextBlock;
                        if (delayLabel != null)
                            delayLabel.Text = "التأخير بعد الإجراء (بالملي ثانية):";
                    }
                    
                    UpdatePreview();
                }
            }
        }

        private void UpdatePreview()
        {
            if (MacroAction == null || PreviewTextBlock == null)
                return;

            try
            {
                // Update MacroAction with current values
                if (int.TryParse(DurationTextBox.Text, out int duration))
                    MacroAction.Duration = Math.Max(0, duration);
                
                if (int.TryParse(DelayTextBox.Text, out int delay))
                    MacroAction.Delay = Math.Max(0, delay);
                
                MacroAction.KeyCode = KeyCodeTextBox.Text;

                // Generate preview text
                string preview = GeneratePreviewText();
                PreviewTextBlock.Text = preview;
            }
            catch (Exception ex)
            {
                PreviewTextBlock.Text = $"خطأ في المعاينة: {ex.Message}";
            }
        }

        private string GeneratePreviewText()
        {
            var preview = $"نوع الإجراء: {GetActionTypeDisplayName(MacroAction.ActionType)}\n";
            
            if (MacroAction.ActionType == ActionType.KeyDown || MacroAction.ActionType == ActionType.KeyUp)
            {
                preview += $"المفتاح: {MacroAction.KeyCode ?? "غير محدد"}\n";
            }
            
            if (MacroAction.ActionType != ActionType.Delay)
            {
                preview += $"مدة الضغط: {MacroAction.Duration} ملي ثانية\n";
            }
            
            if (MacroAction.ActionType == ActionType.Delay)
            {
                preview += $"مدة التأخير: {MacroAction.Delay} ملي ثانية\n";
            }
            else
            {
                preview += $"التأخير بعد الإجراء: {MacroAction.Delay} ملي ثانية\n";
            }
            
            preview += $"\nالوصف: {MacroAction.DisplayName}";
            
            return preview;
        }

        private string GetActionTypeDisplayName(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.MouseLeftDown => "ضغط الماوس الأيسر",
                ActionType.MouseLeftUp => "تحرير الماوس الأيسر",
                ActionType.MouseRightDown => "ضغط الماوس الأيمن",
                ActionType.MouseRightUp => "تحرير الماوس الأيمن",
                ActionType.MouseMiddleDown => "ضغط الماوس الأوسط",
                ActionType.MouseMiddleUp => "تحرير الماوس الأوسط",
                ActionType.KeyDown => "ضغط مفتاح",
                ActionType.KeyUp => "تحرير مفتاح",
                ActionType.Delay => "تأخير زمني",
                _ => actionType.ToString()
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (!ValidateInput())
                    return;

                // Update MacroAction with final values
                if (int.TryParse(DurationTextBox.Text, out int duration))
                    MacroAction.Duration = Math.Max(0, duration);
                
                if (int.TryParse(DelayTextBox.Text, out int delay))
                    MacroAction.Delay = Math.Max(0, delay);
                
                MacroAction.KeyCode = KeyCodeTextBox.Text?.Trim();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الإجراء: {ex.Message}", "خطأ", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            // Validate duration
            if (!int.TryParse(DurationTextBox.Text, out int duration) || duration < 0)
            {
                MessageBox.Show("يرجى إدخال قيمة صحيحة لمدة الضغط (0 أو أكثر)", "خطأ في الإدخال", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                DurationTextBox.Focus();
                return false;
            }

            // Validate delay
            if (!int.TryParse(DelayTextBox.Text, out int delay) || delay < 0)
            {
                MessageBox.Show("يرجى إدخال قيمة صحيحة للتأخير (0 أو أكثر)", "خطأ في الإدخال", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                DelayTextBox.Focus();
                return false;
            }

            // Validate key code for keyboard actions
            if ((MacroAction.ActionType == ActionType.KeyDown || MacroAction.ActionType == ActionType.KeyUp) &&
                string.IsNullOrWhiteSpace(KeyCodeTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال رمز المفتاح للإجراءات المتعلقة بالكيبورد", "خطأ في الإدخال", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                KeyCodeTextBox.Focus();
                return false;
            }

            // Validate delay action
            if (MacroAction.ActionType == ActionType.Delay && delay == 0)
            {
                MessageBox.Show("يجب أن تكون مدة التأخير أكبر من 0", "خطأ في الإدخال", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                DelayTextBox.Focus();
                return false;
            }

            return true;
        }
    }
}