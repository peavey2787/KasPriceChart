using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using System;
using System.Linq;
using System.Text;
using System.Drawing.Drawing2D;
using System.Threading;

namespace KasPriceChart
{
    public class GraphPlotter
    {
        #region Variables
        private Chart _priceChart;
        private Chart _hashrateChart;
        private Color _priceBackgroundColor;
        private Color _priceLineColor;
        private Color _priceTextColor;
        private Color _hashrateBackgroundColor;
        private Color _hashrateLineColor;
        private Color _hashrateTextColor;

        private bool _isPanning;
        private Point _panStartPoint;
        private double _xPanStartMin;
        private double _xPanStartMax;
        private double _yPanStartMin;
        private double _yPanStartMax;

        private int _dataPointSize;
        #endregion


        #region Initialization
        public GraphPlotter(Chart priceChart, Chart hashrateChart)
        {
            // Defaults
            _dataPointSize = 5;

            // Set charts
            _priceChart = priceChart;
            _hashrateChart = hashrateChart;

            // Initialize colors using hex values
            _priceBackgroundColor = ColorTranslator.FromHtml("#231F20");
            _priceLineColor = ColorTranslator.FromHtml("#70C7BA");
            _priceTextColor = ColorTranslator.FromHtml("#70C7BA");
            _hashrateBackgroundColor = ColorTranslator.FromHtml("#231F20");
            _hashrateLineColor = ColorTranslator.FromHtml("#70C7BA");
            _hashrateTextColor = ColorTranslator.FromHtml("#70C7BA");

            // Initialize charts with colors and hide legends
            InitializeChart(_priceChart, _priceBackgroundColor, _priceLineColor, _priceTextColor, "Price", false);
            InitializeChart(_hashrateChart, _hashrateBackgroundColor, _hashrateLineColor, _hashrateTextColor, "Hashrate", false);
        }

        public void InitializeChart(Chart chart, Color backgroundColor, Color lineColor, Color textColor, string title, bool showLegend)
        {
            // Set the background color of the chart area and chart itself
            chart.ChartAreas[0].BackColor = backgroundColor;
            chart.BackColor = backgroundColor;

            // Remove chart border
            chart.BorderlineColor = backgroundColor;
            chart.BorderlineDashStyle = ChartDashStyle.NotSet;
            chart.BorderlineWidth = 0;

            // Set axis and title text colors
            chart.ChartAreas[0].AxisX.LabelStyle.ForeColor = textColor;
            chart.ChartAreas[0].AxisY.LabelStyle.ForeColor = textColor;
            chart.Titles.Clear();
            chart.Titles.Add(new Title
            {
                Text = title,
                ForeColor = textColor,
                Font = new Font("Arial", 12, FontStyle.Bold)
            });

            // Enable zooming and scrolling without showing scroll bars
            chart.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            chart.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
            chart.ChartAreas[0].AxisX.ScrollBar.Enabled = false; // Hide X-axis scroll bar
            chart.ChartAreas[0].AxisY.ScrollBar.Enabled = false; // Hide Y-axis scroll bar

            // Enable panning
            chart.ChartAreas[0].CursorX.IsUserEnabled = false;
            chart.ChartAreas[0].CursorY.IsUserEnabled = false;

            // Set up an example series with the line color
            var exampleSeries = CreateSeries("Example", lineColor);

            chart.Series.Add(exampleSeries); // This can be an initial setup, actual data will overwrite

            // Enable mouse wheel zooming
            chart.MouseWheel += Chart_MouseWheel;

            // Enable panning with mouse
            chart.MouseDown += Chart_MouseDown;
            chart.MouseMove += Chart_MouseMove;
            chart.MouseUp += Chart_MouseUp;

            // Change cursor on mouse enter/leave
            chart.MouseEnter += Chart_MouseEnter;
            chart.MouseLeave += Chart_MouseLeave;

            // Hide legends
            chart.Legends.Clear();
            if (showLegend)
            {
                chart.Legends.Add(new Legend { Enabled = showLegend });
            }

            // Format x-axis labels to show date and time on separate lines
            chart.ChartAreas[0].AxisX.LabelStyle.Format = "dd-MM-yyyy\nhh:mm:ss tt";
            chart.ChartAreas[0].AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;

            // Format y-axis labels to show values with 4 decimal places
            chart.ChartAreas[0].AxisY.LabelStyle.Format = "F4";

            TurnPanningOn(chart);
        }
        #endregion


