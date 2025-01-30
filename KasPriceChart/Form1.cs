using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace KasPriceChart
{
    public partial class MainForm : Form
    {
        #region Variables
        private const int MIN_TIME_BETWEEN_API_CALLS = 3;
        private const int EXT_CHART_BUTTON_X = 10;
        private const int EXT_CHART_BUTTON_Y = 20;
        private DataFetcher _dataFetcher;
        private DataManager _dataManager;
        private GraphPlotter _graphPlotter;
        private Timer _timer;
        private DateTime _lastFetchTime = DateTime.MinValue;
        private DateTime _ogStartTime;
        private DateTime _ogEndTime;
        private int _countdownTime;
        private bool appInControl = false;
        private bool _fetchingData = false;
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
            _graphPlotter = new GraphPlotter(chart1, chart2);

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;
            btnStart.Tag = "start";
            LoadLastFetchTime();

            toolTip1.SetToolTip(chkUseOnlyUploadedData, "When checked the chart will only use the imported data set, but BE WARNED: Keep this ckecked if you don't want it to overwrite your main data set (master.csv)");
            toolTip1.SetToolTip(btnImport, "Import custom data sets to merge with your current data set (master.csv) and continue adding real-time data or optionally just use the imported data");
            toolTip1.SetToolTip(btnExport, "Export your current data set (master.csv) to be used later (The data points exported depend on the drop down value selected)");
            toolTip1.SetToolTip(lblInterval, "The time it takes between attempts to get new real-time data");
        }
        #endregion


        #region Start/Stop
        private void MainForm_Load(object sender, EventArgs e)
        {
            appInControl = true;

            string masterFilePath = Path.Combine(Directory.GetCurrentDirectory(), "master.csv");

            if (CSVHandler.MasterFileExists(masterFilePath))
            {
                var dataPoints = CSVHandler.ImportData(new[] { masterFilePath });
                if (dataPoints.Count > 0)
                {
                    _dataManager.SetData(dataPoints);
                    SetDatePickers(dataPoints);
                }
            }
            else
            {
                CSVHandler.CreateMasterFile(masterFilePath);
                lblDataLoaded.Text = "No data loaded, starting new dataset from right now";
            }

            // Reload last saved state for controls

            LoadControlStates();

            // Auto start if enabled
            if (chkAutoStart.Checked)
            {
                btnStart.PerformClick();
            }
            ShowTheChart();
            //ShowTheHashrateChart(chkPowerLawLines.Checked, chkLogLinear.Checked);

            // Set the location of btnShowSettingsBox
            btnShowSettingsBox.Location = new Point(EXT_CHART_BUTTON_X, EXT_CHART_BUTTON_Y);

            appInControl = false;
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
                if (await FetchData())
                {
                    // Reset countdown time to the interval
                    _countdownTime = int.TryParse(txtInterval.Text, out int interval) && (interval * 60 > MIN_TIME_BETWEEN_API_CALLS) ? interval * 60 : MIN_TIME_BETWEEN_API_CALLS;
                }
                else { _countdownTime = 5; }
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
                List<DataPoint> uploadedDataPoints;

                uploadedDataPoints = CSVHandler.ImportData(openFileDialog.FileNames);

                if (uploadedDataPoints.Count > 0)
                {
                    bool useOnlyUploaded = chkUseOnlyUploadedData.Checked;
                    if (useOnlyUploaded)
                    {
                        _dataManager.SetData(uploadedDataPoints);
                    }
                    else
                    {
                        _dataManager.MergeData(uploadedDataPoints);
                        SaveData();
                    }

                    SetDatePickers(uploadedDataPoints);
                }

                // Select All Data 
                cmbViewTimspan.SelectedIndex = 0;

                ShowTheChart();
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
                string selectedTimespan = cmbViewTimspan.SelectedItem?.ToString() ?? "All Data";
                List<DataPoint> dataPoints = DataManager.FilterDataPointsForView(_dataManager.GetData(), selectedTimespan);
                CSVHandler.ExportData(dataPoints, saveFileDialog.FileName);
                MessageBox.Show("Data exported successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnShowMore_Click(object sender, EventArgs e)
        {
            if (!appInControl)
            {
                if (btnShowMore.Text == "Show More")
                {
                    richTextBoxLog.Visible = true;
                    btnShowMore.Text = "Show Less";
                }
                else if (btnShowMore.Text == "Show Less")
                {
                    richTextBoxLog.Visible = false;
                    btnShowMore.Text = "Show More";
                }
                SaveControlStates();
            }
        }

        private void btnResetZoom_Click(object sender, EventArgs e)
        {
            _graphPlotter.ResetZoom();
        }

        private async void btnResetDates_Click(object sender, EventArgs e)
        {
            appInControl = true;
            dateTimePickerStart.Value = _ogStartTime;
            dateTimePickerEnd.Value = _ogEndTime;
            appInControl = false;

            await FetchData();
        }

        private void btnShowSettingsBox_Click(object sender, EventArgs e)
        {
            ToggleSettingsBox();

            if (!appInControl)
            {
                SaveControlStates();
            }

        }
        #endregion        


        #region Checkboxes
        private void chkUseOnlyUploadedData_CheckedChanged(object sender, EventArgs e)
        {
            if (!appInControl)
            {
                SaveControlStates();
            }
        }

        private void chkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            if (!appInControl)
            {
                SaveControlStates();
            }
        }

        private void chkPowerLawLines_CheckedChanged(object sender, EventArgs e)
        {
            if (!appInControl)
            {
                SaveControlStates();
                chkLogLinear.Checked = chkPowerLawLines.Checked;
                btnShowMore.Visible = chkPowerLawLines.Checked;
                lblRValue.Visible = chkPowerLawLines.Checked;
                ShowTheChart();
                _graphPlotter.ResetZoom();
            }
            else
            {
                chkLogLinear.Checked = true;
                btnShowMore.Visible = chkPowerLawLines.Checked;
                lblRValue.Visible = chkPowerLawLines.Checked;
                ShowTheChart();
                _graphPlotter.ResetZoom();
            }
        }

        private void chkLogLinear_CheckedChanged(object sender, EventArgs e)
        {
            if (!appInControl)
            {
                SaveControlStates();
                ShowTheChart();
                _graphPlotter.ResetZoom();
            }
        }
        #endregion


        #region Textboxes
        private void txtInterval_TextChanged(object sender, EventArgs e)
        {
            if (!appInControl)
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
        }

        private void txtExtendLines_TextChanged(object sender, EventArgs e)
        {
            if (!appInControl)
            {
                string input = txtExtendLines.Text.Trim();  // Get and trim any extra spaces from the input

                // Check if the input is empty
                if (string.IsNullOrEmpty(input))
                {
                    // Optionally, set the TextBox to a default value or handle as needed
                    txtExtendLines.BackColor = Color.White; // Reset the background color if input is empty
                    return;
                }

                // Check if the input is a valid non-negative integer
                if (int.TryParse(input, out int result))
                {
                    // Ensure the value is non-negative
                    if (result < 0)
                    {
                        txtExtendLines.BackColor = Color.LightPink;  // Highlight the textbox for invalid input
                        MessageBox.Show("'Extend Lines' cannot be negative. Please enter a non-negative value.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    else
                    {
                        txtExtendLines.BackColor = Color.White; // Reset the background color if input is valid
                    }
                }
                else
                {
                    // If the input is not a valid integer, highlight the textbox and show an error
                    txtExtendLines.BackColor = Color.LightPink;  // Highlight the textbox for invalid input
                    MessageBox.Show("Invalid input. Please enter a valid non-negative integer value for 'Extend Lines'.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                ShowTheChart();

                SaveControlStates();
            }
        }
        #endregion


        #region Combobox/Drop down menu
        private async void cmbViewTimspan_TextUpdate(object sender, EventArgs e)
        {
            if (!appInControl)
            {
                await FetchData();
                SaveControlStates();
            }
        }

        private async void cmbViewTimspan_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!appInControl)
            {
                await FetchData();
                SaveControlStates();
            }
        }
        #endregion


        #region Trackbars
        private void trackBarDataPointSize_ValueChanged(object sender, EventArgs e)
        {
            if (!appInControl)
            {
                // Get the new size from the TrackBar
                int newSize = trackBarDataPointSize.Value;

                _graphPlotter.ChangeDataPointSize(newSize);

                SaveControlStates();
            }
        }
        private void trackBarZoomSpeed_ValueChanged(object sender, EventArgs e)
        {
            if (!appInControl)
            {
                double zoomSpeed = GetZoomLevel(trackBarZoomSpeed.Value);
                _graphPlotter.ChangeZoomSpeed(zoomSpeed);
                SaveControlStates();
            }
        }

        #endregion

        #region DateTimePickers
        private async void dateTimePickerStart_ValueChanged(object sender, EventArgs e)
        {
            if (!appInControl)
            {
                await FetchData();
            }
        }

        private async void dateTimePickerEnd_ValueChanged(object sender, EventArgs e)
        {
            if (!appInControl)
            {
                await FetchData();
            }
        }
        #endregion

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPageHashrate)
            {
                // Move the button to the second tab page
                tabPageHashrate.Controls.Add(btnShowSettingsBox);
                btnShowSettingsBox.Location = new Point(EXT_CHART_BUTTON_X, EXT_CHART_BUTTON_Y);
                btnShowSettingsBox.BringToFront();

                // Move chart settings
                tabPageHashrate.Controls.Add(groupBoxSettings);
                groupBoxSettings.BringToFront();

                // Move r2 value info
                tabPageHashrate.Controls.Add(lblRValue);
                lblRValue.BringToFront();

                // Move show more button
                tabPageHashrate.Controls.Add(btnShowMore);
                btnShowMore.BringToFront();
            }
            else if (tabControl1.SelectedTab == tabPagePrice)
            {
                // Move the button to the first tab page
                tabPagePrice.Controls.Add(btnShowSettingsBox);
                btnShowSettingsBox.Location = new Point(EXT_CHART_BUTTON_X, EXT_CHART_BUTTON_Y);
                btnShowSettingsBox.BringToFront();

                // Move chart settings
                tabPagePrice.Controls.Add(groupBoxSettings);
                groupBoxSettings.BringToFront();

                // Move r2 value info
                tabPagePrice.Controls.Add(lblRValue);
                lblRValue.BringToFront();

                // Move show more button
                tabPagePrice.Controls.Add(btnShowMore);
                btnShowMore.BringToFront();
            }

            ShowTheChart();
        }
        #endregion


        #region Helpers

        private void SetDatePickers(List<DataPoint> dataPoints)
        {
            var earliestDate = dataPoints.Min(dp => dp.Timestamp);
            var latestDate = dataPoints.Max(dp => dp.Timestamp);
            dateTimePickerStart.Value = earliestDate;
            dateTimePickerEnd.Value = latestDate;
            _ogStartTime = earliestDate;
            _ogEndTime = latestDate;
            lblDataLoaded.Text = $"Data loaded from: {earliestDate}  through  {latestDate}";
        }

        private void ToggleSettingsBox()
        {
            if (btnShowSettingsBox.Text == "<")
            {
                // Show less
                groupBoxSettings.Visible = false;
                btnShowSettingsBox.Text = ">";
            }
            else if (btnShowSettingsBox.Text == ">")
            {
                // Show more
                groupBoxSettings.Visible = true;
                groupBoxSettings.BringToFront();
                btnShowSettingsBox.Text = "<";
            }
        }

        private async Task<bool> FetchData()
        {
            if (!_fetchingData)
            {
                _fetchingData = true;
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

                ShowTheChart();
                _fetchingData = false;
                return success;
            }
            return false;
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
            AppSettings.Save("UsePowerLaw", chkPowerLawLines.Checked);
            AppSettings.Save("ShowOrHideSettings", btnShowSettingsBox.Text);
            AppSettings.Save("LogOrLinear", chkLogLinear.Checked);
            AppSettings.Save("ExtendLinesByDays", txtExtendLines.Text);
            AppSettings.Save("DataPointSize", trackBarDataPointSize.Value);
            AppSettings.Save("ZoomSpeed", trackBarZoomSpeed.Value);
            AppSettings.Save("TimeSpanView", cmbViewTimspan.Text);
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

            var autoStart = AppSettings.Load<bool>("AutoStart");
            chkAutoStart.Checked = autoStart;

            var UsePowerLaw = AppSettings.Load<bool>("UsePowerLaw");
            chkPowerLawLines.Checked = UsePowerLaw;

            var logOrLinear = AppSettings.Load<bool>("LogOrLinear");
            chkLogLinear.Checked = logOrLinear;

            var extendLinesByDays = AppSettings.Load<string>("ExtendLinesByDays");
            if (!string.IsNullOrEmpty(extendLinesByDays))
            {
                txtExtendLines.Text = extendLinesByDays;
            }

            var dataPointSize = AppSettings.Load<string>("DataPointSize");
            if (!string.IsNullOrEmpty(dataPointSize))
            {
                trackBarDataPointSize.Value = int.TryParse(dataPointSize, out int parsedDPSize) ? parsedDPSize : 10;
                _graphPlotter.ChangeDataPointSize(parsedDPSize);
            }

            var zoomSpeed = AppSettings.Load<int>("ZoomSpeed");
            if (zoomSpeed == 0) { zoomSpeed = 3; }            
            trackBarZoomSpeed.Value = zoomSpeed;
            _graphPlotter.ChangeZoomSpeed(zoomSpeed);
            

            var timespanView = AppSettings.Load<string>("TimeSpanView");
            if (!string.IsNullOrEmpty(timespanView))
            {
                cmbViewTimspan.Text = timespanView;
            }

            var showOrHideSettings = AppSettings.Load<string>("ShowOrHideSettings");

            // If different than default, click the button
            if (!string.IsNullOrEmpty(showOrHideSettings) && showOrHideSettings == ">")
            {
                ToggleSettingsBox();
            }

        }

        private void LoadLastFetchTime()
        {
            var lastFetchTimeString = AppSettings.Load<string>("LastFetchTime");
            if (!string.IsNullOrEmpty(lastFetchTimeString))
            {
                DateTime.TryParse(lastFetchTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind, out _lastFetchTime);
            }
        }

        private void ShowTheChart()
        {
            if (tabControl1.SelectedIndex == 0)
            {
                ShowThePriceChart(chkPowerLawLines.Checked, chkLogLinear.Checked);
            }
            else if (tabControl1.SelectedIndex == 1)
            {
                ShowTheHashrateChart(chkPowerLawLines.Checked, chkLogLinear.Checked);
            }
        }

        private async void ShowThePriceChart(bool showPowerLawLines = false, bool logOrLinear = false)
        {
            DateTime startDate;
            DateTime endDate;

            if (dateTimePickerStart.InvokeRequired)
            {
                startDate = (DateTime)dateTimePickerStart.Invoke(new Func<DateTime>(() => dateTimePickerStart.Value));
            }
            else
            {
                startDate = dateTimePickerStart.Value;
            }

            if (dateTimePickerEnd.InvokeRequired)
            {
                endDate = (DateTime)dateTimePickerEnd.Invoke(new Func<DateTime>(() => dateTimePickerEnd.Value));
            }
            else
            {
                endDate = dateTimePickerEnd.Value;
            }

            // Update the time with the current time to get real-time data
            endDate = endDate.Date + DateTime.Now.TimeOfDay;

            List<DataPoint> dataPoints = _dataManager.GetData();

            // Get latest data and update labels
            var latestData = DataManager.GetLatestData(dataPoints);
            UpdateCurrentLabels(latestData.latestPrice, latestData.latestHashrate, latestData.latestTimestamp);

            // Filter data points for selected view
            string selectedTimespan = cmbViewTimspan.SelectedItem?.ToString() ?? "All Data";
            dataPoints = DataManager.FilterDataPointsForView(_dataManager.GetData(), selectedTimespan);
            dataPoints = DataManager.FilterDataPointsByDateRange(dataPoints, startDate, endDate);

            // Update the chart
            if (showPowerLawLines)
            {
                var genesisDate = new DateTime(2021, 11, 7);
                var powerLawData = await Task.Run(() => DataManager.PreparePowerLawData(dataPoints, genesisDate, GetExtendLinesValue()));
                _graphPlotter.UpdatePriceWithPowerLaw(dataPoints, logOrLinear, GetExtendLinesValue(), powerLawData.supportPrices, powerLawData.resistancePrices, powerLawData.fairPrices);

                richTextBoxLog.Clear();
                richTextBoxLog.Text = powerLawData.logText;

                lblRValue.Text = powerLawData.rValue;
            }
            else
            {
                _graphPlotter.UpdatePriceChart(dataPoints, logOrLinear);
            }
        }

        private async void ShowTheHashrateChart(bool showPowerLawLines = false, bool logOrLinear = false)
        {
            // Filter data points for selected view
            string selectedTimespan = cmbViewTimspan.SelectedItem?.ToString() ?? "All Data";
            List<DataPoint> dataPoints = DataManager.FilterDataPointsForView(_dataManager.GetData(), selectedTimespan);

            // Get latest data and update labels
            var latestData = DataManager.GetLatestData(dataPoints);
            UpdateCurrentLabels(latestData.latestPrice, latestData.latestHashrate, latestData.latestTimestamp);

            // Update the chart
            if (showPowerLawLines)
            {
                var genesisDate = new DateTime(2021, 11, 7);
                var powerLawData = await Task.Run(() => DataManager.PreparePowerLawData(dataPoints, genesisDate, GetExtendLinesValue(), false));
                _graphPlotter.UpdateHashrateWithPowerLaw(dataPoints, logOrLinear, GetExtendLinesValue(), powerLawData.supportPrices, powerLawData.resistancePrices, powerLawData.fairPrices);
            }
            else
            {
                _graphPlotter.UpdateHashrateChart(dataPoints, logOrLinear);
            }
        }

        private void UpdateCurrentLabels(double latestPrice, double latestHashrate, DateTime latestTimestamp)
        {
            if (latestPrice > 0)
            {
                lblCurrentPrice.Text = $"Price: ${latestPrice:F4}";
            }
            else
            {
                lblCurrentPrice.Text = "Error Fetching Price";
            }
            if (latestHashrate > 0)
            {
                lblCurrentHashrate.Text = $"Hashrate: {GraphPlotter.FormatHashrateGetNumber(latestHashrate)} {GraphPlotter.FormatHashrateGetLabel(latestHashrate)}";
            }
            else
            {
                lblCurrentHashrate.Text = "Error Fetching Hashrate";
            }

            lblLastTimeStamp.Text = $"Last Update: {latestTimestamp:MMM dd-yy hh:mm:ss tt}";
        }

        private int GetExtendLinesValue()
        {
            int extendLines = 365;  // Default value

            try
            {
                // Ensure that the TextBox value is not empty
                string input = txtExtendLines.Text.Trim();

                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("Please enter a value in the 'Extend Lines' field.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return extendLines;  // Return the default value
                }

                // Try to parse the value to an integer
                if (!int.TryParse(input, out extendLines))
                {
                    MessageBox.Show("Invalid input. Please enter a valid integer value for 'Extend Lines'.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return extendLines;  // Return the default value
                }

                // Optional: You can add further validation (e.g., range checks)
                if (extendLines < 0)
                {
                    MessageBox.Show("'Extend Lines' cannot be negative. Please enter a positive value.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return extendLines;
                }

            }
            catch (Exception ex)
            {
                // Catch any unexpected errors and show an error message
                MessageBox.Show($"An error occurred while retrieving the value: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return extendLines;
        }

        public double GetZoomLevel(int value)
        {
            switch (value)
            {
                case 1:
                    return 0.9;
                case 2:
                    return 0.7;
                case 3:
                    return 0.5;
                case 4:
                    return 0.3;
                case 5:
                    return 0.1;
                default:
                    return 0.5; // Default value in case of unexpected values
            }
        }









        #endregion


    }
}
