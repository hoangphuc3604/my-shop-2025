using Microsoft.UI.Xaml.Data;
using System;

namespace MyShop.Converters
{
    public class DiscountTypeToUnitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string discountType)
            {
                return discountType switch
                {
                    "PERCENTAGE" => "%",
                    "FIXED" => " ₫",
                    _ => ""
                };
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException("DiscountTypeToUnitConverter is only for one-way binding");
        }
    }
}