        #region Core Logic
        private void UpdateChartCore(Chart chart, List<DataPoint> dataPoints, bool logOrLinear, string seriesName, Color seriesColor)
        {
            chart.Series.Clear();

            var series = CreateSeries(seriesName, seriesColor);

            foreach (var point in dataPoints)
            {
                if (point.Price > 0 && seriesName == "Price")
                {
                    series.Points.AddXY(point.Timestamp, point.Price);
                }
                else if (point.Hashrate > 0 && seriesName == "Hashrate")
                {
                    series.Points.AddXY(point.Timestamp, point.Hashrate);
                }
            }

            chart.Series.Add(series);

            // Adjust y-axis for hashrate chart to show full values if it is the hashrate chart
            if (seriesName == "Hashrate")
            {
                chart.ChartAreas[0].AxisY.LabelStyle.Format = "N0"; // Show full values in normal format
                chart.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                chart.ChartAreas[0].AxisY.Interval = 0; // Reset interval to allow automatic adjustment
            }

            // Calculate yMin and yMax from the price data points if it is the price chart
            if (seriesName == "Price")
            {
                double yMin = dataPoints.Where(dp => dp.Price > 0).Min(dp => dp.Price);
                double yMax = dataPoints.Max(dp => dp.Price);

                // Ensure the yMin is not less than 0
                yMin = Math.Max(yMin, 0.01);

                // Set the y-axis range to prevent excessive zooming out
                chart.ChartAreas[0].AxisY.Minimum = yMin;
                chart.ChartAreas[0].AxisY.Maximum = yMax;
            }

            if (logOrLinear)
            {
                // Set y-axis to logarithmic
                chart.ChartAreas[0].AxisY.IsLogarithmic = true;
                chart.ChartAreas[0].AxisY.LogarithmBase = 10;
            }
            else
            {
                // Ensure the y-axis is not set to logarithmic
                chart.ChartAreas[0].AxisY.IsLogarithmic = false;
            }

            // Reset zoom to fully zoomed out
            ResetZoom(chart);

            chart.Invalidate();
        }

