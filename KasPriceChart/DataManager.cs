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
    }

    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Price { get; set; }
        public double Hashrate { get; set; }
    }
}
