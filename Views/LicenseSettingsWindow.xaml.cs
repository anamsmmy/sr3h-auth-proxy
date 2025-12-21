using System;
using System.Threading.Tasks;
using System.Windows;
using MacroApp.Services;
using MacroApp.Models;
using SR3H_MACRO.Services;
using System.Diagnostics;

namespace MacroApp.Views
{
    public partial class LicenseSettingsWindow : Window
    {
        private readonly MacroFortActivationService _activationService;
        
        // Store original sensitive data
        private string _originalEmail = "";
        private string _originalSubscriptionCode = "";
        
        // Track visibility state
        private bool _isEmailVisible = false;
        private bool _isSubscriptionCodeVisible = false;

        public LicenseSettingsWindow()
        {
            InitializeComponent();
            _activationService = MacroFortActivationService.Instance;
            
            Loaded += LicenseSettingsWindow_Loaded;
        }

        private async void LicenseSettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadLicenseInfo();
        }

        private async Task LoadLicenseInfo()
        {
            try
            {
                UpdateMessage("جاري تحميل معلومات الترخيص...", false);
                
                var hardwareId = SafeHardwareIdService.GetFreshHardwareId();
                var subscription = await _activationService.GetSubscriptionByHardwareIdAsync(hardwareId);
                
                if (subscription != null && !string.IsNullOrEmpty(subscription.Email))
                {
                    StatusTextBlock.Text = "✅ مفعل";
                    StatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                    
                    // Store original data and hide completely by default
                    _originalEmail = subscription.Email ?? "غير متوفر";
                    _originalSubscriptionCode = subscription.SubscriptionCode ?? "غير متوفر";
                    
                    // Hide data completely initially
                    EmailTextBlock.Text = "••••••••••••••••";
                    SubscriptionCodeTextBlock.Text = "••••••••••••••••";
                    
                    ActivationDateTextBlock.Text = subscription.ActivationDate.ToString("yyyy/MM/dd HH:mm");
                    LastCheckTextBlock.Text = subscription.LastCheckDate?.ToString("yyyy/MM/dd HH:mm") ?? DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm");
                    
                    var daysRemaining = (subscription.ExpiryDate - DateTime.UtcNow).TotalDays;
                    if (daysRemaining > 0)
                    {
                        ExpiryTextBlock.Text = $"{(int)daysRemaining} يوم متبقي";
                        ExpiryTextBlock.Foreground = daysRemaining > 7 ? 
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green) :
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                    }
                    else
                    {
                        ExpiryTextBlock.Text = "انتهت الصلاحية";
                        ExpiryTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    }
                    
                    UpdateMessage("تم تحميل البيانات بنجاح ✅", false);
                }
                else
                {
                    StatusTextBlock.Text = "❌ غير مفعل";
                    StatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    
                    // Reset original data
                    _originalEmail = "غير متوفر";
                    _originalSubscriptionCode = "غير متوفر";
                    
                    EmailTextBlock.Text = "غير متوفر";
                    SubscriptionCodeTextBlock.Text = "غير متوفر";
                    ActivationDateTextBlock.Text = "غير متوفر";
                    LastCheckTextBlock.Text = "غير متوفر";
                    ExpiryTextBlock.Text = "غير متوفر";
                    
                    // Reset visibility states
                    _isEmailVisible = false;
                    _isSubscriptionCodeVisible = false;
                    EmailToggleButton.Content = "👁";
                    SubscriptionCodeToggleButton.Content = "👁";
                    
                    UpdateMessage("لم يتم العثور على اشتراك نشط", true);
                }
            }
            catch (Exception ex)
            {
                UpdateMessage($"خطأ في تحميل معلومات الترخيص: {ex.Message}", true);
            }
        }





        private async void ReactivateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var activationWindow = new MacroFortActivationWindow(MacroFortActivationType.Rebind);
                var result = activationWindow.ShowDialog();
                
                if (result == true)
                {
                    UpdateMessage("تم إعادة التفعيل بنجاح ✅", false);
                    await LoadLicenseInfo();
                }
            }
            catch (Exception ex)
            {
                UpdateMessage($"خطأ في إعادة التفعيل: {ex.Message}", true);
            }
        }





        private void UpdateMessage(string message, bool isError)
        {
            MessageTextBlock.Text = message;
            MessageTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
                isError ? System.Windows.Media.Colors.Red : System.Windows.Media.Colors.Green);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            ReactivateButton.IsEnabled = enabled;
        }



        // Email toggle button click handler
        private void EmailToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isEmailVisible = !_isEmailVisible;
            
            if (_isEmailVisible)
            {
                EmailTextBlock.Text = _originalEmail;
                EmailToggleButton.Content = "🙈"; // Hide icon
                EmailToggleButton.ToolTip = "إخفاء البريد الإلكتروني";
            }
            else
            {
                EmailTextBlock.Text = "••••••••••••••••";
                EmailToggleButton.Content = "👁"; // Show icon
                EmailToggleButton.ToolTip = "إظهار البريد الإلكتروني";
            }
        }

        // Subscription Code toggle button click handler
        private void SubscriptionCodeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isSubscriptionCodeVisible = !_isSubscriptionCodeVisible;
            
            if (_isSubscriptionCodeVisible)
            {
                SubscriptionCodeTextBlock.Text = _originalSubscriptionCode;
                SubscriptionCodeToggleButton.Content = "🙈"; // Hide icon
                SubscriptionCodeToggleButton.ToolTip = "إخفاء كود التفعيل";
            }
            else
            {
                SubscriptionCodeTextBlock.Text = "••••••••••••••••";
                SubscriptionCodeToggleButton.Content = "👁"; // Show icon
                SubscriptionCodeToggleButton.ToolTip = "إظهار كود التفعيل";
            }
        }
    }
}