        private void UpdateChartWithPowerLawCore(Chart chart, List<DataPoint> dataPoints, RichTextBox richTextBoxLog, Label lblRValue, bool logOrLinear, Color lineColor, int extendLines)
        {
            chart.Series.Clear();
            richTextBoxLog.Clear();

            if (dataPoints == null || dataPoints.Count == 0)
            {
                return;
            }

            var seriesData = CreateSeries("Data", lineColor);

            foreach (var point in dataPoints)
            {
                if (chart.Titles[0].Text == "Hashrate")
                {
                    if (point.Hashrate > 0)
                    {
                        seriesData.Points.AddXY(point.Timestamp, point.Hashrate);
                    }
                }
                else if (chart.Titles[0].Text == "Price")
                {
                    if (point.Price > 0)
                    {
                        seriesData.Points.AddXY(point.Timestamp, point.Price);
                    }
                }
                    
            }

            chart.Series.Add(seriesData);

            var genesisDate = new DateTime(2021, 11, 7);

            // Perform log-log regression once to get the constants
            var (exponent, fairPriceConstant, rSquared, sumX, sumY, sumXY, sumX2, sumY2) = DataManager.GetRegressionConstants(dataPoints, genesisDate, richTextBoxLog);

            // Extract significant digits and exponent part
            string significantDigits = "N/A";
            string formattedFairPriceConstant = "N/A";
            try
            {
                string fairPriceString = fairPriceConstant.ToString("0.000E+0");
                int exponentIndex = fairPriceString.IndexOf('E');
                if (exponentIndex > -1)
                {
                    significantDigits = fairPriceString.Substring(0, exponentIndex);
                    string exponentPart = fairPriceString.Substring(exponentIndex);
                    exponentPart = exponentPart.Replace("E-0", "E-").Replace("E+0", "E+");
                    formattedFairPriceConstant = $"{significantDigits}...{exponentPart}";
                }
            }
            catch (Exception ex)
            {
                richTextBoxLog.AppendText($"Error extracting significant digits and exponent: {ex.Message}\n");
            }

            lblRValue.Text = string.Format(
                "R² = {0:F4}{1}Exponent: {2:F4}{1}Fair Price Constant: {3}",
                rSquared,
                Environment.NewLine,
                exponent,
                formattedFairPriceConstant
            );

            richTextBoxLog.AppendText($"Regression Constants:\nExponent: {exponent}\nFair Price Constant: {fairPriceConstant}\n\n");

            DateTime latestDate = dataPoints.Max(dp => dp.Timestamp);
            DateTime endDate = latestDate.AddDays(extendLines);

            var (dates, prices, supportPrices, resistancePrices, fairPrices, logDeltaGB, logPrices) = DataManager.PrepareChartData(genesisDate, dataPoints, exponent, fairPriceConstant * Math.Pow(10, -0.25), fairPriceConstant * Math.Pow(10, 0.25), fairPriceConstant, endDate);

            double yMin = Math.Min(supportPrices.Min(), Math.Min(resistancePrices.Min(), fairPrices.Min()));
            double yMax = Math.Max(supportPrices.Max(), Math.Max(resistancePrices.Max(), fairPrices.Max()));

            chart.ChartAreas[0].AxisY.Minimum = yMin;
            chart.ChartAreas[0].AxisY.Maximum = yMax;

            // Ensure the yMin is not less than 0
            yMin = Math.Max(yMin, 0.01);

            // Set the y-axis range to prevent excessive zooming out
            chart.ChartAreas[0].AxisY.Minimum = yMin;
            chart.ChartAreas[0].AxisY.Maximum = yMax;

            if (logOrLinear)
            {
                chart.ChartAreas[0].AxisY.IsLogarithmic = true;
                chart.ChartAreas[0].AxisY.LogarithmBase = 10;
            }
            else
            {
                chart.ChartAreas[0].AxisY.IsLogarithmic = false;
            }

            var supportSeries = CreateSeries("Support", Color.Green, true);
            var resistanceSeries = CreateSeries("Resistance", Color.Red, true);
            var fairPriceSeries = CreateSeries("Fair", Color.Blue, true);

            for (int i = 0; i < dates.Count; i++)
            {
                supportSeries.Points.AddXY(dates[i], supportPrices[i]);
                resistanceSeries.Points.AddXY(dates[i], resistancePrices[i]);
                fairPriceSeries.Points.AddXY(dates[i], fairPrices[i]);
            }

            chart.Series.Add(supportSeries);
            chart.Series.Add(resistanceSeries);
            chart.Series.Add(fairPriceSeries);

            ResetZoom(chart);
            chart.Invalidate();

            string report = GenerateReport(dataPoints, genesisDate, exponent, fairPriceConstant, rSquared, logDeltaGB, logPrices, sumX, sumY, sumXY, sumX2, sumY2);
            richTextBoxLog.Text = report;
        }
        #endregion


        #region Public Actions
        public void UpdatePriceChart(List<DataPoint> dataPoints, bool logOrLinear)
        {
            UpdateChartCore(_priceChart, dataPoints, logOrLinear, "Price", _priceLineColor);
        }

        public void UpdateHashrateChart(List<DataPoint> dataPoints, bool logOrLinear)
        {
            UpdateChartCore(_hashrateChart, dataPoints, logOrLinear, "Hashrate", _hashrateLineColor);
        }

