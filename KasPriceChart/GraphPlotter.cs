using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using System;
using System.Linq;

namespace KasPriceChart
{
    public class GraphPlotter
    {
        private Chart _priceChart;
        private Chart _hashrateChart;
        private Label _lblCurrentPrice;
        private Label _lblCurrentHashrate;
        private Label _lblLastTimeStamp;
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

        public GraphPlotter(Chart priceChart, Chart hashrateChart, Label lblCurrentPrice, Label lblCurrentHashrate, Label lblLastTimeStamp)
        {
            _priceChart = priceChart;
            _hashrateChart = hashrateChart;
            _lblCurrentPrice = lblCurrentPrice;
            _lblCurrentHashrate = lblCurrentHashrate;
            _lblLastTimeStamp = lblLastTimeStamp;

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


        #region Mouse Events
        private void Chart_MouseWheel(object sender, MouseEventArgs e)
        {
            var chart = (Chart)sender;
            var xAxis = chart.ChartAreas[0].AxisX;
            var yAxis = chart.ChartAreas[0].AxisY;

            try
            {
                double zoomFactor = 0.5; // Adjust this value to make zooming more gradual
                double minZoomSize = 0.01; // Minimum zoom size to prevent zooming out when zooming in

                double xMin = xAxis.ScaleView.ViewMinimum;
                double xMax = xAxis.ScaleView.ViewMaximum;
                double yMin = yAxis.ScaleView.ViewMinimum;
                double yMax = yAxis.ScaleView.ViewMaximum;

                double posXStart = xAxis.PixelPositionToValue(e.Location.X) - (xMax - xMin) * zoomFactor;
                double posXFinish = xAxis.PixelPositionToValue(e.Location.X) + (xMax - xMin) * zoomFactor;
                double posYStart = yAxis.PixelPositionToValue(e.Location.Y) - (yMax - yMin) * zoomFactor;
                double posYFinish = yAxis.PixelPositionToValue(e.Location.Y) + (yMax - yMin) * zoomFactor;

                if (e.Delta > 0) // Zoom in
                {
                    // Ensure the zoom range is within the minimum zoom size
                    if ((posXFinish - posXStart) > minZoomSize && (posYFinish - posYStart) > minZoomSize)
                    {
                        xAxis.ScaleView.Zoom(posXStart, posXFinish);
                        yAxis.ScaleView.Zoom(posYStart, posYFinish);
                    }
                }
                else if (e.Delta < 0) // Zoom out
                {
                    xAxis.ScaleView.ZoomReset();
                    yAxis.ScaleView.ZoomReset();
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

                    // Ensure the new position is within valid range
                    newXPosition = Math.Max(xAxis.Minimum, Math.Min(newXPosition, xAxis.Maximum - xAxis.ScaleView.Size));
                    newYPosition = Math.Max(yAxis.Minimum, Math.Min(newYPosition, yAxis.Maximum - yAxis.ScaleView.Size));

                    xAxis.ScaleView.Position = newXPosition;
                    yAxis.ScaleView.Position = newYPosition;
                }
                catch { }
            }
        }

        private void Chart_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = false;
            }
        }
        #endregion


