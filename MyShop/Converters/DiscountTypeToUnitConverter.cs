using Microsoft.UI.Xaml.Data;
using MyShop.Data.Models;
using System;

namespace MyShop.Converters
{
    public class DiscountTypeToUnitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return "";

            // Handle both string and Enum values
            var typeString = value.ToString();

            // Check if string matches known enum names or values
            if (typeString == nameof(PromotionType.PERCENTAGE) || typeString == "PERCENTAGE")
            {
                return "%";
            }
            
            if (typeString == nameof(PromotionType.FIXED) || typeString == "FIXED")
            {
                return " ₫";
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException("DiscountTypeToUnitConverter is only for one-way binding");
        }
    }
}
