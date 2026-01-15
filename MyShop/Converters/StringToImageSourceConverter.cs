using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace MyShop.Converters
{
    /// <summary>
    /// Converts a string URL to an ImageSource, with fallback for empty/invalid URLs
    /// </summary>
    public class StringToImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string url && !string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    // Try to create a valid URI
                    if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                    {
                        return new BitmapImage(uri);
                    }
                }
                catch
                {
                    // Fall through to return null
                }
            }
            
            // Return null for empty/invalid URLs - XAML will show nothing
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