        public void UpdateGraph(List<DataPoint> dataPoints, string selectedTimespan)
        {
            // Filter data points based on the selected timespan
            List<DataPoint> filteredDataPoints = FilterDataPoints(dataPoints, selectedTimespan);

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

            double latestPrice = 0;
            double latestHashrate = 0;
            DateTime latestTimestamp = DateTime.Now;

            foreach (var point in filteredDataPoints)
            {
                if (point.Price > 0)
                {
                    seriesPrice.Points.AddXY(point.Timestamp, point.Price);
                    latestPrice = point.Price;
                }
                if (point.Hashrate > 0)
                {
                    seriesHashrate.Points.AddXY(point.Timestamp, point.Hashrate);
                    latestHashrate = point.Hashrate;
                }
                if (point.Timestamp != null)
                {
                    latestTimestamp = point.Timestamp;
                }
            }

            _priceChart.Series.Add(seriesPrice);
            _hashrateChart.Series.Add(seriesHashrate);

            // Adjust y-axis for hashrate chart to show full values
            _hashrateChart.ChartAreas[0].AxisY.LabelStyle.Format = "N0"; // Show full values in normal format
            _hashrateChart.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
            _hashrateChart.ChartAreas[0].AxisY.Interval = 0; // Reset interval to allow automatic adjustment

            _priceChart.Invalidate();
            _hashrateChart.Invalidate();

            // Update labels with the latest data point
            if (filteredDataPoints.Count > 0)
            {
                if (latestPrice > 0)
                {
                    _lblCurrentPrice.Text = $"Price: ${latestPrice:F4}";
                }
                else
                {
                    _lblCurrentPrice.Text = "Error Fetching Price";
                }
                if (latestHashrate > 0)
                {
                    _lblCurrentHashrate.Text = $"Hashrate: {FormatHashrateGetNumber(latestHashrate)} {FormatHashrateGetLabel(latestHashrate)}";
                }
                else
                {
                    _lblCurrentHashrate.Text = "Error Fetching Hashrate";
                }

                _lblLastTimeStamp.Text = $"Last Update: {latestTimestamp:dd-MM-yyyy hh:mm:ss tt}";
            }
        }
        private List<DataPoint> FilterDataPoints(List<DataPoint> dataPoints, string timespan)
        {
            // Validate and parse the timespan string
            var match = System.Text.RegularExpressions.Regex.Match(timespan, @"^(\d+)\s*(minute|minutes|mins|hour|hours|hr|hrs|day|days|week|weeks|month|months|year|years)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                // Return all data points if the timespan format is invalid
                return dataPoints;
            }

            // Extract number and unit from the validated timespan
            int number = int.Parse(match.Groups[1].Value);
            string unit = match.Groups[2].Value.ToLower();

            // Convert unit to full name with proper capitalization
            string fullUnit;
            switch (unit)
            {
                case "minute":
                case "minutes":
                case "mins":
                    fullUnit = "Minutes";
                    break;
                case "hour":
                case "hours":
                case "hr":
                case "hrs":
                    fullUnit = "Hours";
                    break;
                case "day":
                case "days":
                    fullUnit = "Days";
                    break;
                case "week":
                case "weeks":
                    fullUnit = "Weeks";
                    break;
                case "month":
                case "months":
                    fullUnit = "Months";
                    break;
                case "year":
                case "years":
                    fullUnit = "Years";
                    break;
                default:
                    throw new ArgumentException("Invalid time unit");
            }

            // Calculate the interval based on the parsed number and unit
            TimeSpan interval;
            switch (fullUnit)
            {
                case "Minutes":
                    interval = TimeSpan.FromMinutes(number);
                    break;
                case "Hours":
                    interval = TimeSpan.FromHours(number);
                    break;
                case "Days":
                    interval = TimeSpan.FromDays(number);
                    break;
                case "Weeks":
                    interval = TimeSpan.FromDays(number * 7);
                    break;
                case "Months":
                    interval = TimeSpan.FromDays(number * 30);
                    break;
                case "Years":
                    interval = TimeSpan.FromDays(number * 365);
                    break;
                default:
                    throw new ArgumentException("Invalid time unit");
            }

            // Define the allowable variance
            TimeSpan variance = TimeSpan.FromMinutes(2);

            // Filter data points based on the interval
            List<DataPoint> filteredDataPoints = new List<DataPoint>();

            if (dataPoints.Count > 0)
            {
                for (int i = 0; i < dataPoints.Count; i++)
                {
                    bool foundMatch = false;

                    DateTime previousTimestamp = dataPoints[i].Timestamp;

                    for (int j = i + 1; j < dataPoints.Count; j++)
                    {
                        TimeSpan difference = dataPoints[j].Timestamp - previousTimestamp;

                        // Check if the difference matches the interval +/- variance
                        if (difference >= interval - variance && difference <= interval + variance)
                        {
                            filteredDataPoints.Add(dataPoints[j]);
                            previousTimestamp = dataPoints[j].Timestamp;
                            i = j - 1; // Move the outer loop to the current position
                            foundMatch = true;
                            break;
                        }
                    }

                    // If no match found within the interval, move to the next point
                    if (!foundMatch && i < dataPoints.Count - 1)
                    {
                        previousTimestamp = dataPoints[i + 1].Timestamp;
                    }
                }
            }

            return filteredDataPoints;
        }




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

        private string FormatHashrateGetLabel(double hashrate)
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

        private string FormatHashrateGetNumber(double hashrate)
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


    }
}