        public void UpdateHashrateWithPowerLaw(List<DataPoint> dataPoints, RichTextBox richTextBoxLog, Label lblRValue, bool logOrLinear, int extendLines)
        {
            UpdateChartWithPowerLawCore(_hashrateChart, dataPoints, richTextBoxLog, lblRValue, logOrLinear, _hashrateLineColor, extendLines);
        }

        public void UpdatePriceWithPowerLaw(List<DataPoint> dataPoints, RichTextBox richTextBoxLog, Label lblRValue, bool logOrLinear, int extendLines)
        {
            UpdateChartWithPowerLawCore(_priceChart, dataPoints, richTextBoxLog, lblRValue, logOrLinear, _priceLineColor, extendLines);
        }

        public void ResetZoom(bool priceChart = true)
        {
            if(priceChart)
            {
                ResetZoom(_priceChart);
            }
            else
            {
                ResetZoom(_hashrateChart);
            }
        }

        public void ZoomToFit(bool priceChart = true)
        {
            if (priceChart)
            {
                ZoomToFitAllDataPoints(_priceChart);
            }
            else
            {
                ZoomToFitAllDataPoints(_hashrateChart);
            }
        }

        public void ChangeDataPointSize(int newSize)
        {
            _dataPointSize = newSize;
            ChangeDataPointSizeInChart(_priceChart, _dataPointSize);
        }
        #endregion


        #region Mouse Events
        private void Chart_MouseWheel(object sender, MouseEventArgs e)
        {
            var chart = (Chart)sender;
            var xAxis = chart.ChartAreas[0].AxisX;
            var yAxis = chart.ChartAreas[0].AxisY;

            try
            {
                double zoomFactor = 0.9; // This remains for zooming in
                double minZoomSize = 0.001; // Minimum zoom size for zooming in further
                double zoomOutFactor = 1.1; // Factor for incremental zooming out

                // Get mouse position in axis values
                double mouseXValue = xAxis.PixelPositionToValue(e.Location.X);
                double mouseYValue = yAxis.PixelPositionToValue(e.Location.Y);

                if (e.Delta > 0) // Zoom in
                {
                    double posXStart = mouseXValue - (mouseXValue - xAxis.ScaleView.ViewMinimum) * zoomFactor;
                    double posXFinish = mouseXValue + (xAxis.ScaleView.ViewMaximum - mouseXValue) * zoomFactor;
                    double posYStart = mouseYValue - (mouseYValue - yAxis.ScaleView.ViewMinimum) * zoomFactor;
                    double posYFinish = mouseYValue + (yAxis.ScaleView.ViewMaximum - mouseYValue) * zoomFactor;

                    if ((posXFinish - posXStart) > minZoomSize && (posYFinish - posYStart) > minZoomSize)
                    {
                        xAxis.ScaleView.Zoom(posXStart, posXFinish);
                        yAxis.ScaleView.Zoom(posYStart, posYFinish);
                    }
                }
                else if (e.Delta < 0) // Zoom out
                {
                    double posXStart = mouseXValue - (mouseXValue - xAxis.ScaleView.ViewMinimum) * zoomOutFactor;
                    double posXFinish = mouseXValue + (xAxis.ScaleView.ViewMaximum - mouseXValue) * zoomOutFactor;
                    double posYStart = mouseYValue - (mouseYValue - yAxis.ScaleView.ViewMinimum) * zoomOutFactor;
                    double posYFinish = mouseYValue + (yAxis.ScaleView.ViewMaximum - mouseYValue) * zoomOutFactor;

                    xAxis.ScaleView.Zoom(posXStart, posXFinish);
                    yAxis.ScaleView.Zoom(posYStart, posYFinish);
                }

                // Adjust x-axis label format based on zoom level
                double range = xAxis.ScaleView.ViewMaximum - xAxis.ScaleView.ViewMinimum;
                if (range > 365) // If the range is greater than a year
                {
                    xAxis.LabelStyle.Format = "yyyy"; // Yearly format
                }
                else
                {
                    xAxis.LabelStyle.Format = "dd-MM-yyyy\nhh:mm:ss tt"; // Daily format with time
                }
            }
            catch (Exception ex)
            {
                // Log or handle exception as needed
                Thread.Sleep(100);
            }
        }

