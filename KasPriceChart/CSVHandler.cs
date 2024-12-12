using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KasPriceChart
{
    public static class CSVHandler
    {
        public static void ExportData(List<DataPoint> data, string filePath)
        {
            // Sort the data points by their timestamps before exporting
            data.Sort((dp1, dp2) => dp1.Timestamp.CompareTo(dp2.Timestamp));

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Timestamp,Price,Hashrate");
                foreach (var point in data)
                {
                    writer.WriteLine($"{point.Timestamp},{point.Price},{point.Hashrate}");
                }
            }
        }

        public static List<DataPoint> ImportData(string[] fileNames)
        {
            var allData = new List<DataPoint>();

            foreach (var fileName in fileNames)
            {
                try
                {
                    var lines = File.ReadAllLines(fileName).Skip(1); // Skip header
                    foreach (var line in lines)
                    {
                        var values = line.Split(',');
                        var dataPoint = new DataPoint
                        {
                            Timestamp = DateTime.Parse(values[0]),
                            Price = double.Parse(values[1]),
                            Hashrate = double.Parse(values[2])
                        };
                        allData.Add(dataPoint);
                    }
                }
                catch (FormatException)
                {
                    try { allData = ConvertAndImportCoinCodexCSV(fileName); break; } catch { }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing file {fileName} {ex.Message}");
                }
            }

            return allData;
        }

        public static bool MasterFileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public static void CreateMasterFile(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Timestamp,Price,Hashrate");
            }
        }
        public static List<DataPoint> ConvertAndImportCoinCodexCSV(string fileName)
        {
            var allData = new List<DataPoint>();
            var lines = File.ReadAllLines(fileName);

            // Parse header to find the relevant column indices
            var headers = lines.First().Split(',');
            int startDateIndex = Array.FindIndex(headers, header => header.ToLower().Contains("start"));
            int endDateIndex = Array.FindIndex(headers, header => header.ToLower().Contains("end"));
            int openPriceIndex = Array.FindIndex(headers, header => header.ToLower().Contains("open"));
            int closePriceIndex = Array.FindIndex(headers, header => header.ToLower().Contains("close"));

            // Skip header
            foreach (var line in lines.Skip(1))
            {
                var values = line.Split(',');

                var startDate = DateTime.Parse(values[startDateIndex]);
                var endDate = DateTime.Parse(values[endDateIndex]);
                var openPrice = double.Parse(values[openPriceIndex]);
                var closePrice = double.Parse(values[closePriceIndex]);

                // Add data point for start date at 8 AM with open price
                var startDataPoint = new DataPoint
                {
                    Timestamp = new DateTime(startDate.Year, startDate.Month, startDate.Day, 8, 0, 0),
                    Price = openPrice,
                    Hashrate = 0 // Placeholder value for Hashrate as it's not in the test CSV
                };
                allData.Add(startDataPoint);

                // Add data point for end date at 8 PM with close price
                var endDataPoint = new DataPoint
                {
                    Timestamp = new DateTime(endDate.Year, endDate.Month, endDate.Day, 20, 0, 0),
                    Price = closePrice,
                    Hashrate = 0 // Placeholder value for Hashrate as it's not in the test CSV
                };
                allData.Add(endDataPoint);
            }

            // Sort the data points by their timestamps
            allData.Sort((dp1, dp2) => dp1.Timestamp.CompareTo(dp2.Timestamp));

            return allData;
        }

    }
}
