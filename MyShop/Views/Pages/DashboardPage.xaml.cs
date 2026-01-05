using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MyShop.Contracts;
using MyShop.Data.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI;

namespace MyShop.Views.Pages
{
    public sealed partial class DashboardPage : Page
    {
        private readonly IReportService _reportService;
        private readonly IProductService _productService;
        private readonly ISessionService _sessionService;
        private bool _isLoading = false;
        private RevenueReport _currentReport;
        private List<Product> _allProducts;
        private int _selectedProductId = -1;

        public DashboardPage()
        {
            this.InitializeComponent();
            _reportService = (App.Services.GetService(typeof(IReportService)) as IReportService)!;
            _productService = (App.Services.GetService(typeof(IProductService)) as IProductService)!;
            _sessionService = (App.Services.GetService(typeof(ISessionService)) as ISessionService)!;
            _allProducts = new List<Product>();
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadProductsAsync();
            await GenerateReportAsync();
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var token = _sessionService?.GetAuthToken();
                _allProducts = await _productService.GetProductsAsync(1, 1000, null, null, null, null, null, token);
                
                // Populate product combo - sorted by ID
                foreach (var product in _allProducts.OrderBy(p => p.ProductId))
                {
                    ProductCombo.Items.Add(new ComboBoxItem 
                    { 
                        Content = product.Name, 
                        Tag = product.ProductId.ToString() 
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DASHBOARD] Error loading products: {ex.Message}");
            }
        }

        private async void OnDateRangeChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            await GenerateReportAsync();
        }

