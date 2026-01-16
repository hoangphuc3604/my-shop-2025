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


