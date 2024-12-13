using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    }

    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Price { get; set; }
        public double Hashrate { get; set; }
    }
}
