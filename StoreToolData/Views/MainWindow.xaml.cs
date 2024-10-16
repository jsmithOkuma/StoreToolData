using System;
using System.Text;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Win32;
using System.Windows.Threading;

//1.21.1
using Okuma.CMDATAPI.DataAPI;

namespace StoreToolData
{
    public partial class MainWindow : Window
    {
        CTools cTools;

        List<Tool> toolsList;
        string sqlServerConnectionString;
        string sqliteDatabasePath;
        bool sqlServerConnected;
        bool waitingToPoll;

        private TimeSpan _pollingInterval;
        private DispatcherTimer _pollingTimer;

        IDatabaseOperations dbOperations;

        Log log;

        public MainWindow()
        {
            InitializeComponent();
            log = new Log();
            log.AddLogMessage("Initializing component");
            SplashScreen splashScreen = new SplashScreen(@"Images\splash.png");
            splashScreen.Show(true);

            try
            {
                (new CMachine()).Init();

                cTools = new CTools();

                toolsList = new List<Tool>();
                sqlServerConnectionString = "";
                sqliteDatabasePath = "";

            } catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                log.AddLogMessage(ex.ToString());
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Properties.Settings.Default.Reset();
            //Properties.Settings.Default.Save();

            string sqlSelection = Properties.Settings.Default.SQLSelection;
            if (sqlSelection == "SQLite")
            {
                rbSqlite.IsChecked = true;
            } else if (sqlSelection == "SQLServer")
            {
                rbSqlServer.IsChecked = true;
            }

            if (Properties.Settings.Default.autoPoll)
            {
                cbxAutoPoll.IsChecked = true;
            } else
            {
                cbxAutoPoll.IsChecked = false;
            }

            ColumnSettings settings = LoadColumnSettings();

            if (settings.ToolNumberVisible) { chkToolNumber.IsChecked = true; }
            if (settings.PotNumVisible) { chkPotNum.IsChecked = true; }
            if (settings.ToolLifeVisible) { chkToolLife.IsChecked = true; }
            if (settings.ToolLifeRemainingVisible) { chkToolLifeRem.IsChecked = true; }
            if (settings.ToolLengthOffsetVisible) { chkToolLengthOffset.IsChecked = true; }
            if (settings.ToolIsAttachedVisible) { chkIsAttached.IsChecked = true; }
            if (settings.ToolNameVisible) { chkToolName.IsChecked = true; }

            txtPollingValue.Text = Properties.Settings.Default.PollingValue.ToString();

            foreach (ComboBoxItem item in PollingUnitComboBox.Items)
            {
                if (item.Content.ToString() == Properties.Settings.Default.PollingUnit)
                {
                    PollingUnitComboBox.SelectedItem = item;
                    break;
                }
            }

            if (settings != null && cbxAutoPoll.IsChecked == true)
            {
                btnConnect_Click(null, null);
                btnUpdateColumnSettings_Click(null, null);
                btnStartPolling_Click(null, null);
            }

            if (!isDatabaseConnected())
            {
                btnUpdateColumnSettings.IsEnabled = false;
                btnGetToolData.IsEnabled = false;
                //btnStartPolling_Click(null, null);
            }
        }

        /////////////////////////////
        /////////// EVENTS /////////
        ///////////////////////////

