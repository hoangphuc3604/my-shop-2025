using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyShop.Views.Dialogs
{
    public sealed partial class EditPromotionDialog : ContentDialog
    {
        private int _promotionId;

        public EditPromotionDialog()
        {
            this.InitializeComponent();
        }

        public void SetPromotion(MyShop.Data.Models.Promotion promotion)
        {
            if (promotion == null) return;
            _promotionId = promotion.PromotionId;
            CodeBox.Text = promotion.Code;
            DescriptionBox.Text = promotion.Description;
            DiscountTypeBox.SelectedIndex = promotion.DiscountType == MyShop.Data.Models.PromotionType.FIXED ? 1 : 0;
            DiscountValueBox.Text = promotion.DiscountValue.ToString();
            AppliesToBox.SelectedIndex = promotion.AppliesTo == MyShop.Data.Models.AppliesTo.PRODUCTS ? 1 : (promotion.AppliesTo == MyShop.Data.Models.AppliesTo.CATEGORIES ? 2 : 0);
            AppliesToIdsBox.Text = promotion.AppliesToIds != null ? string.Join(",", promotion.AppliesToIds) : string.Empty;
            StartDatePicker.Date = promotion.StartAt != null ? new DateTimeOffset(promotion.StartAt.Value) : null;
            EndDatePicker.Date = promotion.EndAt != null ? new DateTimeOffset(promotion.EndAt.Value) : null;
            IsActiveBox.IsChecked = promotion.IsActive;
        }

        public async Task<(bool updated, string? error)> ShowAndSaveAsync(PromotionViewModel vm)
        {
            try
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
                    bool? isActive = IsActiveBox.IsChecked;

                    var updated = await vm.UpdatePromotionAsync(_promotionId, code, description,
                        Enum.TryParse(discountType, out MyShop.Data.Models.PromotionType dt) ? dt : MyShop.Data.Models.PromotionType.PERCENTAGE,
                        discountValue,
                        Enum.TryParse(appliesTo, out MyShop.Data.Models.AppliesTo at) ? at : MyShop.Data.Models.AppliesTo.ALL,
                        appliesToIds.Length == 0 ? null : appliesToIds,
                        startAt, endAt, isActive, null);

                    return (updated != null, updated != null ? null : "Update failed");
                }

                return (false, null);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (errorMessage.StartsWith("GraphQL Error: "))
                {
                    try
                    {
                        var jsonStart = errorMessage.IndexOf('[');
                        var jsonEnd = errorMessage.LastIndexOf(']') + 1;
                        if (jsonStart >= 0 && jsonEnd > jsonStart)
                        {
                            var jsonErrors = errorMessage.Substring(jsonStart, jsonEnd - jsonStart);
                            var errors = System.Text.Json.JsonDocument.Parse(jsonErrors);
                            if (errors.RootElement.GetArrayLength() > 0)
                            {
                                var firstError = errors.RootElement[0];
                                if (firstError.TryGetProperty("message", out var messageElement))
                                {
                                    errorMessage = messageElement.GetString() ?? ex.Message;
                                }
                            }
                        }
                    }
                    catch
                    {
                        errorMessage = ex.Message;
                    }
                }
                return (false, errorMessage);
            }
        }
    }
}


