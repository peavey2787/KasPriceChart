using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using System;
using System.Linq;
using System.Text;

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
        #endregion


        #region Initialization
        public GraphPlotter(Chart priceChart, Chart hashrateChart)
        {
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
            var exampleSeries = new Series
            {
                ChartType = SeriesChartType.Line,
                Color = lineColor,
                MarkerStyle = MarkerStyle.Circle, // Show data points as dots
                MarkerSize = 5, // Adjust the size of the dots
                ToolTip = "#VALX{dd-MM-yyyy}\n#VALX{hh:mm:ss tt}\n#VALY{F4}" // Display tooltips with full datetime info in 12-hour format
            };

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
        }

        #endregion


        public void UpdateGraph(List<DataPoint> dataPoints)
        {
            // Ensure the y-axis is not set to logarithmic
            _priceChart.ChartAreas[0].AxisY.IsLogarithmic = false;

            _priceChart.Series.Clear();
            _hashrateChart.Series.Clear();

            var seriesPrice = new Series("Price")
            {
                ChartType = SeriesChartType.Line,
                Color = _priceLineColor, // Use stored color
                MarkerStyle = MarkerStyle.Circle, // Show data points as dots
                MarkerSize = 10, // Adjust the size of the dots
                ToolTip = "#VALX{dd-MM-yyyy}\n#VALX{hh:mm:ss tt}\n#VALY{F4}" // Display tooltips with full datetime info in 12-hour format
            };

            var seriesHashrate = new Series("Hashrate")
            {
                ChartType = SeriesChartType.Line,
                Color = _hashrateLineColor, // Use stored color
                MarkerStyle = MarkerStyle.Circle, // Show data points as dots
                MarkerSize = 10, // Adjust the size of the dots
                ToolTip = "#VALX{dd-MM-yyyy}\n#VALX{hh:mm:ss tt}\n#VALY{F4}" // Display tooltips with full datetime info in 12-hour format
            };

            foreach (var point in dataPoints)
            {
                if (point.Price > 0)
                {
                    seriesPrice.Points.AddXY(point.Timestamp, point.Price);
                }
                if (point.Hashrate > 0)
                {
                    seriesHashrate.Points.AddXY(point.Timestamp, point.Hashrate);
                }
            }

            _priceChart.Series.Add(seriesPrice);
            _hashrateChart.Series.Add(seriesHashrate);

            // Adjust y-axis for hashrate chart to show full values
            _hashrateChart.ChartAreas[0].AxisY.LabelStyle.Format = "N0"; // Show full values in normal format
            _hashrateChart.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
            _hashrateChart.ChartAreas[0].AxisY.Interval = 0; // Reset interval to allow automatic adjustment

            // Calculate yMin and yMax from the price data points
            double yMin = dataPoints.Where(dp => dp.Price > 0).Min(dp => dp.Price);
            double yMax = dataPoints.Max(dp => dp.Price);

            // Ensure the yMin is not less than 0
            yMin = Math.Max(yMin, 0.01);

            // Set the y-axis range to prevent excessive zooming out
            _priceChart.ChartAreas[0].AxisY.Minimum = yMin;
            _priceChart.ChartAreas[0].AxisY.Maximum = yMax;

            // Reset zoom to fully zoomed out
            ResetZoom(_priceChart);
            ResetZoom(_hashrateChart);

            _priceChart.Invalidate();
            _hashrateChart.Invalidate();
        }


        public void UpdateGraphWithPowerLaw(List<DataPoint> dataPoints, RichTextBox richTextBoxLog, Label lblRValue)
        {
            // Set y-axis to logarithmic
            _priceChart.ChartAreas[0].AxisY.IsLogarithmic = true;
            _priceChart.ChartAreas[0].AxisY.LogarithmBase = 10;

            _priceChart.Series.Clear();
            richTextBoxLog.Clear();


            var seriesPrice = new Series("Price")
            {
                ChartType = SeriesChartType.StepLine,
                Color = _priceLineColor,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 10,
                ToolTip = "#VALX{dd-MM-yyyy}\n#VALX{hh:mm:ss tt}\n#VALY{F4}",
                IsXValueIndexed = false
            };

            foreach (var point in dataPoints)
            {
                if (point.Price > 0)
                {
                    seriesPrice.Points.AddXY(point.Timestamp, point.Price);
                }
            }

            _priceChart.Series.Add(seriesPrice);

            var genesisDate = new DateTime(2021, 11, 7);

            // Perform log-log regression once to get the constants
            var (exponent, fairPriceConstant, rSquared, sumX, sumY, sumXY, sumX2, sumY2) = DataManager.GetRegressionConstants(dataPoints, genesisDate, richTextBoxLog);

            // Extract significant digits (mantissa) and the exponent part separately
            string significantDigits = fairPriceConstant.ToString("0.000E+0").Split('E')[0];
            string exponentPart = fairPriceConstant.ToString("E").Substring(fairPriceConstant.ToString("E").IndexOf('E'));

            // Remove leading zeroes from the exponent part
            exponentPart = exponentPart.Replace("E-0", "E-").Replace("E+0", "E+");

            // Combine the parts to show the significant digits followed by the exponent part
            string formattedFairPriceConstant = $"{significantDigits}...{exponentPart}";

            // Combine all the values into the label
            lblRValue.Text = string.Format(
                "R² = {0:F4}{1}Exponent: {2:F4}{1}Fair Price Constant: {3}",
                rSquared,
                Environment.NewLine,
                exponent,
                formattedFairPriceConstant
            );

            // Log the regression constants
            richTextBoxLog.AppendText($"Regression Constants:\nExponent: {exponent}\nFair Price Constant: {fairPriceConstant}\n\n");

            // Calculate the end date for the projection
            DateTime latestDate = dataPoints.Max(dp => dp.Timestamp);
            DateTime endDate = latestDate.AddYears(2);

            // Prepare the chart data
            var (dates, prices, supportPrices, resistancePrices, fairPrices, logDeltaGB, logPrices) = DataManager.PrepareChartData(genesisDate, dataPoints, exponent, fairPriceConstant * Math.Pow(10, -0.25), fairPriceConstant * Math.Pow(10, 0.25), fairPriceConstant, endDate);

            // Calculate yMin and yMax from the projected values
            double yMin = Math.Min(supportPrices.Min(), Math.Min(resistancePrices.Min(), fairPrices.Min())); double yMax = Math.Max(supportPrices.Max(), Math.Max(resistancePrices.Max(), fairPrices.Max())); 
            
            // Set the y-axis range to prevent excessive zooming out
            _priceChart.ChartAreas[0].AxisY.Minimum = yMin; 
            _priceChart.ChartAreas[0].AxisY.Maximum = yMax;

            setYAxisRange(supportPrices, resistancePrices, fairPrices);

            var supportSeries = CreateSeries("Support Price", Color.Green);
            var resistanceSeries = CreateSeries("Resistance Price", Color.Red);
            var fairPriceSeries = CreateSeries("Fair Price", Color.Blue);

            for (int i = 0; i < dates.Count; i++)
            {
                supportSeries.Points.AddXY(dates[i], supportPrices[i]);
                resistanceSeries.Points.AddXY(dates[i], resistancePrices[i]);
                fairPriceSeries.Points.AddXY(dates[i], fairPrices[i]);
            }

            _priceChart.Series.Add(supportSeries);
            _priceChart.Series.Add(resistanceSeries);
            _priceChart.Series.Add(fairPriceSeries);

            // Reset zoom to fully zoomed out
            ResetZoom(_priceChart);

            _priceChart.Invalidate();

            string report = GenerateReport(dataPoints, genesisDate, exponent, fairPriceConstant, rSquared, logDeltaGB, logPrices, sumX, sumY, sumXY, sumX2, sumY2);
            richTextBoxLog.Text = report;
        }
        private void setYAxisRange(List<double> supportPrices, List<double> resistancePrices, List<double> fairPrices)
        {
            // Calculate yMin and yMax from the projected values
            double yMin = Math.Min(supportPrices.Min(), Math.Min(resistancePrices.Min(), fairPrices.Min())); double yMax = Math.Max(supportPrices.Max(), Math.Max(resistancePrices.Max(), fairPrices.Max()));

            // Set the y-axis range to prevent excessive zooming out
            _priceChart.ChartAreas[0].AxisY.Minimum = yMin;
            _priceChart.ChartAreas[0].AxisY.Maximum = yMax;
        }
        private Series CreateSeries(string name, Color color)
        {
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
            }
        }

        private void Chart_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = true;
                _panStartPoint = e.Location;
                var chart = (Chart)sender;
                var xAxis = chart.ChartAreas[0].AxisX;
                var yAxis = chart.ChartAreas[0].AxisY;
                _xPanStartMin = xAxis.ScaleView.ViewMinimum;
                _xPanStartMax = xAxis.ScaleView.ViewMaximum;
                _yPanStartMin = yAxis.ScaleView.ViewMinimum;
                _yPanStartMax = yAxis.ScaleView.ViewMaximum;
                chart.Cursor = Cursors.NoMove2D;
            }
        }

        private void Chart_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                var chart = (Chart)sender;
                var xAxis = chart.ChartAreas[0].AxisX;
                var yAxis = chart.ChartAreas[0].AxisY;

                try
                {
                    double deltaX = xAxis.PixelPositionToValue(e.Location.X) - xAxis.PixelPositionToValue(_panStartPoint.X);
                    double deltaY = yAxis.PixelPositionToValue(e.Location.Y) - yAxis.PixelPositionToValue(_panStartPoint.Y);
                    double newXPosition = _xPanStartMin - deltaX;
                    double newYPosition = _yPanStartMin - deltaY;

                    // Ensure the new y-axis position is not less than 0
                    if (newYPosition < 0)
                    {
                        newYPosition = 0;
                    }

                    xAxis.ScaleView.Position = newXPosition;
                    yAxis.ScaleView.Position = newYPosition;
                }
                catch (Exception ex)
                {
                    // Log or handle exception as needed
                }
            }
        }

        private void Chart_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = false;
                var chart = (Chart)sender;
                chart.Cursor = Cursors.Hand;
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

        private void ResetZoom(Chart chart)
        {
            var chartArea = chart.ChartAreas[0];

            chartArea.AxisX.ScaleView.ZoomReset();
            chartArea.AxisY.ScaleView.ZoomReset();

            chartArea.RecalculateAxesScale();
        }

        #endregion

    }
}