        private void DatabaseTypeChanged(object sender, RoutedEventArgs e)
        {
            if (rbSqlite.IsChecked == true)
            {
                if (Properties.Settings.Default.SQLiteFileLocation != "")
                {
                    txtSqliteFilePath.Text = Properties.Settings.Default.SQLiteFileLocation;
                    sqliteDatabasePath = Properties.Settings.Default.SQLiteFileLocation;
                    btnUpdateColumnSettings.IsEnabled = true;
                    btnGetToolData.IsEnabled = true;
                    string connectionString = "Data Source=" + sqliteDatabasePath + ";Version=3;New=False;";
                    dbOperations = new SQLiteOperations(connectionString);
                    UpdateListViewColumns(LoadColumnSettings());
                    Properties.Settings.Default.SQLSelection = "SQLite";
                    Properties.Settings.Default.isMsAuth = true;
                    Properties.Settings.Default.Save();
                }
            }
            else if (rbSqlServer.IsChecked == true)
            {
                if (Properties.Settings.Default.serverName != null)
                {
                    txtServerName.Text = Properties.Settings.Default.serverName;
                    txtInstanceName.Text = Properties.Settings.Default.instanceName;
                    txtDatabaseName.Text = Properties.Settings.Default.databaseName;
                    txtPort.Text = Properties.Settings.Default.port;
                    btnUpdateColumnSettings.IsEnabled = false;
                    Properties.Settings.Default.SQLSelection = "SQLServer";
                    if (Properties.Settings.Default.isMsAuth)
                    {
                        rbSQLAuth.IsChecked = true;
                        txtUsername.Text = Properties.Settings.Default.username;
                        txtPassword.Password = Properties.Settings.Default.password;
                    }
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void btnGetToolData_Click(object sender, RoutedEventArgs e)
        {
            getTools();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            string serverName = txtServerName.Text;
            string instanceName = txtInstanceName.Text;
            string databaseName = txtDatabaseName.Text;

            string username = txtUsername.Text;
            string password = txtPassword.Password;

            string port = txtPort.Text;

            Properties.Settings.Default.serverName = serverName;
            Properties.Settings.Default.instanceName = instanceName;
            Properties.Settings.Default.databaseName = databaseName;
            Properties.Settings.Default.username = username;
            Properties.Settings.Default.password = password;
            Properties.Settings.Default.port = port;

            StringBuilder builder = new StringBuilder();
            builder.Append("Server=");
            builder.Append(serverName);

            if (!string.IsNullOrEmpty(instanceName))
            {
                builder.Append("\\");
                builder.Append(instanceName);
            }

            if (!string.IsNullOrEmpty(port))
            {
                if (Int32.TryParse(port, out int portNumber) && portNumber != 1433)
                {
                    builder.Append(",");
                    builder.Append(portNumber);
                }
            }
            builder.Append(";");

            builder.Append("Database=");
            builder.Append(databaseName);
            builder.Append(";");

            if (rbWindowsAuth.IsChecked == true)
            {
                builder.Append("Trusted_Connection=True;");
                Properties.Settings.Default.isMsAuth = false;
            }
            else 
            {
                builder.Append("User Id=");
                builder.Append(username);
                builder.Append(";");

                builder.Append("Password=");
                builder.Append(password);
                builder.Append(";");

                Properties.Settings.Default.isMsAuth = true;
            }
            Properties.Settings.Default.Save();

            sqlServerConnectionString = builder.ToString();

            dbOperations = new SQLServerOperations(sqlServerConnectionString);
            if (dbOperations.TestConnection())
            {
                lblConnectionSucessful.Content = "Connected";
                sqlServerConnected = true;
                dbOperations.CheckAndCreateTable();
                btnUpdateColumnSettings.IsEnabled = true;
                btnGetToolData.IsEnabled = true;
                if (chkToolNumber.IsChecked == true || chkToolName.IsChecked == true|| chkToolLifeRem.IsChecked == true ||chkToolLife.IsChecked == true
                    || chkToolLengthOffset.IsChecked == true || chkPotNum.IsChecked == true || chkIsAttached.IsChecked == true)
                {
                    if (waitingToPoll)
                    {
                        btnStartPolling_Click(null, null);
                    }
                }
            }
            else
            {
                lblConnectionSucessful.Content = "Not Connected";
                MessageBox.Show("There was a problem connecting to the server.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnUpdateColumnSettings_Click(object sender, RoutedEventArgs e)
        {
            ColumnSettings settings = new ColumnSettings
            {
                ToolNumberVisible = chkToolNumber.IsChecked.Value,
                PotNumVisible = chkPotNum.IsChecked.Value,
                ToolLifeVisible = chkToolLife.IsChecked.Value,
                ToolLifeRemainingVisible = chkToolLifeRem.IsChecked.Value,
                ToolNameVisible = chkToolName.IsChecked.Value,
                ToolLengthOffsetVisible = chkToolLengthOffset.IsChecked.Value,
                ToolIsAttachedVisible = chkIsAttached.IsChecked.Value
            };

            dbOperations.CheckAndCreateTable();

            UpdateListViewColumns(settings);

            dbOperations.UpdateTableStructure(settings);
            SaveColumnSettings(settings);

            if (waitingToPoll)
            {
                btnStartPolling_Click(null, null);
            }
        }

        private void btnDirectoryBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "SQLite Files (*.sqlite)|*.sqlite|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                txtSqliteFilePath.Text = openFileDialog.FileName;
            }
        }

        private void btnSqliteSave_Click(object sender, RoutedEventArgs e)
        {
            sqliteDatabasePath = txtSqliteFilePath.Text;
            Properties.Settings.Default.SQLiteFileLocation = sqliteDatabasePath;
            Properties.Settings.Default.Save();

            string connectionString = "Data Source=" + sqliteDatabasePath + ";Version=3;New=False;";

            dbOperations = new SQLiteOperations(connectionString);
            btnUpdateColumnSettings.IsEnabled = true;
            btnGetToolData.IsEnabled = true;
            waitingToPoll = true;
        }

        private void txtSqliteFilePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnSqliteSave.IsEnabled = true;
        }

        private void btnStartPolling_Click(object sender, RoutedEventArgs e)
        { 
            if (isDatabaseConnected())
            {
                if (int.TryParse(txtPollingValue.Text, out int pollingValue))
                {
                    switch (PollingUnitComboBox.SelectedItem)
                    {
                        case ComboBoxItem item when item.Content.ToString() == "Seconds":
                            _pollingInterval = TimeSpan.FromSeconds(Math.Max(pollingValue, 1));
                            break;
                        case ComboBoxItem item when item.Content.ToString() == "Minutes":
                            _pollingInterval = TimeSpan.FromMinutes(pollingValue);
                            break;
                        case ComboBoxItem item when item.Content.ToString() == "Hours":
                            _pollingInterval = TimeSpan.FromHours(pollingValue);
                            break;
                    }

                    Properties.Settings.Default.PollingValue = pollingValue;
                    Properties.Settings.Default.PollingUnit = ((ComboBoxItem)PollingUnitComboBox.SelectedItem).Content.ToString();
                    Properties.Settings.Default.Save();

                    if (_pollingInterval.TotalMilliseconds < 1000)
                    {
                        MessageBox.Show("Please set the polling frequency to at least 1 second.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (_pollingTimer == null)
                    {
                        _pollingTimer = new DispatcherTimer();
                        _pollingTimer.Tick += PollData;
                    }

                    _pollingTimer.Interval = _pollingInterval;
                    _pollingTimer.Start();
                    btnStartPolling.IsEnabled = false;
                    btnStopPolling.IsEnabled = true;
                }
            }
        }

        private void PollingValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int.TryParse(txtPollingValue.Text, out int pollingValue);
                if (pollingValue != 0)
                {
                    btnStartPolling.IsEnabled = true;
                }
                else
                {
                    btnStartPolling.IsEnabled = false;
                }
            } catch (Exception ex)
            {
                MessageBox.Show("Please enter a numerical value", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnStopPolling_Click(object sender, RoutedEventArgs e)
        {
            _pollingTimer.Stop();
            int.TryParse(txtPollingValue.Text, out int pollingValue);
            if (pollingValue != 0)
            {
                btnStartPolling.IsEnabled = true;
            }
            btnStopPolling.IsEnabled = false;
        }

        private void PollData(object sender, EventArgs e)
        {
            getTools();

        }

        ////////////////////////////////
        /////////// Utilities /////////
        //////////////////////////////

        private bool isDatabaseConnected()
        {
            if (rbSqlServer.IsChecked == true && !sqlServerConnected)
            {
                return false;
            }
            else if (rbSqlite.IsChecked == true && txtSqliteFilePath.Text == "")
            {
                return false;
            }
            else if (rbSqlServer.IsChecked == false && rbSqlite.IsChecked == false)
            {
                return false;
            }

            return true;
        }

        private ColumnSettings LoadColumnSettings()
        {
            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ColumnSettings))
                return new ColumnSettings();

            var serializer = new XmlSerializer(typeof(ColumnSettings));
            using (var stringReader = new StringReader(Properties.Settings.Default.ColumnSettings))
            {
                return (ColumnSettings)serializer.Deserialize(stringReader);
            }
        }

        private void SaveColumnSettings(ColumnSettings settings)
        {
            var serializer = new XmlSerializer(typeof(ColumnSettings));
            using (var stringWriter = new StringWriter())
            {
                serializer.Serialize(stringWriter, settings);
                Properties.Settings.Default.ColumnSettings = stringWriter.ToString();
                Properties.Settings.Default.Save();
            }
        }

        private void UpdateListViewColumns(ColumnSettings settings)
        {
            GridView gridView = (GridView)lvTools.View;

            gridView.Columns.Clear();

            if (settings.ToolNumberVisible)
            {
                GridViewColumn column = new GridViewColumn();
                column.DisplayMemberBinding = new Binding("ToolNumber");
                column.Header = "Tool Number";
                gridView.Columns.Add(column);
            }

            if (settings.PotNumVisible)
            {
                GridViewColumn column = new GridViewColumn();
                column.DisplayMemberBinding = new Binding("PotNum");
                column.Header = "Pot Number";
                gridView.Columns.Add(column);
            }

            if (settings.ToolLifeVisible)
            {
                GridViewColumn column = new GridViewColumn();
                column.DisplayMemberBinding = new Binding("ToolLife");
                column.Header = "Tool Life";
                gridView.Columns.Add(column);
            }

            if (settings.ToolLifeRemainingVisible)
            {
                GridViewColumn column = new GridViewColumn();
                column.DisplayMemberBinding = new Binding("ToolLifeRem");
                column.Header = "Tool Life Remaining";
                gridView.Columns.Add(column);
            }

            if (settings.ToolLengthOffsetVisible)
            {
                GridViewColumn column = new GridViewColumn();
                column.DisplayMemberBinding = new Binding("ToolLengthOffset");
                column.Header = "Length Offset";
                gridView.Columns.Add(column);
            }

            if (settings.ToolIsAttachedVisible)
            {
                GridViewColumn column = new GridViewColumn();
                column.DisplayMemberBinding = new Binding("ToolIsAttached");
                column.Header = "Tool Attached";
                gridView.Columns.Add(column);
            }

            if (settings.ToolNameVisible)
            {
                GridViewColumn column = new GridViewColumn();
                column.DisplayMemberBinding = new Binding("ToolName");
                column.Header = "Tool Name";
                gridView.Columns.Add(column);
            }
        }

        private void getTools()
        {
            if (!chkToolNumber.IsChecked.Value &&
                !chkPotNum.IsChecked.Value &&
                !chkToolLife.IsChecked.Value &&
                !chkToolLifeRem.IsChecked.Value &&
                !chkToolLengthOffset.IsChecked.Value &&
                !chkIsAttached.IsChecked.Value &&
                !chkToolName.IsChecked.Value)
            {
                MessageBox.Show("Check which columns to add.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                int[] toolNumList = cTools.GetToolList(Okuma.CMDATAPI.Enumerations.ToolListTypeEnum.AllTools);
                int[] attachedToolList = cTools.GetToolList(Okuma.CMDATAPI.Enumerations.ToolListTypeEnum.AttachedTools);
                toolsList.Clear();

                foreach (int toolNum in toolNumList)
                {
                    int potNumber = cTools.GetPotNo(toolNum);
                    int toolLife = cTools.GetToolLife(toolNum);
                    int toolLifeRem = cTools.GetToolLifeRemaining(toolNum);
                    double lengthOffset = cTools.GetToolOffset(toolNum, Okuma.CMDATAPI.Enumerations.ToolCompensationEnum.HADA);
                    string isAttached = "Not Attached";
                    string toolName = cTools.GetToolName(toolNum);

                    for (int i = 0; i < attachedToolList.Length; i++)
                    {
                        if (attachedToolList[i] == toolNum)
                        {
                            isAttached = "Attached";
                            break;
                        }
                    }

                    Tool tool = new Tool(toolNum, potNumber, toolLife, toolLifeRem, lengthOffset, isAttached, toolName);

                    toolsList.Add(tool);
                }
                if (dbOperations != null)
                {
                    dbOperations.InsertTools(toolsList);
                    lvTools.ItemsSource = dbOperations.GetTools();
                }
            }
        }

        private void cbxAutoPoll_Checked(object sender, RoutedEventArgs e)
        {
            if (cbxAutoPoll.IsChecked == true)
            {
                Properties.Settings.Default.autoPoll = true;
            }

            Properties.Settings.Default.Save();
        }

        private void cbxAutoPoll_Unchecked(object sender, RoutedEventArgs e)
        {
            if (cbxAutoPoll.IsChecked == false)
            {
                Properties.Settings.Default.autoPoll = false;
            }
            Properties.Settings.Default.Save();
        }
    }
}
