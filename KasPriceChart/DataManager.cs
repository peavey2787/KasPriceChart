using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KasPriceChart
{
    public class DataManager
    {
        private List<DataPoint> _dataPoints;

        public DataManager()
        {
            _dataPoints = new List<DataPoint>();
        }

        public void AddData(DateTime timestamp, double price, double hashrate)
        {
            if (price > 0 || hashrate > 0)
            {
                // Check if a data point with the same timestamp already exists
                var existingDataPoint = _dataPoints.FirstOrDefault(dp => dp.Timestamp == timestamp);

                if (existingDataPoint == null)
                {
                    _dataPoints.Add(new DataPoint { Timestamp = timestamp, Price = price, Hashrate = hashrate });
                }
                else
                {
                    // Update the existing data point if it already exists
                    if (price > 0)
                    {
                        existingDataPoint.Price = price;
                    }

                    if (hashrate > 0)
                    {
                        existingDataPoint.Hashrate = hashrate;
                    }
                }
            }
        }

        public List<DataPoint> GetData()
        {
            _dataPoints.Sort((x, y) =>
            {
                return DateTime.Compare(x.Timestamp, y.Timestamp);
            });

            return new List<DataPoint>(_dataPoints); // Returning a new list to prevent external modifications
        }

        public void SetData(List<DataPoint> dataPoints)
        {
            _dataPoints = dataPoints;
        }

        public void MergeData(List<DataPoint> dataPoints)
        {
            foreach (var newDataPoint in dataPoints)
            {
                // Check if the data point with the same timestamp exists
                var existingDataPoint = _dataPoints.FirstOrDefault(dp => dp.Timestamp == newDataPoint.Timestamp);

                if (existingDataPoint != null)
                {
                    // Overwrite the existing data point with the new one
                    existingDataPoint.Price = newDataPoint.Price;
                    existingDataPoint.Hashrate = newDataPoint.Hashrate;
                }
                else
                {
                    // Add the new data point if it doesn't exist
                    _dataPoints.Add(newDataPoint);
                }
            }

            // Sort the data points by their timestamps
            _dataPoints.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));
        }

        public static List<DataPoint> FilterDataPoints(List<DataPoint> dataPoints, string timespan)
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

        public static (double latestPrice, double latestHashrate, DateTime latestTimestamp) GetLatestData(List<DataPoint> dataPoints)
        {
            double latestPrice = 0;
            double latestHashrate = 0;
            DateTime latestTimestamp = DateTime.MinValue;

            foreach (var point in dataPoints)
            {
                if (point.Price > 0)
                {
                    latestPrice = point.Price;
                }
                if (point.Hashrate > 0)
                {
                    latestHashrate = point.Hashrate;
                }
                if (point.Timestamp != null)
                {
                    latestTimestamp = point.Timestamp;
                }
            }

            return (latestPrice, latestHashrate, latestTimestamp);
        }


        public static (double exponent, double fairPriceConstant, double rSquared, double sumX, double sumY, double sumXY, double sumX2, double sumY2) GetRegressionConstants(List<DataPoint> dataPoints, DateTime genesisDate, RichTextBox richTextBoxLog)
        {
            var deltaGB = new List<double>();
            var prices = new List<double>();

            if (dataPoints == null || dataPoints.Count == 0)
            {
                richTextBoxLog.AppendText("Error: Data points list is empty or null.\n");
                return (double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
            }

            var earliestDate = dataPoints.Min(dp => dp.Timestamp);
            if (genesisDate >= earliestDate)
            {
                richTextBoxLog.AppendText($"Genesis date {genesisDate} is not earlier than the earliest data date {earliestDate}\n");
                genesisDate = earliestDate.AddDays(-1); // Adjust genesisDate to be 1 day before the earliest data date
            }

            foreach (var point in dataPoints)
            {
                double delta = (point.Timestamp - genesisDate).TotalDays;
                if (delta <= 0)
                {
                    richTextBoxLog.AppendText($"Invalid delta for date: {point.Timestamp}, delta: {delta}\n");
                    delta = 1; // Set to 1 day if delta is zero or negative
                }
                deltaGB.Add(delta);
                prices.Add(point.Price);

                if (point.Price <= 0)
                {
                    richTextBoxLog.AppendText($"Invalid Price: {point.Price} for {point.Timestamp}\n");
                }
            }

            if (deltaGB.Count == 0 || prices.Count == 0)
            {
                richTextBoxLog.AppendText("Empty deltaGB or prices array\n");
                throw new InvalidOperationException("Empty deltaGB or prices array");
            }

            const double THRESHOLD = 1e-6;

            var logDeltaGB = deltaGB.Select((x, index) =>
            {
                if (x <= THRESHOLD)
                {
                    richTextBoxLog.AppendText($"Error: Non-positive delta (below threshold): {x} at index: {index}\n");
                }
                return Math.Log(x);
            }).ToList();

            var logPrices = prices.Select((x, index) =>
            {
                if (x <= THRESHOLD)
                {
                    richTextBoxLog.AppendText($"Error: Non-positive price (below threshold): {x} at index: {index}\n");
                }
                return Math.Log(x);
            }).ToList();

            if (logDeltaGB.Count == 0 || logPrices.Count == 0)
            {
                richTextBoxLog.AppendText("Log values are invalid, cannot proceed with regression\n");
                throw new InvalidOperationException("Log values are invalid, cannot proceed with regression");
            }

            var regression = LinearRegression(logDeltaGB, logPrices, richTextBoxLog);

            if (double.IsNaN(regression.slope) || double.IsNaN(regression.intercept))
            {
                richTextBoxLog.AppendText("Regression resulted in NaN values for slope or intercept\n");
                throw new InvalidOperationException("Regression resulted in NaN values for slope or intercept");
            }

            return (exponent: regression.slope, fairPriceConstant: Math.Exp(regression.intercept), rSquared: regression.rSquared, sumX: regression.sumX, sumY: regression.sumY, sumXY: regression.sumXY, sumX2: regression.sumX2, sumY2: regression.sumY2);
        }

        private static (double slope, double intercept, double rSquared, double sumX, double sumY, double sumXY, double sumX2, double sumY2) LinearRegression(List<double> x, List<double> y, RichTextBox richTextBoxLog)
        {
            int n = x.Count;
            if (n != y.Count)
            {
                richTextBoxLog.AppendText("Arrays x and y must have the same length\n");
                throw new ArgumentException("Arrays x and y must have the same length");
            }

            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;

            for (int i = 0; i < n; i++)
            {
                sumX += x[i];
                sumY += y[i];
                sumXY += x[i] * y[i];
                sumX2 += x[i] * x[i];
                sumY2 += y[i] * y[i];

                if (double.IsNaN(x[i]) || double.IsNaN(y[i]))
                {
                    richTextBoxLog.AppendText($"Invalid data at index {i}: x={x[i]}, y={y[i]}\n");
                }
            }

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double intercept = (sumY - slope * sumX) / n;
            double rSquared = Math.Pow((n * sumXY - sumX * sumY) / Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY)), 2);

            richTextBoxLog.AppendText($"sumX: {sumX}\n");
            richTextBoxLog.AppendText($"sumY: {sumY}\n");
            richTextBoxLog.AppendText($"sumXY: {sumXY}\n");
            richTextBoxLog.AppendText($"sumX2: {sumX2}\n");
            richTextBoxLog.AppendText($"sumY2: {sumY2}\n");
            richTextBoxLog.AppendText($"slope: {slope}\n");
            richTextBoxLog.AppendText($"intercept: {intercept}\n");
            richTextBoxLog.AppendText($"rSquared: {rSquared}\n");

            return (slope, intercept, rSquared, sumX, sumY, sumXY, sumX2, sumY2);
        }

        public static (double supportPrice, double resistancePrice, double fairPrice) CalculatePowerLawPrices(double exponent, double fairPriceConstant, DateTime genesisDate, DateTime currentDate)
        {
            double deltaGB = (currentDate - genesisDate).TotalDays;
            double fairPrice = fairPriceConstant * Math.Pow(deltaGB, exponent);
            double supportPrice = fairPrice * Math.Pow(10, -0.25); // adjust these constants to match the correctly calculated power law lines
            double resistancePrice = fairPrice * Math.Pow(10, 0.25);

            return (supportPrice, resistancePrice, fairPrice);
        }

        public static (List<DateTime> dates, List<double> prices, List<double> supportPrices, List<double> resistancePrices, List<double> fairPrices, List<double> logDeltaGB, List<double> logPrices) PrepareChartData(DateTime genesisDate, List<DataPoint> dataPoints, double exponent, double supportPriceConstant, double resistancePriceConstant, double fairPriceConstant, DateTime endDate)
        {
            var dates = new List<DateTime>();
            var prices = new List<double>();
            var supportPrices = new List<double>();
            var resistancePrices = new List<double>();
            var fairPrices = new List<double>();
            var logDeltaGB = new List<double>();
            var logPrices = new List<double>();

            foreach (var point in dataPoints)
            {
                var date = point.Timestamp;
                var price = point.Price;

                if (price > 0)
                {
                    dates.Add(date);

                    double deltaGB = (date - genesisDate).TotalDays;
                    supportPrices.Add(supportPriceConstant * Math.Pow(deltaGB, exponent));
                    resistancePrices.Add(resistancePriceConstant * Math.Pow(deltaGB, exponent));
                    fairPrices.Add(fairPriceConstant * Math.Pow(deltaGB, exponent));
                    prices.Add(price);

                    logDeltaGB.Add(Math.Log(deltaGB));
                    logPrices.Add(Math.Log(price));
                }
            }

            DateTime latestDate = dataPoints.Max(dp => dp.Timestamp);
            for (DateTime futureDate = latestDate.AddDays(1); futureDate <= endDate; futureDate = futureDate.AddDays(1))
            {
                dates.Add(futureDate);

                double deltaGB = (futureDate - genesisDate).TotalDays;
                supportPrices.Add(supportPriceConstant * Math.Pow(deltaGB, exponent));
                resistancePrices.Add(resistancePriceConstant * Math.Pow(deltaGB, exponent));
                fairPrices.Add(fairPriceConstant * Math.Pow(deltaGB, exponent));
            }

            return (dates, prices, supportPrices, resistancePrices, fairPrices, logDeltaGB, logPrices);
        }
    }

    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Price { get; set; }
        public double Hashrate { get; set; }
    }
}
