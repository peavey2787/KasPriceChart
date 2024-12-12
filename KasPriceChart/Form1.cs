using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace KasPriceChart
{
    public partial class MainForm : Form
    {
        #region Variables
        private const int MIN_TIME_BETWEEN_API_CALLS = 3;
        private DataFetcher _dataFetcher;
        private DataManager _dataManager;
        private GraphPlotter _graphPlotter;
        private Timer _timer;
        private DateTime _lastFetchTime = DateTime.MinValue;
        private int _countdownTime;
        #endregion


        #region Initialize
        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            _dataFetcher = new DataFetcher();
            _dataManager = new DataManager();
            _graphPlotter = new GraphPlotter(chart1, chart2, lblCurrentPrice, lblCurrentHashrate, lblLastTimeStamp);

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;
            btnStart.Tag = "start";
            LoadLastFetchTime();

            toolTip1.SetToolTip(chkUseOnlyUploadedData, "When checked the chart will only use the imported data set, but BE WARNED: Keep this ckecked if you don't want it to overwrite your main data set (master.csv)");
            toolTip1.SetToolTip(chkCoinCodex, "When checked the app will use the specific code to extract \n any columns with 'open' in their name as the open price for the start date \n and 'close' for the close price for the end date from the imported .csv \n You can download free historical data from \n https://coincodex.com/crypto/kaspa/historical-data/");
            toolTip1.SetToolTip(btnImport, "Import custom data sets to merge with your current data set (master.csv) and continue adding real-time data or optionally just use the imported data");
            toolTip1.SetToolTip(btnExport, "Export your current data set (master.csv) to be used later");
            toolTip1.SetToolTip(lblInterval, "The time it takes between attempts to get new real-time data");
        }
        #endregion


        #region Start/Stop
        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadControlStates();

            string masterFilePath = "master.csv";

            if (CSVHandler.MasterFileExists(masterFilePath))
            {
                var dataPoints = CSVHandler.ImportData(new[] { masterFilePath });
                if (dataPoints.Count > 0)
                {
                    _dataManager.SetData(dataPoints);
                    var earliestDate = dataPoints.Min(dp => dp.Timestamp);
                    var latestDate = dataPoints.Max(dp => dp.Timestamp);
                    lblDataLoaded.Text = $"Previous data loaded from: {earliestDate} through {latestDate}";
                }
            }
            else
            {
                CSVHandler.CreateMasterFile(masterFilePath);
                lblDataLoaded.Text = "No data loaded, starting new dataset from right now";
            }

            // Auto start if enabled
            if (chkAutoStart.Checked)
            {
                btnStart.PerformClick();
            }
        }
        #endregion


        #region Timers
        private async void Timer_Tick(object sender, EventArgs e)
        {
            _countdownTime--;
            lblCountDown.Text = TimeSpan.FromSeconds(_countdownTime).ToString(@"mm\:ss");

            if (_countdownTime <= 0)
            {
                // Fetch data
                if(await FetchData())
                {
                    // Reset countdown time to the interval
                    _countdownTime = Int32.TryParse(txtInterval.Text, out int interval) ? interval * 60 : MIN_TIME_BETWEEN_API_CALLS;
                }                
            }
        }
        #endregion


        #region User Action Controls
        
        #region Button Clicks
        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (btnStart.Tag.ToString() == "start")
            {
                if (int.TryParse(txtInterval.Text, out int interval))
                {
                    _countdownTime = interval * 60; // Initialize countdown time in seconds
                    if (_timer.Enabled)
                    {
                        _timer.Stop();
                        _timer.Start();
                        btnStart.Tag = "stop";
                        btnStart.Text = "Stop";
                    }
                    else
                    {
                        _timer.Start();

                        btnStart.Tag = "stop";
                        btnStart.Text = "Stop";

                        // Fetch data
                        await FetchData();
                    }
                }
                else
                {
                    MessageBox.Show("Please enter a valid interval in minutes.", "Invalid Interval", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (btnStart.Tag.ToString() == "stop")
            {
                btnStart.Tag = "start";
                btnStart.Text = "Start";
                _timer.Stop();
                lblCountDown.Text = "";
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                List<DataPoint> uploadedData;

                if (chkCoinCodex.Checked)
                {
                    // Use method for CoinCodex format
                    uploadedData = CSVHandler.ConvertAndImportCoinCodexCSV(openFileDialog.FileName);
                }
                else
                {
                    // Use the original ImportData method
                    uploadedData = CSVHandler.ImportData(openFileDialog.FileNames);
                }

                bool useOnlyUploaded = chkUseOnlyUploadedData.Checked;
                if (useOnlyUploaded)
                {
                    _dataManager.SetData(uploadedData);
                }
                else
                {
                    _dataManager.MergeData(uploadedData);
                    SaveData();
                }

                _graphPlotter.UpdateGraph(_dataManager.GetData());
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = "data.csv"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var data = _dataManager.GetData();
                CSVHandler.ExportData(data, saveFileDialog.FileName);
                MessageBox.Show("Data exported successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion        

        #region Checkboxes
        private void chkUseOnlyUploadedData_CheckedChanged(object sender, EventArgs e)
        {
            SaveControlStates();
        }

        private void chkCoinCodex_CheckedChanged(object sender, EventArgs e)
        {
            SaveControlStates();
        }

        private void chkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            SaveControlStates();
        }
        #endregion

        private void txtInterval_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(txtInterval.Text, out int interval))
            {
                _countdownTime = interval * 60; // Update countdown time in seconds
                if (_timer.Enabled)
                {
                    _timer.Stop();
                    _timer.Start();
                }
                SaveControlStates();
            }
            else
            {
                MessageBox.Show("Please enter a valid interval in minutes.", "Invalid Interval", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion


        #region Helpers
        private async Task<bool> FetchData()
        {
            bool success = false;
            TimeSpan timeSinceLastFetch = DateTime.Now - _lastFetchTime;
            var minValue = int.TryParse(txtInterval.Text, out int parsedInterval) && parsedInterval >= MIN_TIME_BETWEEN_API_CALLS ? parsedInterval : MIN_TIME_BETWEEN_API_CALLS;
            if (timeSinceLastFetch.TotalMinutes >= minValue)
            {
                // Fetch data
                var priceData = await _dataFetcher.FetchPriceData();
                var hashrateData = await _dataFetcher.FetchHashrateData();
                _dataManager.AddData(DateTime.Now, priceData, hashrateData);
                _lastFetchTime = DateTime.Now; // Update the last fetch time
                AppSettings.Save("LastFetchTime", _lastFetchTime.ToString("o"));

                if (!chkUseOnlyUploadedData.Checked)
                {
                    // Save updated data to master.csv
                    SaveData();
                }
                success = true;
            }
            else
            {
                // Calculate remaining seconds until min. minutes have passed
                double remainingSeconds = (minValue * 60) - timeSinceLastFetch.TotalSeconds;
                _countdownTime = (int)Math.Ceiling(remainingSeconds);
            }

            _graphPlotter.UpdateGraph(_dataManager.GetData());

            return success;
        }
        private void SaveData()
        {
            // Save updated data to master.csv
            var data = _dataManager.GetData();
            CSVHandler.ExportData(data, "master.csv");
        }
        private void SaveControlStates()
        {
            AppSettings.Save("UpdateInterval", txtInterval.Text);
            AppSettings.Save("UseOnlyUploadedData", chkUseOnlyUploadedData.Checked);
            AppSettings.Save("AutoStart", chkAutoStart.Checked);
        }
        private void LoadControlStates()
        {
            var updateInterval = AppSettings.Load<string>("UpdateInterval");
            if (!string.IsNullOrEmpty(updateInterval))
            {
                txtInterval.Text = updateInterval;
            }

            var useOnlyUploadedData = AppSettings.Load<bool>("UseOnlyUploadedData");
            chkUseOnlyUploadedData.Checked = useOnlyUploadedData;

            var someOtherCheckbox = AppSettings.Load<bool>("AutoStart");
            chkAutoStart.Checked = someOtherCheckbox;
        }
        private void LoadLastFetchTime()
        {
            var lastFetchTimeString = AppSettings.Load<string>("LastFetchTime");
            if (!string.IsNullOrEmpty(lastFetchTimeString))
            {
                DateTime.TryParse(lastFetchTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind, out _lastFetchTime);
            }
        }

        #endregion


    }
}