        private void Chart_MouseDown(object sender, MouseEventArgs e)
        {
            var chart = (Chart)sender;

            if (e.Button == MouseButtons.Right)
            {
                _panStartPoint = e.Location;
                var xAxis = chart.ChartAreas[0].AxisX;
                var yAxis = chart.ChartAreas[0].AxisY;
                _xPanStartMin = xAxis.ScaleView.ViewMinimum;
                _xPanStartMax = xAxis.ScaleView.ViewMaximum;
                _yPanStartMin = yAxis.ScaleView.ViewMinimum;
                _yPanStartMax = yAxis.ScaleView.ViewMaximum;
                chart.Cursor = Cursors.NoMove2D;
            }
        }

        private void Chart_MouseUp(object sender, MouseEventArgs e)
        {
            var chart = (Chart)sender;

            if (e.Button == MouseButtons.Left)
            {
                chart.Cursor = Cursors.Default;
            }
            else if (e.Button == MouseButtons.Right)
            {                
                chart.Cursor = Cursors.Default;                               
            }
        }

        private void Chart_MouseMove(object sender, MouseEventArgs e)
        {
            var chart = (Chart)sender;
            if (e.Button == MouseButtons.Right)
            {
                var xAxis = chart.ChartAreas[0].AxisX;
                var yAxis = chart.ChartAreas[0].AxisY;

                try
                {
                    double deltaX = xAxis.PixelPositionToValue(e.Location.X) - xAxis.PixelPositionToValue(_panStartPoint.X);
                    double deltaY = yAxis.PixelPositionToValue(e.Location.Y) - yAxis.PixelPositionToValue(_panStartPoint.Y);
                    double newXPosition = _xPanStartMin - deltaX;
                    double newYPosition = _yPanStartMin - deltaY;

                    // Ensure the new position values do not exceed bounds
                    if (newXPosition < xAxis.Minimum) newXPosition = xAxis.Minimum;
                    if (newXPosition > xAxis.Maximum - xAxis.ScaleView.Size) newXPosition = xAxis.Maximum - xAxis.ScaleView.Size;

                    xAxis.ScaleView.Position = newXPosition;
                    yAxis.ScaleView.Position = newYPosition;
                }
                catch (OverflowException ex)
                {
                    Thread.Sleep(500);
                    //MessageBox.Show($"Overflow error during panning: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"An error occurred during panning: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Chart_MouseEnter(object sender, EventArgs e)
        {
            var chart = (Chart)sender;
            chart.Cursor = Cursors.Hand; // Set cursor to hand when entering the chart
        }

        private void Chart_MouseLeave(object sender, EventArgs e)
        {
            var chart = (Chart)sender;
            chart.Cursor = Cursors.Default; // Reset cursor to default when leaving the chart
        }
        #endregion


        #region Helpers
        public void ShowLegends()
        {
            if (!_priceChart.Legends.Any())
            {
                _priceChart.Legends.Add(new Legend { Enabled = true });
            }

            if (!_hashrateChart.Legends.Any())
            {
                _hashrateChart.Legends.Add(new Legend { Enabled = true });
            }
        }

        public static string FormatHashrateGetLabel(double hashrate)
        {
            string[] units = { "H/s", "KH/s", "MH/s", "GH/s", "TH/s", "PH/s", "EH/s" };
            int unitIndex = units.Length - 1;

            // Start with the highest unit and scale down
            while (unitIndex > 0 && hashrate < 1e3)
            {
                hashrate *= 1e3;
                unitIndex--;
            }

            return $"{units[unitIndex]}";
        }

        public static string FormatHashrateGetNumber(double hashrate)
        {
            string[] units = { "H/s", "KH/s", "MH/s", "GH/s", "TH/s", "PH/s", "EH/s" };
            int unitIndex = 0;

            while (hashrate >= 1000 && unitIndex < units.Length - 1)
            {
                hashrate /= 1000.0;
                unitIndex++;
            }

            // Ensure the value is converted to the highest possible unit
            while (hashrate < 1 && unitIndex > 0)
            {
                hashrate *= 1000;
                unitIndex--;
            }

            return $"{hashrate:F3}";
        }

        public static string GenerateReport(List<DataPoint> dataPoints, DateTime genesisDate, double exponent, double fairPriceConstant, double rSquared, List<double> logDeltaGB, List<double> logPrices, double sumX, double sumY, double sumXY, double sumX2, double sumY2)
        {
            var report = new StringBuilder();

            // Preprocessing Steps
            report.AppendLine("### Data Preprocessing");
            report.AppendLine("- Data Source: CoinCodex for majority of historical data and real time data");
            report.AppendLine($"- Timeframe: {genesisDate:yyyy-MM-dd} to {dataPoints.Max(dp => dp.Timestamp):yyyy-MM-dd}");
            report.AppendLine("- Preprocessing Steps:");
            report.AppendLine("  1. Filtered out data points with non-positive prices.");
            report.AppendLine("  2. Calculated the difference in days from the genesis date (deltaGB).");
            report.AppendLine("  3. Applied log transformation to deltaGB and prices.");

            // Calculation Methodology
            report.AppendLine("\n### Calculation Methodology");
            report.AppendLine("- Log-Log Regression:");
            report.AppendLine("  - Formula: log(y) = a * log(x) + b");
            report.AppendLine("  - a (Exponent): Calculated as (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX)");
            report.AppendLine("  - b (Intercept): Calculated as (sumY - a * sumX) / n");
            report.AppendLine("  - rSquared: Calculated to measure goodness of fit");
            report.AppendLine("  - Fair Price Constant: exp(b)");
            report.AppendLine("- Support and Resistance Lines:");
            report.AppendLine("  - Support Price: fairPrice * 10^-0.25");
            report.AppendLine("  - Resistance Price: fairPrice * 10^0.25");

            // Regression Constants
            report.AppendLine("\n### Regression Constants");
            report.AppendLine($"- sumX: {sumX}");
            report.AppendLine($"- sumY: {sumY}");
            report.AppendLine($"- sumXY: {sumXY}");
            report.AppendLine($"- sumX2: {sumX2}");
            report.AppendLine($"- sumY2: {sumY2}");
            report.AppendLine($"- Slope (Exponent): {exponent}");
            report.AppendLine($"- Intercept: {Math.Exp(fairPriceConstant)}");
            report.AppendLine($"- rSquared: {rSquared}");
            report.AppendLine($"- Fair Price Constant: {fairPriceConstant}");

            // Summary of Key Findings and Insights
            report.AppendLine("\n### Summary of Key Findings and Insights");
            report.AppendLine("Kaspa is the only other crypto asset besides Bitcoin to follow a power law. This indicates that Kaspa shares unique properties with Bitcoin, suggesting a fundamental value similar to that of Bitcoin. Kaspa follows Satoshi's vision by generalizing Nakamoto consensus, which allows for more scalable and efficient decentralized consensus mechanisms. This makes Kaspa a unique and innovative project in the crypto space already with several real world projects ready to use Kaspa once zk_opcode is released.");

            // Full Dataset
            report.AppendLine($"\n### Full Dataset with {dataPoints.Count} unique data points");
            report.AppendLine("Timestamp,Price,Hashrate");
            foreach (var point in dataPoints)
            {
                report.AppendLine($"{point.Timestamp:yyyy-MM-dd HH:mm:ss},{point.Price},{point.Hashrate}");
            }

            return report.ToString();
        }
        
        private void TurnPanningOn(Chart chart)
        {
            _isPanning = true;
            chart.ChartAreas[0].CursorX.IsUserEnabled = true;
            chart.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart.ChartAreas[0].CursorY.IsUserEnabled = true;
            chart.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;

            // Disable snapping to grid
            chart.ChartAreas[0].CursorX.Interval = 0;
            chart.ChartAreas[0].CursorX.IntervalOffset = 0;
            chart.ChartAreas[0].CursorY.Interval = 0;
            chart.ChartAreas[0].CursorY.IntervalOffset = 0;
        }

        private void ResetZoom(Chart chart)
        {
            var chartArea = chart.ChartAreas[0];

            chartArea.AxisX.ScaleView.ZoomReset();
            chartArea.AxisY.ScaleView.ZoomReset();

            chartArea.RecalculateAxesScale();
        }

        private Series CreateSeries(string name, Color color, bool powerLawLines = false)
        {
            if (!powerLawLines)
            {
                return new Series(name)
                {
                    ChartType = SeriesChartType.Line,
                    Color = color,
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = _dataPointSize,
                    ToolTip = "#VALX{dd-MM-yyyy}\n#VALX{hh:mm:ss tt}\n#VALY{F4}",
                    IsXValueIndexed = false
                };
            }

            return new Series(name)
            {
                ChartType = SeriesChartType.Line,
                Color = color,
                BorderDashStyle = ChartDashStyle.Dash,
                BorderWidth = 2,
                ToolTip = "#VALX{dd-MM-yyyy}\n#VALX{hh:mm:ss tt}\n#VALY{F4}",
                IsXValueIndexed = false
            };
        }

        private void ZoomToFitAllDataPoints(Chart chart)
        {
            // Variables to hold the overall min and max values
            double xMin = double.MaxValue, xMax = double.MinValue;
            double yMin = double.MaxValue, yMax = double.MinValue;

            // Loop through all series in the chart
            foreach (var series in chart.Series)
            {
                // Ensure the series has data points
                if (series.Points.Count > 0)
                {
                    foreach (var point in series.Points)
                    {
                        // Update the min and max values for X and Y
                        xMin = Math.Min(xMin, point.XValue);
                        xMax = Math.Max(xMax, point.XValue);

                        // Ensure no invalid data for Y values (avoid NaN, Infinity)
                        double yValue = point.YValues.Min();
                        if (!double.IsNaN(yValue) && !double.IsInfinity(yValue))
                        {
                            yMin = Math.Min(yMin, yValue);
                            yMax = Math.Max(yMax, yValue);
                        }
                    }
                }
            }

            // Ensure we found valid data points
            if (xMin < double.MaxValue && xMax > double.MinValue && yMin < double.MaxValue && yMax > double.MinValue)
            {
                // Handle potential issues like range inversion (e.g., yMin > yMax)
                if (xMin == xMax)
                {
                    xMin -= 1;  // Prevent the zoom from being zero or invalid
                    xMax += 1;
                }

                if (yMin == yMax)
                {
                    yMin -= 1;  // Prevent the zoom from being zero or invalid
                    yMax += 1;
                }

                // Ensure the ranges are within reasonable bounds to avoid extreme zooming
                const double maxRange = 1e6;  // You can adjust this to suit your data
                xMin = Math.Max(xMin, 0);  // Ensure xMin is non-negative
                xMax = Math.Min(xMax, maxRange);
                yMin = Math.Max(yMin, 0);  // Ensure yMin is non-negative
                yMax = Math.Min(yMax, maxRange);

                // Adjust the axis view to include all points
                try
                {
                    var chartArea = chart.ChartAreas[0];
                    chartArea.AxisX.ScaleView.Zoom(xMin, xMax);
                    chartArea.AxisY.ScaleView.Zoom(yMin, yMax);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while adjusting zoom: {ex.Message}", "Zoom Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No data points found to zoom to fit.", "Zoom Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ChangeDataPointSizeInChart(Chart chart, int newSize)
        {
            // Loop through all series and set the data point size
            foreach (var series in chart.Series)
            {
                series.MarkerSize = newSize;
            }
        }

        #endregion

    }
}
