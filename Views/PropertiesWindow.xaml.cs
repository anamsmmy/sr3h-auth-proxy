using System;
using System.Threading.Tasks;
using System.Windows;
using MacroApp.Models;
using MacroApp.Services;
using SR3H_MACRO.Services;

namespace MacroApp.Views
{
    public partial class PropertiesWindow : Window
    {
        private MacroFortActivationService _activationService;
        private string _emailOriginal;
        private string _codeOriginal;
        private bool _emailVisible = false;
        private bool _codeVisible = false;

        public PropertiesWindow()
        {
            InitializeComponent();
            
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
            
            _activationService = MacroFortActivationService.Instance;
            
            this.Loaded += (s, e) =>
            {
                this.Width = 725;
                this.Height = 688;
            };
            
            LoadPropertiesAsync();
        }

        private async void LoadPropertiesAsync()
        {
            try
            {
                await LoadProperties();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل الخصائص: {ex.Message}");
                CurrentStatusText.Text = "خطأ في التحميل";
                CurrentStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F44336"));
            }
        }

        private async Task LoadProperties()
        {
            try
            {
                var activation = SessionActivationCache.GetCachedActivation();
                
                if (activation == null)
                {
                    var hardwareId = SafeHardwareIdService.GetFreshHardwareId();
                    var subscriptionData = await _activationService.GetSubscriptionByHardwareIdAsync(hardwareId);
                    
                    if (subscriptionData != null)
                    {
                        activation = new ActivationData
                        {
                            Email = subscriptionData.Email,
                            HardwareId = subscriptionData.HardwareId,
                            SubscriptionCode = subscriptionData.SubscriptionCode,
                            SubscriptionType = subscriptionData.SubscriptionType,
                            ActivationDate = subscriptionData.ActivationDate,
                            ExpiryDate = subscriptionData.ExpiryDate,
                            IsActive = subscriptionData.IsActive,
                            EmailVerified = subscriptionData.EmailVerified,
                            LastSync = subscriptionData.LastCheckDate ?? DateTime.UtcNow,
                            DeviceTransferCount = subscriptionData.DeviceTransferCount,
                            LastDeviceTransferDate = subscriptionData.LastDeviceTransferDate ?? DateTime.MinValue,
                            MaxDevices = subscriptionData.MaxDevices ?? 10
                        };
                        SessionActivationCache.SetCachedActivation(activation);
                    }
                }

                if (activation != null)
                {
                    CurrentStatusText.Text = "نشط - Active";
                    CurrentStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));

                    ActivationTypeText.Text = GetActivationTypeDisplay(activation.SubscriptionType);
                    ActivationTypeText.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2196F3"));

                    _emailOriginal = activation.Email;
                    _codeOriginal = activation.SubscriptionCode;
                    EmailText.Text = "***مخفي***";
                    SubscriptionCodeText.Text = string.IsNullOrEmpty(activation.SubscriptionCode) ? "-" : "***مخفي***";

                    LastActivationText.Text = activation.ActivationDate.ToString("yyyy-MM-dd HH:mm:ss");
                    LastActivationText.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666666"));

                    if (activation.ExpiryDate > DateTime.UtcNow)
                    {
                        ExpirationDateText.Text = activation.ExpiryDate.ToString("yyyy-MM-dd");
                        ExpirationDateText.Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F44336"));

                        var remainingDays = (int)(activation.ExpiryDate - DateTime.UtcNow).TotalDays;
                        var remainingHours = (int)(activation.ExpiryDate - DateTime.UtcNow).TotalHours % 24;
                        RemainingTimeText.Text = $"{remainingDays} أيام، {remainingHours:D2}:{DateTime.UtcNow.Minute:D2} ساعات";
                    }
                    else
                    {
                        ExpirationDateText.Text = activation.ExpiryDate.ToString("yyyy-MM-dd");
                        ExpirationDateText.Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F44336"));
                        RemainingTimeText.Text = "انتهت الصلاحية";
                    }

                    var maxDevices = activation.MaxDevices > 0 ? activation.MaxDevices : 10;
                    TransferCountText.Text = $"{activation.DeviceTransferCount}/{maxDevices}";
                    LastTransferText.Text = activation.LastDeviceTransferDate == DateTime.MinValue ? "-" : activation.LastDeviceTransferDate.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    CurrentStatusText.Text = "غير نشط - Inactive";
                    CurrentStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F44336"));

                    ActivationTypeText.Text = "تجريبي";
                    LastActivationText.Text = "لم يتم التفعيل بعد";
                    ExpirationDateText.Text = "-";
                    RemainingTimeText.Text = "-";
                    EmailText.Text = "-";
                    SubscriptionCodeText.Text = "-";
                    TransferCountText.Text = "0/10";
                    LastTransferText.Text = "-";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في جلب بيانات الاشتراك: {ex.Message}");
                CurrentStatusText.Text = "خطأ في التحميل";
                CurrentStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F44336"));
            }
        }

        private string GetActivationTypeDisplay(string subscriptionType)
        {
            return subscriptionType?.ToLower() switch
            {
                "trial" => "تجريبي",
                "month" => "شهري",
                "monthly" => "شهري",
                "6-month" => "6 أشهر",
                "semi" => "نصف سنوي",
                "yearly" => "سنوي",
                "year" => "سنوي",
                "lifetime" => "مدى الحياة",
                _ => subscriptionType ?? "-"
            };
        }

        private void TrialActivationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var activationWindow = new MacroFortActivationWindow(MacroFortActivationType.Trial)
                {
                    Owner = this
                };
                activationWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة التفعيل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CodeActivationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var activationWindow = new MacroFortActivationWindow(MacroFortActivationType.CodeActivation)
                {
                    Owner = this
                };
                activationWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة التفعيل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RebindDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var activationWindow = new MacroFortActivationWindow(MacroFortActivationType.Rebind)
                {
                    Owner = this
                };
                activationWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إعادة الربط: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleEmail_Click(object sender, RoutedEventArgs e)
        {
            _emailVisible = !_emailVisible;
            EmailText.Text = _emailVisible ? _emailOriginal : "***مخفي***";
        }

        private void ToggleCode_Click(object sender, RoutedEventArgs e)
        {
            _codeVisible = !_codeVisible;
            SubscriptionCodeText.Text = _codeVisible ? _codeOriginal : "***مخفي***";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}