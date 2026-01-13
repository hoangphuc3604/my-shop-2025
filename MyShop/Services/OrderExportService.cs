using System;
using System.IO;
using System.Threading.Tasks;
using MyShop.Data.Models;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;

namespace MyShop.Services
{
    public class OrderExportService
    {
        static OrderExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Exports an order to PDF or XPS format
        /// </summary>
        public async Task ExportOrderAsync(Order order, XamlRoot xamlRoot, string format)
        {
            if (order == null)
            {
                await ShowError(xamlRoot, "Order cannot be null");
                return;
            }

            try
            {
                Debug.WriteLine($"[EXPORT] Starting export to {format.ToUpper()}...");

                var fileExtension = format.ToLower() == "pdf" ? ".pdf" : ".xps";
                var fileName = $"Order_{order.OrderId}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";
                
                // Save to Documents folder
                var documentsFolder = await StorageFolder.GetFolderFromPathAsync(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                
                var file = await documentsFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                var filePath = file.Path;

                // Generate PDF bytes
                var pdfBytes = Document.Create(container => BuildOrderDocument(container, order))
                    .GeneratePdf();

                // Save to file
                using (var stream = System.IO.File.Create(filePath))
                {
                    await stream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                    await stream.FlushAsync();
                }

                Debug.WriteLine($"[EXPORT] ✓ File saved to: {filePath}");

                // Show success dialog
                await ShowSuccess(xamlRoot, 
                    $"Order exported successfully!\n\n" +
                    $"File: {fileName}\n\n" +
                    $"Location: Documents folder\n\n" +
                    $"Full path: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EXPORT] ✗ Error: {ex.Message}");
                Debug.WriteLine($"[EXPORT] Stack: {ex.StackTrace}");
                await ShowError(xamlRoot, $"Export failed: {ex.Message}");
            }
        }

        private void BuildOrderDocument(IDocumentContainer container, Order order)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                // Header
                page.Header()
                    .PaddingBottom(20)
                    .Text(text =>
                    {
                        text.Line($"Order Receipt #{order.OrderId}")
                            .FontSize(20)
                            .Bold()
                            .FontColor("#2c3e50");
                    });

                // ALL CONTENT IN ONE CALL
                page.Content()
                    .Column(column =>
                    {
                        // Order Info Section
                        column.Item()
                            .Grid(grid =>
                            {
                                grid.Columns(2);

                                // Order ID
                                grid.Item().Text("Order ID").Bold().FontSize(10).FontColor("#7f8c8d");
                                grid.Item().Text(order.OrderId.ToString()).FontSize(11);

                                // Status
                                grid.Item().Text("Status").Bold().FontSize(10).FontColor("#7f8c8d");
                                grid.Item().Text(order.Status).FontSize(11);

                                // Date Created
                                grid.Item().Text("Date Created").Bold().FontSize(10).FontColor("#7f8c8d");
                                grid.Item().Text(order.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(11);

                                // Exported Date
                                grid.Item().Text("Exported").Bold().FontSize(10).FontColor("#7f8c8d");
                                grid.Item().Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(11);
                            });

                        // Divider Line
                        column.Item()
                            .PaddingVertical(20)
                            .LineHorizontal(1, unit: Unit.Point);

                        // Items Table
                        column.Item()
                            .PaddingTop(20)
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                });

                                // Header Row
                                table.Header(header =>
                                {
                                    void HeaderCell(string text)
                                    {
                                        header.Cell()
                                            .Background("#3498db")
                                            .Padding(8)
                                            .Text(text)
                                            .FontColor(QuestPDF.Helpers.Colors.White)
                                            .Bold()
                                            .FontSize(10);
                                    }

                                    HeaderCell("Product Name");
                                    HeaderCell("SKU");
                                    HeaderCell("Qty");
                                    HeaderCell("Unit Price");
                                    HeaderCell("Total");
                                });

                                // Data Rows
                                if (order.OrderItems != null && order.OrderItems.Count > 0)
                                {
                                    foreach (var item in order.OrderItems)
                                    {
                                        table.Cell().Padding(8).Text(item.Product?.Name ?? "N/A").FontSize(10);
                                        table.Cell().Padding(8).Text(item.Product?.Sku ?? "N/A").FontSize(10);
                                        table.Cell().Padding(8).Text(item.Quantity.ToString()).FontSize(10);
                                        table.Cell().Padding(8).Text($"{item.UnitSalePrice:N0} ₫").FontSize(10);
                                        table.Cell().Padding(8).Text($"{item.TotalPrice:N0} ₫").FontSize(10);
                                    }
                                }
                                else
                                {
                                    table.Cell().ColumnSpan(5).Padding(8).Text("No items").FontSize(10);
                                }
                            });

                        // Divider Line
                        column.Item()
                            .PaddingVertical(20)
                            .LineHorizontal(1, unit: Unit.Point);

                        // Total Section
                        column.Item()
                            .PaddingTop(20)
                            .AlignRight()
                            .Width(250)
                            .Grid(grid =>
                            {
                                grid.Columns(2);

                                grid.Item().Text("Final Total:").Bold().FontSize(12);
                                grid.Item().Text($"{order.FinalPrice:N0} ₫").Bold().FontSize(12).FontColor("#27ae60");
                            });
                    });

                // Footer
                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Generated on ").FontSize(9).FontColor("#7f8c8d");
                        text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(9).Bold();
                        text.Span(" | Thank you for your business!").FontSize(9).FontColor("#7f8c8d");
                    });
            });
        }

        private async Task ShowSuccess(XamlRoot xamlRoot, string message)
        {
            if (xamlRoot == null) return;

            try
            {
                var dialog = new ContentDialog
                {
                    Title = "✓ Export Successful",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = xamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EXPORT] Dialog error: {ex.Message}");
            }
        }

        private async Task ShowError(XamlRoot xamlRoot, string message)
        {
            if (xamlRoot == null) return;

            try
            {
                var dialog = new ContentDialog
                {
                    Title = "✗ Export Error",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = xamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EXPORT] Error dialog failed: {ex.Message}");
            }
        }
    }
}