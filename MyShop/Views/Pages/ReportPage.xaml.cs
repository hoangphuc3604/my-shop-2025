using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services;
using MyShop.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI;

namespace MyShop.Views.Pages
{
    public sealed partial class ReportPage : Page
    {
        private ReportViewModel _viewModel;
        private bool _isInitialized = false;

        public ReportPage()
        {
            this.InitializeComponent();
            
            var reportService = (App.Services.GetService(typeof(IReportService)) as IReportService)!;
            var productService = (App.Services.GetService(typeof(IProductService)) as IProductService)!;
            var sessionService = (App.Services.GetService(typeof(ISessionService)) as ISessionService)!;
            _viewModel = new ReportViewModel(reportService, productService, sessionService);
            
            DataContext = _viewModel;
        }

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SizeChanged += ReportPage_SizeChanged;
            
            _isInitialized = true;
            await LoadProductsAsync();
            await GenerateReportAsync();
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SizeChanged -= ReportPage_SizeChanged;
            _isInitialized = false;
        }

        private void ReportPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.NewSize.Width, e.NewSize.Height);
            
            if (_viewModel?.CurrentReport != null)
            {
                RedrawCharts();
            }
        }

        private void ApplyResponsiveLayout(double width, double height)
        {
            try
            {
                var viewportSize = ResponsiveService.GetCurrentViewportSize(width, height);
                var isCompact = ResponsiveService.IsCompactLayout(width);
                var padding = ResponsiveService.GetOptimalPadding(width);

                Debug.WriteLine($"[REPORT] Responsive: {viewportSize}, Compact: {isCompact}, Width: {width}");

                this.Padding = new Thickness(padding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[REPORT] Error applying responsive layout: {ex.Message}");
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                await _viewModel.LoadProductsAsync();
                
                foreach (var product in _viewModel.AllProducts.OrderBy(p => p.ProductId))
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
                Debug.WriteLine($"[REPORT] Error loading products: {ex.Message}");
                await ShowErrorAsync($"Failed to load products: {ex.Message}");
            }
        }

        private async void OnDateRangeChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (!_isInitialized || _viewModel == null)
                return;

            _viewModel.FromDate = FromDatePicker.Date?.DateTime;
            _viewModel.ToDate = ToDatePicker.Date?.DateTime;
            await GenerateReportAsync();
        }

        private void OnTimePeriodChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || _viewModel == null || _viewModel.CurrentReport == null)
                return;

            var selected = TimePeriodCombo.SelectedItem as ComboBoxItem;
            _viewModel.SelectedTimePeriod = selected?.Tag.ToString() ?? "Day";
            RedrawCharts();
        }

        private void OnProductSelected(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || _viewModel == null)
                return;

            var selected = ProductCombo.SelectedItem as ComboBoxItem;
            if (selected != null)
            {
                _viewModel.SelectedProductId = int.Parse(selected.Tag.ToString());
                if (_viewModel.CurrentReport != null)
                {
                    RedrawCharts();
                }
            }
        }

        private async Task GenerateReportAsync()
        {
            if (_viewModel == null)
                return;

            try
            {
                await _viewModel.GenerateReportAsync();
                RedrawCharts();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[REPORT] ✗ Error generating report: {ex.Message}");
                await ShowErrorAsync($"Failed to generate report: {ex.Message}");
            }
        }

        private void RedrawCharts()
        {
            if (_viewModel?.CurrentReport == null)
                return;

            var timePeriod = _viewModel.SelectedTimePeriod;
            
            // Revenue Chart
            if (timePeriod == "Day" && _viewModel.CurrentReport.DailyRevenues.Count > 0)
            {
                NoDataMessageRevenue.Visibility = Visibility.Collapsed;
                DrawRevenueBarChart(_viewModel.CurrentReport.DailyRevenues, 
                    d => d.Date.ToString("MM-dd"), 
                    d => d.TotalRevenue);
            }
            else if (timePeriod == "Week" && _viewModel.CurrentReport.WeeklyRevenues.Count > 0)
            {
                NoDataMessageRevenue.Visibility = Visibility.Collapsed;
                DrawRevenueBarChart(_viewModel.CurrentReport.WeeklyRevenues, 
                    d => $"{d.WeekStartDate:MM-dd} ~ {d.WeekEndDate.AddDays(-1):MM-dd}", 
                    d => d.TotalRevenue);
            }
            else if (timePeriod == "Month" && _viewModel.CurrentReport.MonthlyRevenues.Count > 0)
            {
                NoDataMessageRevenue.Visibility = Visibility.Collapsed;
                DrawRevenueBarChart(_viewModel.CurrentReport.MonthlyRevenues, 
                    d => d.MonthName.Substring(0, 3), 
                    d => d.TotalRevenue);
            }
            else if (timePeriod == "Year" && _viewModel.CurrentReport.YearlyRevenues.Count > 0)
            {
                NoDataMessageRevenue.Visibility = Visibility.Collapsed;
                DrawRevenueBarChart(_viewModel.CurrentReport.YearlyRevenues, 
                    d => d.Year.ToString(), 
                    d => d.TotalRevenue);
            }
            else
            {
                NoDataMessageRevenue.Visibility = Visibility.Visible;
                RevenueChartCanvas.Children.Clear();
            }

            // Quantity Chart
            if (timePeriod == "Day" && _viewModel.CurrentReport.DailyRevenues.Count > 0)
            {
                NoDataMessageQuantity.Visibility = Visibility.Collapsed;
                DrawQuantityLineChart(_viewModel.CurrentReport.DailyRevenues.Cast<dynamic>().ToList(), 
                    d => ((DailyRevenue)d).Date.ToString("MM-dd"), 
                    d => _viewModel.GetProductQuantity((DailyRevenue)d, _viewModel.SelectedProductId));
            }
            else if (timePeriod == "Week" && _viewModel.CurrentReport.WeeklyRevenues.Count > 0)
            {
                NoDataMessageQuantity.Visibility = Visibility.Collapsed;
                DrawQuantityLineChart(_viewModel.CurrentReport.WeeklyRevenues.Cast<dynamic>().ToList(), 
                    d => $"{((WeeklyRevenue)d).WeekStartDate:MM-dd} ~ {((WeeklyRevenue)d).WeekEndDate.AddDays(-1):MM-dd}", 
                    d => _viewModel.GetProductQuantity((WeeklyRevenue)d, _viewModel.SelectedProductId));
            }
            else if (timePeriod == "Month" && _viewModel.CurrentReport.MonthlyRevenues.Count > 0)
            {
                NoDataMessageQuantity.Visibility = Visibility.Collapsed;
                DrawQuantityLineChart(_viewModel.CurrentReport.MonthlyRevenues.Cast<dynamic>().ToList(), 
                    d => ((MonthlyRevenue)d).MonthName.Substring(0, 3), 
                    d => _viewModel.GetProductQuantity((MonthlyRevenue)d, _viewModel.SelectedProductId));
            }
            else if (timePeriod == "Year" && _viewModel.CurrentReport.YearlyRevenues.Count > 0)
            {
                NoDataMessageQuantity.Visibility = Visibility.Collapsed;
                DrawQuantityLineChart(_viewModel.CurrentReport.YearlyRevenues.Cast<dynamic>().ToList(), 
                    d => ((YearlyRevenue)d).Year.ToString(), 
                    d => _viewModel.GetProductQuantity((YearlyRevenue)d, _viewModel.SelectedProductId));
            }
            else
            {
                NoDataMessageQuantity.Visibility = Visibility.Visible;
                QuantityChartCanvas.Children.Clear();
            }
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
                
                var leftPadding = width < 600 ? 40 : width < 1000 ? 50 : 70;
                var rightPadding = 20;
                var topPadding = 20;
                var bottomPadding = width < 600 ? 35 : width < 1000 ? 40 : 50;
                
                var chartWidth = width - leftPadding - rightPadding;
                var chartHeight = height - topPadding - bottomPadding;

                var maxValue = data.Max(d => valueFunc(d));
                var minValue = 0;

                DrawLine(canvas, leftPadding, height - bottomPadding, width - rightPadding, height - bottomPadding, Colors.White, 1);
                DrawLine(canvas, leftPadding, topPadding, leftPadding, height - bottomPadding, Colors.White, 1);

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

                    DrawRectangle(canvas, x, y, actualBarWidth, barHeight, Colors.DodgerBlue);

                    var labelWidth = label.Length * 6;
                    DrawText(canvas, x + (actualBarWidth / 2) - (labelWidth / 2), height - bottomPadding + 10, label, Colors.White);

                    var valueText = $"{value:#,0}";
                    var valueWidth = valueText.Length * 5;
                    DrawText(canvas, x + (actualBarWidth / 2) - (valueWidth / 2), y - 20, valueText, Colors.White);
                }

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
                Debug.WriteLine($"[REPORT] Error drawing revenue chart: {ex.Message}");
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
                
                var leftPadding = width < 600 ? 40 : width < 1000 ? 50 : 70;
                var rightPadding = 20;
                var topPadding = 20;
                var bottomPadding = width < 600 ? 35 : width < 1000 ? 40 : 50;
                
                var chartWidth = width - leftPadding - rightPadding;
                var chartHeight = height - topPadding - bottomPadding;

                var maxValue = data.Max(d => valueFunc(d));
                var minValue = 0;
                var range = maxValue - minValue;
                if (range == 0) range = maxValue;

                DrawLine(canvas, leftPadding, height - bottomPadding, width - rightPadding, height - bottomPadding, Colors.White, 1);
                DrawLine(canvas, leftPadding, topPadding, leftPadding, height - bottomPadding, Colors.White, 1);

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

                    var label = labelFunc(item);
                    var labelWidth = label.Length * 6;
                    DrawText(canvas, x - (labelWidth / 2), height - bottomPadding + 10, label, Colors.White);
                }

                for (int i = 0; i < pointsCount - 1; i++)
                {
                    var x1 = pointData[i].x;
                    var y1 = pointData[i].y;
                    var x2 = pointData[i + 1].x;
                    var y2 = pointData[i + 1].y;

                    DrawLine(canvas, x1, y1, x2, y2, Colors.LimeGreen, 2);
                }

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
                Debug.WriteLine($"[REPORT] Error drawing quantity chart: {ex.Message}");
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
            var dialog = new ContentDialog 
            { 
                Title = "Error", 
                Content = message, 
                CloseButtonText = "Close", 
                XamlRoot = this.XamlRoot 
            };
            await dialog.ShowAsync();
        }
    }
}
