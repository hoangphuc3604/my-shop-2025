using Microsoft.UI.Xaml.Data;
using System;

namespace MyShop.Converters
{
    public class PriceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int intPrice)
            {
                return intPrice.ToString("#,0 ₫");
            }

            if (value is float floatPrice)
            {
                return ((int)floatPrice).ToString("#,0 ₫");
            }

            if (value is double doublePrice)
            {
                return ((int)doublePrice).ToString("#,0 ₫");
            }

            return value?.ToString() ?? "0 ₫";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException("PriceConverter is only for one-way binding");
        }
    }
}