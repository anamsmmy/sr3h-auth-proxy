using System;
using System.Globalization;
using System.Windows.Data;

namespace MacroApp.Converters
{
    public class NumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            
            // تحويل الأرقام إلى الإنجليزية
            var text = value.ToString();
            return ConvertToEnglishNumbers(text);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            
            var text = value.ToString();
            text = ConvertToEnglishNumbers(text);
            
            if (targetType == typeof(int) || targetType == typeof(int?))
            {
                if (int.TryParse(text, out int intResult))
                    return intResult;
                return targetType == typeof(int?) ? (int?)null : 0;
            }
            
            if (targetType == typeof(double) || targetType == typeof(double?))
            {
                if (double.TryParse(text, out double doubleResult))
                    return doubleResult;
                return targetType == typeof(double?) ? (double?)null : 0.0;
            }
            
            return text;
        }

        private string ConvertToEnglishNumbers(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // تحويل الأرقام العربية إلى الإنجليزية
            var arabicNumbers = new char[] { '٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩' };
            var englishNumbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            
            for (int i = 0; i < arabicNumbers.Length; i++)
            {
                text = text.Replace(arabicNumbers[i], englishNumbers[i]);
            }
            
            return text;
        }
    }
}