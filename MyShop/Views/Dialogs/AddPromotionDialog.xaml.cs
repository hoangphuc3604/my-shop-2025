using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Views.Dialogs
{
    public sealed partial class AddPromotionDialog : ContentDialog
    {
        public AddPromotionDialog()
        {
            this.InitializeComponent();
        }

        public async Task<(bool created, string? error)> ShowAndCreateAsync(PromotionViewModel vm)
        {
            var result = await this.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var code = CodeBox.Text?.Trim() ?? string.Empty;
                var description = DescriptionBox.Text ?? string.Empty;
                var discountType = (DiscountTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "PERCENTAGE";
                var discountValue = int.TryParse(DiscountValueBox.Text, out var dv) ? dv : 0;
                var appliesTo = (AppliesToBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "ALL";
                var idsText = AppliesToIdsBox.Text ?? string.Empty;
                var appliesToIds = idsText.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(s => { int.TryParse(s.Trim(), out var k); return k; })
                                          .ToArray();
                DateTime? startAt = StartDatePicker.Date?.DateTime;
                DateTime? endAt = EndDatePicker.Date?.DateTime;

                var created = await vm.CreatePromotionAsync(code, description,
                    Enum.TryParse(discountType, out MyShop.Data.Models.PromotionType dt) ? dt : MyShop.Data.Models.PromotionType.PERCENTAGE,
                    discountValue,
                    Enum.TryParse(appliesTo, out MyShop.Data.Models.AppliesTo at) ? at : MyShop.Data.Models.AppliesTo.ALL,
                    appliesToIds.Length == 0 ? null : appliesToIds,
                    startAt, endAt, null);

                return (created, created ? null : "Creation failed");
            }

            return (false, null);
        }
    }
}