        private void OnTimePeriodChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentReport != null && !_isLoading)
            {
                RedrawCharts();
            }
        }

        private void OnProductSelected(object sender, SelectionChangedEventArgs e)
        {
            var selected = ProductCombo.SelectedItem as ComboBoxItem;
            if (selected != null)
            {
                _selectedProductId = int.Parse(selected.Tag.ToString());
                if (_currentReport != null && !_isLoading)
                {
                    RedrawCharts();
                }
            }
        }

        private async Task GenerateReportAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                var fromDate = FromDatePicker.Date?.DateTime;
                var toDate = ToDatePicker.Date?.DateTime.AddDays(1);
                var token = _sessionService?.GetAuthToken();

                Debug.WriteLine("[DASHBOARD] Generating report...");
                Debug.WriteLine($"[DASHBOARD] Date Range: {fromDate} to {toDate}");

                _currentReport = await _reportService.GenerateRevenueReportAsync(fromDate, toDate, token);

                TotalRevenueText.Text = $"{_currentReport.TotalRevenue:#,0} ₫";
                TotalOrdersText.Text = _currentReport.TotalOrders.ToString();
                AverageOrderValueText.Text = $"{_currentReport.AverageOrderValue:#,0.00} ₫";

                RedrawCharts();

                Debug.WriteLine("[DASHBOARD] ✓ Report generated successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DASHBOARD] ✗ Error generating report: {ex.Message}");
                await ShowErrorAsync($"Failed to generate report: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void RedrawCharts()
        {
            if (_currentReport == null)
                return;

            var timePeriod = (TimePeriodCombo.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "Day";
            
            // Draw revenue chart
            if (timePeriod == "Day" && _currentReport.DailyRevenues.Count > 0)
            {
                NoDataMessageRevenue.Visibility = Visibility.Collapsed;
                DrawRevenueBarChart(_currentReport.DailyRevenues, 
                    d => d.Date.ToString("MM-dd"), 
                    d => d.TotalRevenue);
            }
            else if (timePeriod == "Week" && _currentReport.WeeklyRevenues.Count > 0)
            {
                NoDataMessageRevenue.Visibility = Visibility.Collapsed;
                DrawRevenueBarChart(_currentReport.WeeklyRevenues, 
                    d => GetWeekDateRange(d.Year, d.WeekNumber), 
                    d => d.TotalRevenue);
            }
            else if (timePeriod == "Month" && _currentReport.MonthlyRevenues.Count > 0)
            {
                NoDataMessageRevenue.Visibility = Visibility.Collapsed;
                DrawRevenueBarChart(_currentReport.MonthlyRevenues, 
                    d => d.MonthName.Substring(0, 3), 
                    d => d.TotalRevenue);
            }
            else if (timePeriod == "Year" && _currentReport.YearlyRevenues.Count > 0)
            {
                NoDataMessageRevenue.Visibility = Visibility.Collapsed;
                DrawRevenueBarChart(_currentReport.YearlyRevenues, 
                    d => d.Year.ToString(), 
                    d => d.TotalRevenue);
            }
            else
            {
                NoDataMessageRevenue.Visibility = Visibility.Visible;
                RevenueChartCanvas.Children.Clear();
            }

            // Draw quantity chart - filter by product selection
            if (timePeriod == "Day" && _currentReport.DailyRevenues.Count > 0)
            {
                NoDataMessageQuantity.Visibility = Visibility.Collapsed;
                DrawQuantityLineChart(_currentReport.DailyRevenues.Cast<dynamic>().ToList(), 
                    d => ((DailyRevenue)d).Date.ToString("MM-dd"), 
                    d => GetProductQuantity(((DailyRevenue)d), _selectedProductId));
            }
            else if (timePeriod == "Week" && _currentReport.WeeklyRevenues.Count > 0)
            {
                NoDataMessageQuantity.Visibility = Visibility.Collapsed;
                DrawQuantityLineChart(_currentReport.WeeklyRevenues.Cast<dynamic>().ToList(), 
                    d => GetWeekDateRange(((WeeklyRevenue)d).Year, ((WeeklyRevenue)d).WeekNumber), 
                    d => GetProductQuantity(((WeeklyRevenue)d), _selectedProductId));
            }
            else if (timePeriod == "Month" && _currentReport.MonthlyRevenues.Count > 0)
            {
                NoDataMessageQuantity.Visibility = Visibility.Collapsed;
                DrawQuantityLineChart(_currentReport.MonthlyRevenues.Cast<dynamic>().ToList(), 
                    d => ((MonthlyRevenue)d).MonthName.Substring(0, 3), 
                    d => GetProductQuantity(((MonthlyRevenue)d), _selectedProductId));
            }
            else if (timePeriod == "Year" && _currentReport.YearlyRevenues.Count > 0)
            {
                NoDataMessageQuantity.Visibility = Visibility.Collapsed;
                DrawQuantityLineChart(_currentReport.YearlyRevenues.Cast<dynamic>().ToList(), 
                    d => ((YearlyRevenue)d).Year.ToString(), 
                    d => GetProductQuantity(((YearlyRevenue)d), _selectedProductId));
            }
            else
            {
                NoDataMessageQuantity.Visibility = Visibility.Visible;
                QuantityChartCanvas.Children.Clear();
            }
        }

        private string GetWeekDateRange(int year, int weekNumber)
        {
            // Find the week using both year and week number, prioritizing year match
            var week = _currentReport?.WeeklyRevenues
                .FirstOrDefault(w => w.Year == year && w.WeekNumber == weekNumber);
            
            // If not found, try just week number (for edge cases)
            week ??= _currentReport?.WeeklyRevenues
                .FirstOrDefault(w => w.WeekNumber == weekNumber);
            
            if (week != null)
            {
                return $"{week.WeekStartDate:MM-dd} ~ {week.WeekEndDate:MM-dd}";
            }
            
            return $"W{weekNumber}";
        }

        private int GetProductQuantity(DailyRevenue daily, int productId)
        {
            if (productId < 0)
            {
                return daily.TotalQuantity;
            }

            var productQuantity = daily.ProductQuantities
                .FirstOrDefault(pq => pq.ProductId == productId);

            return productQuantity?.Quantity ?? 0;
        }

        private int GetProductQuantity(WeeklyRevenue weekly, int productId)
        {
            if (productId < 0)
            {
                return weekly.TotalQuantity;
            }

            var productQuantity = weekly.ProductQuantities
                .FirstOrDefault(pq => pq.ProductId == productId);

            return productQuantity?.Quantity ?? 0;
        }

        private int GetProductQuantity(MonthlyRevenue monthly, int productId)
        {
            if (productId < 0)
            {
                return monthly.TotalQuantity;
            }

            var productQuantity = monthly.ProductQuantities
                .FirstOrDefault(pq => pq.ProductId == productId);

            return productQuantity?.Quantity ?? 0;
        }

        private int GetProductQuantity(YearlyRevenue yearly, int productId)
        {
            if (productId < 0)
            {
                return yearly.TotalQuantity;
            }

            var productQuantity = yearly.ProductQuantities
                .FirstOrDefault(pq => pq.ProductId == productId);

            return productQuantity?.Quantity ?? 0;
        }

        private void DrawRevenueBarChart<T>(List<T> data, Func<T, string> labelFunc, Func<T, int> valueFunc)
        {
            try
            {
                var canvas = RevenueChartCanvas;
                canvas.Children.Clear();

                if (data.Count == 0) return;

                var width = canvas.ActualWidth;
                var height = canvas.ActualHeight;
                var leftPadding = 70;
                var rightPadding = 20;
                var topPadding = 20;
                var bottomPadding = 50;
                var chartWidth = width - leftPadding - rightPadding;
                var chartHeight = height - topPadding - bottomPadding;

                var maxValue = data.Max(d => valueFunc(d));
                var minValue = 0;

                // Draw axes
                DrawLine(canvas, leftPadding, height - bottomPadding, width - rightPadding, height - bottomPadding, Colors.White, 1);
                DrawLine(canvas, leftPadding, topPadding, leftPadding, height - bottomPadding, Colors.White, 1);

                // Draw bars
                var barCount = data.Count;
                var barWidth = chartWidth / barCount;
                var spacing = barWidth * 0.15;
                var actualBarWidth = barWidth - spacing;

                for (int i = 0; i < barCount; i++)
                {
                    var item = data[i];
                    var value = valueFunc(item);
                    var label = labelFunc(item);
                    
                    var x = leftPadding + (i * barWidth) + (spacing / 2);
                    var barHeight = (value - minValue) / (double)maxValue * chartHeight;
                    var y = height - bottomPadding - barHeight;

                    // Draw bar
                    DrawRectangle(canvas, x, y, actualBarWidth, barHeight, Colors.DodgerBlue);

                    // Draw label centered below bar
                    var labelWidth = label.Length * 6;
                    DrawText(canvas, x + (actualBarWidth / 2) - (labelWidth / 2), height - bottomPadding + 10, label, Colors.White);

                    // Draw value centered above bar
                    var valueText = $"{value:#,0}";
                    var valueWidth = valueText.Length * 5;
                    DrawText(canvas, x + (actualBarWidth / 2) - (valueWidth / 2), y - 20, valueText, Colors.White);
                }

                // Draw Y-axis labels (outside chart)
                for (int i = 0; i <= 5; i++)
                {
                    var value = (maxValue / 5.0) * i;
                    var y = height - bottomPadding - (i / 5.0 * chartHeight);
                    var text = $"{(int)value:#,0}";
                    DrawText(canvas, 5, y - 8, text, Colors.Gray);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DASHBOARD] Error drawing revenue chart: {ex.Message}");
            }
        }

        private void DrawQuantityLineChart(List<dynamic> data, Func<dynamic, string> labelFunc, Func<dynamic, int> valueFunc)
        {
            try
            {
                var canvas = QuantityChartCanvas;
                canvas.Children.Clear();

                if (data.Count == 0) return;

                var width = canvas.ActualWidth;
                var height = canvas.ActualHeight;
                var leftPadding = 70;
                var rightPadding = 20;
                var topPadding = 20;
                var bottomPadding = 50;
                var chartWidth = width - leftPadding - rightPadding;
                var chartHeight = height - topPadding - bottomPadding;

                var maxValue = data.Max(d => valueFunc(d));
                var minValue = 0;
                var range = maxValue - minValue;
                if (range == 0) range = maxValue;

                // Draw axes
                DrawLine(canvas, leftPadding, height - bottomPadding, width - rightPadding, height - bottomPadding, Colors.White, 1);
                DrawLine(canvas, leftPadding, topPadding, leftPadding, height - bottomPadding, Colors.White, 1);

                // Draw points
                var pointsCount = data.Count;
                var xSpacing = pointsCount > 1 ? chartWidth / (pointsCount - 1) : chartWidth;
                var pointData = new List<(double x, double y, dynamic item)>();

                for (int i = 0; i < pointsCount; i++)
                {
                    var item = data[i];
                    var value = valueFunc(item);
                    var x = leftPadding + (i * xSpacing);
                    var y = height - bottomPadding - ((value - minValue) / (double)range * chartHeight);

                    pointData.Add((x, y, item));

                    // Draw point with hover effect
                    var circle = new Ellipse 
                    { 
                        Width = 8, 
                        Height = 8, 
                        Fill = new SolidColorBrush(Colors.LimeGreen),
                        Tag = new { value, label = labelFunc(item) }
                    };
                    Canvas.SetLeft(circle, x - 4);
                    Canvas.SetTop(circle, y - 4);
                    
                    circle.PointerEntered += (s, e) =>
                    {
                        var tag = (dynamic)((Ellipse)s).Tag;
                        ShowTooltip(canvas, x, y - 20, $"{tag.value}");
                    };
                    circle.PointerExited += (s, e) => RemoveTooltip(canvas);
                    
                    canvas.Children.Add(circle);

                    // Draw label centered below point
                    var label = labelFunc(item);
                    var labelWidth = label.Length * 6;
                    DrawText(canvas, x - (labelWidth / 2), height - bottomPadding + 10, label, Colors.White);
                }

                // Connect points with lines
                for (int i = 0; i < pointsCount - 1; i++)
                {
                    var x1 = pointData[i].x;
                    var y1 = pointData[i].y;
                    var x2 = pointData[i + 1].x;
                    var y2 = pointData[i + 1].y;

                    DrawLine(canvas, x1, y1, x2, y2, Colors.LimeGreen, 2);
                }

                // Draw Y-axis labels starting from 0
                for (int i = 0; i <= 5; i++)
                {
                    var value = minValue + (range / 5.0 * i);
                    var y = height - bottomPadding - (i / 5.0 * chartHeight);
                    var text = $"{(int)value}";
                    DrawText(canvas, 5, y - 8, text, Colors.Gray);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DASHBOARD] Error drawing quantity chart: {ex.Message}");
            }
        }

        private void ShowTooltip(Canvas canvas, double x, double y, string value)
        {
            RemoveTooltip(canvas);
            
            var tooltip = new TextBlock 
            { 
                Text = value, 
                Name = "Tooltip",
                Foreground = new SolidColorBrush(Colors.Yellow),
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };
            Canvas.SetLeft(tooltip, x);
            Canvas.SetTop(tooltip, y);
            canvas.Children.Add(tooltip);
        }

        private void RemoveTooltip(Canvas canvas)
        {
            var tooltip = canvas.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "Tooltip");
            if (tooltip != null)
            {
                canvas.Children.Remove(tooltip);
            }
        }

        private void DrawLine(Canvas canvas, double x1, double y1, double x2, double y2, Color color, double thickness)
        {
            var line = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = new SolidColorBrush(color), StrokeThickness = thickness };
            canvas.Children.Add(line);
        }

        private void DrawCircle(Canvas canvas, double x, double y, double radius, Color color)
        {
            var circle = new Ellipse { Width = radius * 2, Height = radius * 2, Fill = new SolidColorBrush(color) };
            Canvas.SetLeft(circle, x - radius);
            Canvas.SetTop(circle, y - radius);
            canvas.Children.Add(circle);
        }

        private void DrawRectangle(Canvas canvas, double x, double y, double width, double height, Color color)
        {
            var rect = new Rectangle { Width = width, Height = height, Fill = new SolidColorBrush(color) };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            canvas.Children.Add(rect);
        }

        private void DrawText(Canvas canvas, double x, double y, string text, Color color)
        {
            var textBlock = new TextBlock { Text = text, Foreground = new SolidColorBrush(color), FontSize = 11 };
            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            canvas.Children.Add(textBlock);
        }

        private async Task ShowErrorAsync(string message)
        {
            var dialog = new ContentDialog { Title = "Error", Content = message, CloseButtonText = "Close", XamlRoot = this.XamlRoot };
            await dialog.ShowAsync();
        }

        private static DateTime AddWeeks(DateTime date, int weeks)
        {
            return date.AddDays(weeks * 7);
        }
    }
}
