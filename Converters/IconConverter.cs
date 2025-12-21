using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MacroApp.Converters
{
    /// <summary>
    /// محول لتحويل اسم الأيقونة إلى DrawingImage
    /// </summary>
    public class IconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string iconName && !string.IsNullOrEmpty(iconName))
            {
                try
                {
                    // البحث عن الأيقونة في الموارد باستخدام TryFindResource
                    var resource = Application.Current.TryFindResource(iconName);
                    if (resource is DrawingImage drawingImage)
                    {
                        return drawingImage;
                    }
                }
                catch (Exception ex)
                {
                    // تسجيل الخطأ للتشخيص
                    System.Diagnostics.Debug.WriteLine($"خطأ في تحميل الأيقونة {iconName}: {ex.Message}");
                }
            }

            // إرجاع أيقونة افتراضية أو null
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}