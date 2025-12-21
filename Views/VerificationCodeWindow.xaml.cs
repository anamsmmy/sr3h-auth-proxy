using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MacroApp.Services;

namespace MacroApp.Views
{
    public partial class VerificationCodeWindow : Window
    {
        private readonly VerificationCodeService _verificationCodeService;
        private readonly string _email;
        private readonly string _orderId;

        public bool IsVerified { get; private set; } = false;

        public VerificationCodeWindow(string email, string orderId)
        {
            InitializeComponent();
            _verificationCodeService = new VerificationCodeService();
            _email = email;
            _orderId = orderId;

            EmailDisplayTextBlock.Text = $"تم إرسال كود التحقق إلى:\n{email}";
            VerificationCodeTextBox.Focus();
        }

        private async void VerifyButton_Click(object sender, RoutedEventArgs e)
        {
            var code = VerificationCodeTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                UpdateStatus("يرجى إدخال كود التحقق", true);
                VerificationCodeTextBox.Focus();
                return;
            }

            if (code.Length != 6)
            {
                UpdateStatus("كود التحقق يجب أن يكون مكون من 6 أرقام", true);
                VerificationCodeTextBox.Focus();
                return;
            }

            await VerifyCode(code);
        }

        private async Task VerifyCode(string code)
        {
            try
            {
                SetUIState(false);
                ShowProgress(true);
                UpdateStatus("جاري التحقق من الكود...", false);

                var result = await _verificationCodeService.ValidateCodeAsync(_email, _orderId, code);

                if (result.IsSuccess)
                {
                    UpdateStatus("✅ تم التحقق بنجاح!", false);
                    IsVerified = true;

                    await Task.Delay(1000);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    UpdateStatus(result.ErrorMessage, true);
                    VerificationCodeTextBox.Clear();
                    VerificationCodeTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Verification error: {ex}");
                UpdateStatus("حدث خطأ أثناء التحقق. يرجى المحاولة مرة أخرى", true);
            }
            finally
            {
                SetUIState(true);
                ShowProgress(false);
            }
        }

        private async void ResendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetUIState(false);
                ShowProgress(true);
                UpdateStatus("جاري إعادة إرسال الكود...", false);

                var result = await _verificationCodeService.GenerateAndSendCodeAsync(_email, _orderId);

                if (result.IsSuccess)
                {
                    UpdateStatus("✅ تم إعادة إرسال الكود بنجاح", false);
                    VerificationCodeTextBox.Clear();
                    VerificationCodeTextBox.Focus();
                }
                else
                {
                    UpdateStatus(result.ErrorMessage, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Resend error: {ex}");
                UpdateStatus("حدث خطأ أثناء إعادة الإرسال. يرجى المحاولة مرة أخرى", true);
            }
            finally
            {
                SetUIState(true);
                ShowProgress(false);
            }
        }

        private void VerificationCodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // السماح بالأرقام فقط
            e.Handled = !IsTextNumeric(e.Text);
        }

        private bool IsTextNumeric(string text)
        {
            return Regex.IsMatch(text, "^[0-9]+$");
        }

        private void UpdateStatus(string message, bool isError)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
                isError ? System.Windows.Media.Colors.Red : System.Windows.Media.Colors.Green);
        }

        private void SetUIState(bool enabled)
        {
            VerificationCodeTextBox.IsEnabled = enabled;
            VerifyButton.IsEnabled = enabled;
            ResendButton.IsEnabled = enabled;
        }

        private void ShowProgress(bool show)
        {
            ProgressBar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ProgressBar.IsIndeterminate = show;
        }
    }
}