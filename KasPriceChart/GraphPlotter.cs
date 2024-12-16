using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using System;
using System.Linq;
using System.Text;
using System.Drawing.Drawing2D;
using System.Threading;
using Cursor = System.Windows.Forms.Cursor;
using System.Runtime.InteropServices.ComTypes;

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

            // Set initial x and y axis intervals
            double initialYInterval = chart.ChartAreas[0].AxisY.Interval;
            //SetAxisIntervals(chart, _initialZoomLevel, initialYInterval);
            SetAutoAxisIntervals(chart);

            // Format y-axis labels to show values with 4 decimal places
            chart.ChartAreas[0].AxisY.LabelStyle.Format = "F4";

            TurnPanningOn(chart);
        }
        #endregion


        #region Core Logic
        private void UpdateChartCore(Chart chart, List<DataPoint> dataPoints, bool logOrLinear, string seriesName, Color seriesColor)
        {
            chart.Series.Clear();
            if (dataPoints == null || dataPoints.Count == 0) return;

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

            if (seriesName == "Hashrate")
            {
                chart.ChartAreas[0].AxisY.LabelStyle.Format = "F4"; 
                chart.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                chart.ChartAreas[0].AxisY.Interval = 0; // Reset interval to allow automatic adjustment
            }

            if (seriesName == "Price")
            {
                double yMin = dataPoints.Where(dp => dp.Price > 0).Min(dp => dp.Price);
                double yMax = dataPoints.Max(dp => dp.Price);

                yMin = Math.Max(yMin, 0.001);

                chart.ChartAreas[0].AxisY.Minimum = yMin;
                chart.ChartAreas[0].AxisY.Maximum = yMax;
            }

            if (logOrLinear)
            {
                chart.ChartAreas[0].AxisY.IsLogarithmic = true;
                chart.ChartAreas[0].AxisY.LogarithmBase = 10;
            }
            else
            {
                chart.ChartAreas[0].AxisY.IsLogarithmic = false;
            }

            // Calculate initial xInterval and yInterval for setting the axis intervals
            double initialYInterval = chart.ChartAreas[0].AxisY.Interval; // Adjust as needed

            // Reset zoom to fully zoomed out
            ResetZoom(chart);

            chart.Invalidate();
        }


        private void UpdateChartWithPowerLawCore(Chart chart, List<DataPoint> dataPoints, bool logOrLinear, Color lineColor, int extendLines, Dictionary<DateTime, double> supportPrices, Dictionary<DateTime, double> resistancePrices, Dictionary<DateTime, double> fairPrices)
        {
            chart.Series.Clear();

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

            DateTime latestDate = dataPoints.Max(dp => dp.Timestamp);
            DateTime endDate = latestDate.AddDays(extendLines);

            double yMin = Math.Min(supportPrices.Values.Min(), Math.Min(resistancePrices.Values.Min(), fairPrices.Values.Min()));
            double yMax = Math.Max(supportPrices.Values.Max(), Math.Max(resistancePrices.Values.Max(), fairPrices.Values.Max()));

            // Ensure the yMin is not less than 0
            yMin = Math.Max(yMin, 0.001);

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

            for (int i = 0; i < fairPrices.Values.Count; i++)
            {
                var timestamp = fairPrices.ElementAt(i).Key; // Get timestamp

                if (supportPrices.TryGetValue(timestamp, out double supportPrice))
                {
                    supportSeries.Points.AddXY(timestamp, supportPrice);
                }

                if (resistancePrices.TryGetValue(timestamp, out double resistancePrice))
                {
                    resistanceSeries.Points.AddXY(timestamp, resistancePrice);
                }

                if (fairPrices.TryGetValue(timestamp, out double fairPrice))
                {
                    fairPriceSeries.Points.AddXY(timestamp, fairPrice);
                }
            }

            chart.Series.Add(supportSeries);
            chart.Series.Add(resistanceSeries);
            chart.Series.Add(fairPriceSeries);

            chart.Invalidate();
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

        public void UpdateHashrateWithPowerLaw(List<DataPoint> dataPoints, bool logOrLinear, int extendLines, Dictionary<DateTime, double> supportPrices, Dictionary<DateTime, double> resistancePrices, Dictionary<DateTime, double> fairPrices)
        {
            //UpdateChartWithPowerLawCore(_hashrateChart, dataPoints, logOrLinear, _hashrateLineColor, extendLines, supportPrices, resistancePrices, fairPrices);
        }

        public void UpdatePriceWithPowerLaw(List<DataPoint> dataPoints, bool logOrLinear, int extendLines, Dictionary<DateTime, double> supportPrices, Dictionary<DateTime, double> resistancePrices, Dictionary<DateTime, double> fairPrices)
        {
            UpdateChartWithPowerLawCore(_priceChart, dataPoints, logOrLinear, _priceLineColor, extendLines, supportPrices, resistancePrices, fairPrices);
        }

        public void ResetZoom(bool priceChart = true)
        {
            if (priceChart)
            {
                ResetZoom(_priceChart);
            }
            else
            {
                ResetZoom(_hashrateChart);
            }
        }

        public void ChangeDataPointSize(int newSize)
        {
            _dataPointSize = newSize;
            ChangeDataPointSizeInChart(_priceChart, _dataPointSize);
        }

        #endregion


        #region Mouse Events
        private DateTime _lastZoomTime = DateTime.MinValue;

        private void Chart_MouseWheel(object sender, MouseEventArgs e)
        {
            var chart = (Chart)sender;
            double zoomFactor = 0.9;
            bool zoomIn = e.Delta > 0;

            // Add debounce to limit frequency of zoom operations
            if ((DateTime.Now - _lastZoomTime).TotalMilliseconds > 100) // Adjust threshold as needed
            {
                ZoomChart(chart, zoomFactor, zoomIn);
                _lastZoomTime = DateTime.Now;
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
                chart.Cursor = Cursors.Hand;
            }
            else if (e.Button == MouseButtons.Right)
            {
                chart.Cursor = Cursors.Hand;
            }
        }

        private void Chart_MouseMove(object sender, MouseEventArgs e)
        {           
            if (e.Button == MouseButtons.Right && _panStartPoint != Point.Empty)
            {
                var chart = (Chart)sender;
                var xAxis = chart.ChartAreas[0].AxisX;
                var yAxis = chart.ChartAreas[0].AxisY;

                try
                {
                    // Calculate the difference between the start point and the current point
                    double dx = e.Location.X - _panStartPoint.X;
                    double dy = e.Location.Y - _panStartPoint.Y;

                    // Calculate the new view positions
                    double xPixelRange = chart.ChartAreas[0].AxisX.ScaleView.Size;
                    double yPixelRange = chart.ChartAreas[0].AxisY.ScaleView.Size;
                    double xStart = xAxis.PixelPositionToValue(_panStartPoint.X);
                    double xCurrent = xAxis.PixelPositionToValue(e.Location.X);
                    double yStart = yAxis.PixelPositionToValue(_panStartPoint.Y);
                    double yCurrent = yAxis.PixelPositionToValue(e.Location.Y);                    

                    double xOffset = (xCurrent - xStart);
                    double yOffset = (yCurrent - yStart);

                    double newXMin = _xPanStartMin - xOffset;
                    double newXMax = _xPanStartMax - xOffset;
                    double newYMin = _yPanStartMin - yOffset;
                    double newYMax = _yPanStartMax - yOffset;

                    xAxis.ScaleView.Zoom(newXMin, newXMax);
                    yAxis.ScaleView.Zoom(newYMin, newYMax);

                    chart.Invalidate();
                    chart.Update();
                }
                catch (ArgumentException ex)
                {
                    // Handle the exception gracefully, for example, by logging the error or displaying a message
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

            chart.Invalidate();
            chart.Update();
        }

        private void ResetZoom(Chart chart)
        {
            var chartArea = chart.ChartAreas[0];

            // Reset zoom for both axes
            chartArea.AxisX.ScaleView.ZoomReset(0);
            chartArea.AxisY.ScaleView.ZoomReset(0);

            // Recalculate the axes to ensure proper updates
            chartArea.RecalculateAxesScale();

            // Optionally, reset the axis intervals to auto mode
            chartArea.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chartArea.AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;

            // Optionally, reset any manually set intervals
            chartArea.AxisX.Interval = 0;
            chartArea.AxisY.Interval = 0;

            // Invalidate and update the chart to reflect changes
            chart.Invalidate();
            chart.Update();
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

        private void ChangeDataPointSizeInChart(Chart chart, int newSize)
        {
            // Loop through all series and set the data point size
            foreach (var series in chart.Series)
            {
                series.MarkerSize = newSize;
            }
        }

        private void ZoomChart(Chart chart, double zoomFactor, bool zoomIn)
        {
            var xAxis = chart.ChartAreas[0].AxisX;
            var yAxis = chart.ChartAreas[0].AxisY;

            double minZoomSize = 0.0001; // Minimum zoom size to prevent excessive zoom-in
            double zoomOutFactor = 1 / zoomFactor; // Factor for zooming out

            try
            {
                // Get the current visible range
                double xRangeStart = xAxis.ScaleView.ViewMinimum;
                double xRangeEnd = xAxis.ScaleView.ViewMaximum;
                double yRangeStart = yAxis.ScaleView.ViewMinimum;
                double yRangeEnd = yAxis.ScaleView.ViewMaximum;

                // Calculate the midpoints of the current visible range
                double xCenter = (xRangeStart + xRangeEnd) / 2;
                double yCenter = (yRangeStart + yRangeEnd) / 2;

                // Calculate the new zoom ranges for X-axis
                double xRangeHalf = (xRangeEnd - xRangeStart) / 2;
                double newXHalf = xRangeHalf * (zoomIn ? zoomFactor : zoomOutFactor);
                double posXStart = xCenter - newXHalf;
                double posXFinish = xCenter + newXHalf;

                // Enforce axis bounds for X-axis
                posXStart = Math.Max(xAxis.Minimum, posXStart);
                posXFinish = Math.Min(xAxis.Maximum, posXFinish);

                double posYStart, posYFinish;

                if (yAxis.IsLogarithmic)
                {
                    // Calculate the center value for the y-axis
                    double centerYValue = Math.Pow(10, (Math.Log10(yAxis.ScaleView.ViewMinimum) + Math.Log10(yAxis.ScaleView.ViewMaximum)) / 2);

                    // Ensure the value is positive and above the minimum threshold for logarithmic scales
                    double centerYValueValid = Math.Max(centerYValue, yAxis.Minimum);

                    if (centerYValueValid > 0)
                    {
                        double logCenterYValue = Math.Log10(centerYValueValid);
                        double logViewMin = Math.Log10(Math.Max(yAxis.ScaleView.ViewMinimum, yAxis.Minimum));
                        double logViewMax = Math.Log10(Math.Max(yAxis.ScaleView.ViewMaximum, yAxis.Minimum));

                        // Adjust zoom out factor for logarithmic scale
                        double adjustedZoomOutFactor = zoomIn ? zoomFactor : 1 / (1 + (zoomFactor - 1) / 10);

                        // Calculate new log positions
                        posYStart = Math.Pow(10, logCenterYValue - (logCenterYValue - logViewMin) * adjustedZoomOutFactor);
                        posYFinish = Math.Pow(10, logCenterYValue + (logViewMax - logCenterYValue) * adjustedZoomOutFactor);

                        // Ensure calculated positions remain within valid range
                        posYStart = Math.Max(yAxis.Minimum, posYStart);
                        posYFinish = Math.Min(yAxis.Maximum, posYFinish);
                    }
                    else
                    {
                        posYStart = yAxis.ScaleView.ViewMinimum;
                        posYFinish = yAxis.ScaleView.ViewMaximum;
                    }
                }
                else
                {
                    // Linear scale adjustments for Y-axis
                    double yRangeHalf = (yRangeEnd - yRangeStart) / 2;
                    double newYHalf = yRangeHalf * (zoomIn ? zoomFactor : zoomOutFactor);
                    posYStart = yCenter - newYHalf;
                    posYFinish = yCenter + newYHalf;

                    // Enforce bounds
                    posYStart = Math.Max(yAxis.Minimum, posYStart);
                    posYFinish = Math.Min(yAxis.Maximum, posYFinish);
                }

                // Ensure zoom size does not fall below the minimum threshold
                if ((posXFinish - posXStart) > minZoomSize && (posYFinish - posYStart) > minZoomSize)
                {
                    xAxis.ScaleView.Zoom(posXStart, posXFinish);
                    yAxis.ScaleView.Zoom(posYStart, posYFinish);
                }

                // Dynamically update axis intervals
                SetAutoAxisIntervals(chart);

                // Redraw the chart
                chart.Invalidate();
                chart.Update();
            }
            catch (Exception ex)
            {
                // Handle exceptions gracefully
                System.Diagnostics.Debug.WriteLine($"Error during zooming: {ex.Message}");
            }
        }




        private void SetAutoAxisIntervals(Chart chart)
        {
            var xAxis = chart.ChartAreas[0].AxisX;

            // Set IntervalAutoMode to VariableCount for automatic intervals
            xAxis.IntervalAutoMode = IntervalAutoMode.VariableCount;

            // Optionally disable manually set intervals
            xAxis.Interval = 0;

            // Ensure labels are enabled and auto-fitting is allowed
            xAxis.LabelStyle.Enabled = true;
            xAxis.IsLabelAutoFit = true;

            // Optionally set label format for better readability
            xAxis.LabelStyle.Format = "MMM dd-yy\nhh:mm tt";
        }


        #endregion

    }





}






