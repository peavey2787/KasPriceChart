using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public static List<DataPoint> FilterDataPointsForView(List<DataPoint> dataPoints, string timespan)
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

        public static List<DataPoint> FilterDataPointsByDateRange(List<DataPoint> dataPoints, DateTime startDate, DateTime endDate)
        {
            List<DataPoint> filteredDataPoints = new List<DataPoint>();
            foreach (var point in dataPoints)
            {
                if (point.Timestamp >= startDate && point.Timestamp <= endDate)
                {
                    filteredDataPoints.Add(point);
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

        public static (double exponent, double fairPriceConstant, double rSquared, double sumX, double sumY, double sumXY, double sumX2, double sumY2) GetRegressionConstants(List<DataPoint> dataPoints, DateTime genesisDate)
        {
            var deltaGB = new List<double>();
            var prices = new List<double>();

            if (dataPoints == null || dataPoints.Count == 0)
            {
                return (double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
            }

            var earliestDate = dataPoints.Min(dp => dp.Timestamp);
            if (genesisDate >= earliestDate)
            {
                genesisDate = earliestDate.AddDays(-1); // Adjust genesisDate to be 1 day before the earliest data date
            }

            foreach (var point in dataPoints)
            {
                double delta = (point.Timestamp - genesisDate).TotalDays;
                if (delta <= 0)
                {
                    delta = 1; // Set to 1 day if delta is zero or negative
                }
                deltaGB.Add(delta);
                prices.Add(point.Price);
            }

            if (deltaGB.Count == 0 || prices.Count == 0)
            {
                throw new InvalidOperationException("Empty deltaGB or prices array");
            }

            const double THRESHOLD = 1e-6;

            var logDeltaGB = deltaGB.Select((x, index) =>
            {
                return Math.Log(x);
            }).ToList();

            var logPrices = prices.Select((x, index) =>
            {
                return Math.Log(x);
            }).ToList();

            if (logDeltaGB.Count == 0 || logPrices.Count == 0)
            {
                throw new InvalidOperationException("Log values are invalid, cannot proceed with regression");
            }

            var regression = LinearRegression(logDeltaGB, logPrices);

            if (double.IsNaN(regression.slope) || double.IsNaN(regression.intercept))
            {
                throw new InvalidOperationException("Regression resulted in NaN values for slope or intercept");
            }

            return (exponent: regression.slope, fairPriceConstant: Math.Exp(regression.intercept), rSquared: regression.rSquared, sumX: regression.sumX, sumY: regression.sumY, sumXY: regression.sumXY, sumX2: regression.sumX2, sumY2: regression.sumY2);
        }

        private static (double slope, double intercept, double rSquared, double sumX, double sumY, double sumXY, double sumX2, double sumY2) LinearRegression(List<double> x, List<double> y)
        {
            int n = x.Count;
            if (n != y.Count)
            {
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
            }

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double intercept = (sumY - slope * sumX) / n;
            double rSquared = Math.Pow((n * sumXY - sumX * sumY) / Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY)), 2);

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

        public static (List<DateTime> dates, List<double> prices, List<double> supportPrices, List<double> resistancePrices, List<double> fairPrices, List<double> logDeltaGB, List<double> logPrices) PreparePowerLawUpperAndLowerBands(DateTime genesisDate, List<DataPoint> dataPoints, double exponent, double supportPriceConstant, double resistancePriceConstant, double fairPriceConstant, DateTime endDate)
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

        public static (string logText, string rValue, Dictionary<DateTime, double> prices, Dictionary<DateTime, double> supportPrices, Dictionary<DateTime, double> resistancePrices, Dictionary<DateTime, double> fairPrices) PreparePowerLawData(List<DataPoint> dataPoints, DateTime genesisDate, int extendLines)
        {
            string logText = "";
            string rValue = "";

            if (dataPoints == null || dataPoints.Count == 0)
            {
                return (logText, rValue, null, null, null, null);
            }

            var (exponent, fairPriceConstant, rSquared, sumX, sumY, sumXY, sumX2, sumY2) = DataManager.GetRegressionConstants(dataPoints, genesisDate);

            // Handle significant digits
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
                logText += $"Error extracting significant digits and exponent: {ex.Message}\n";
            }

            rValue = string.Format(
                "R² = {0:F4}{1}Exponent: {2:F4}{1}Fair Price Constant: {3}",
                rSquared,
                Environment.NewLine,
                exponent,
                formattedFairPriceConstant
            );

            logText += $"Regression Constants:\nExponent: {exponent}\nFair Price Constant: {fairPriceConstant}\n\n";

            DateTime latestDate = dataPoints.Max(dp => dp.Timestamp);
            DateTime endDate = latestDate.AddDays(extendLines);

            var (dates, prices, supportPrices, resistancePrices, fairPrices, logDeltaGB, logPrices) = DataManager.PreparePowerLawUpperAndLowerBands(
                genesisDate, dataPoints, exponent, fairPriceConstant * Math.Pow(10, -0.25), fairPriceConstant * Math.Pow(10, 0.25), fairPriceConstant, endDate);

            string report = GenerateReport(dataPoints, genesisDate, exponent, fairPriceConstant, rSquared, logDeltaGB, logPrices, sumX, sumY, sumXY, sumX2, sumY2);
            logText += report;

            var priceDict = dates.Zip(prices, (d, p) => new { d, p }).GroupBy(x => x.d).ToDictionary(g => g.Key, g => g.First().p);
            var supportPriceDict = dates.Zip(supportPrices, (d, sp) => new { d, sp }).GroupBy(x => x.d).ToDictionary(g => g.Key, g => g.First().sp);
            var resistancePriceDict = dates.Zip(resistancePrices, (d, rp) => new { d, rp }).GroupBy(x => x.d).ToDictionary(g => g.Key, g => g.First().rp);
            var fairPriceDict = dates.Zip(fairPrices, (d, fp) => new { d, fp }).GroupBy(x => x.d).ToDictionary(g => g.Key, g => g.First().fp);

            return (logText, rValue, priceDict, supportPriceDict, resistancePriceDict, fairPriceDict);
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


    }

    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Price { get; set; }
        public double Hashrate { get; set; }
    }
}
