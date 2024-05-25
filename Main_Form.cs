using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Win_Duster
{
    public partial class Main_Form : MetroFramework.Forms.MetroForm
    {
        private bool isfixing = false;
        private int currentIndex = 0;
        public Main_Form()
        {
            InitializeComponent();
            metroListView1.View = View.Details;
            metroListView1.Columns.Add("Operation", 150);
            metroListView1.Columns.Add("Status", 150);
            // Set alignment of the "Status" column in the ListView
            metroListView1.Columns[1].TextAlign = HorizontalAlignment.Left;
        }

        private void KillDISMProcesses()
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = "/F /IM DISM.exe /T",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process process = new Process())
                {
                    process.StartInfo = processInfo;
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void start_button_Click(object sender, EventArgs e)
        {
            string errorMessage;
            bool isNetworkAvailable = NetworkHelper.IsNetworkAvailable(out errorMessage);

            if (isNetworkAvailable)
            {
                isfixing = true;
                start_button.Enabled = false;
                KillDISMProcesses();
                // List of DISM commands to execute
                var dismCommands = new Dictionary<string, string>
    {

        {"Scan Health", "/Cleanup-Image /ScanHealth"},
        {"Check Health", "/Cleanup-Image /CheckHealth"},
        {"Restore Health", "/Cleanup-Image /RestoreHealth"},
        {"Analyze Component Store", "/cleanup-image /AnalyzeComponentStore"},
        {"Component Cleanup", "/cleanup-image /StartComponentCleanup"},
        {"Reset Base", "/cleanup-image /ResetBase"}
    };

                // Clear existing items in the ListView
                metroListView1.Items.Clear();

                // Add items to ListView
                foreach (var command in dismCommands)
                {
                    ListViewItem item = new ListViewItem(new[] { command.Key, "Pending" });
                    metroListView1.Items.Add(item);
                }

                // Execute DISM commands
                int totalProgress = 0; // Initialize total progress to 0

                // Execute DISM commands
                foreach (var command in dismCommands)
                {
                    await ExecuteDismCommandAsync(command.Key, command.Value);
                    totalProgress += 100; // Assuming each command contributes 100% to total progress
                }

                start_button.Enabled = true;
                isfixing = false;
            }
            else
            {
                metroLabel1.Style = MetroFramework.MetroColorStyle.Red;
                MessageBox.Show("It appears that you are not connected to the internet. An internet connection is required to start fixing Windows corrupted files.", "Network Status", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private async Task ExecuteDismCommandAsync(string commandKey, string commandValue)
        {
            await Task.Run(() =>
            {
                Console.WriteLine($"Executing DISM command: {commandKey}");

                // Create process to execute DISM command
                Process dismProcess = new Process();
                dismProcess.StartInfo.FileName = "dism.exe";
                dismProcess.StartInfo.Arguments = $"/Online {commandValue}";
                dismProcess.StartInfo.UseShellExecute = false;
                dismProcess.StartInfo.RedirectStandardOutput = true;
                dismProcess.StartInfo.RedirectStandardError = true;
                dismProcess.StartInfo.CreateNoWindow = true;
                dismProcess.EnableRaisingEvents = true;

                // Event handler for capturing output
                dismProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine(e.Data); // Output command status to console
                                                   // Update ListView with command status
                        BeginInvoke(new Action(() =>
                        {
                            // Find the corresponding ListViewItem by searching for the command key
                            var item = metroListView1.Items
                                .Cast<ListViewItem>()
                                .FirstOrDefault(i => i.SubItems[0].Text == commandKey);

                            if (item != null)
                            {
                                item.SubItems[1].Text = e.Data;
                                // Parse progress percentage and update
                                UpdateProgress(e.Data, item);
                            }
                        }));
                    }
                };

                // Start the process
                dismProcess.Start();

                // Begin async reading of the output
                dismProcess.BeginOutputReadLine();

                // Wait for the process to exit
                dismProcess.WaitForExit();

                Console.WriteLine($"DISM command {commandKey} completed.");
            });
        }



        private void UpdateProgress(string output, ListViewItem item)
        {
            // Search for progress percentage in output
            // Assuming progress is represented as a number followed by '%'
            int startIndex = output.IndexOf('%') - 4;
            if (startIndex >= 0)
            {
                string progressStr = output.Substring(startIndex, 4);
                if (double.TryParse(progressStr, out double progress))
                {

                    // Update progress in ListView
                    item.SubItems[1].Text = $"{progress}%";

                }
            }
        }

        private async void cancle_button_Click(object sender, EventArgs e)
        {
            KillDISMProcesses();
            await Task.Delay(1000);
            KillDISMProcesses();
            await Task.Delay(1000);
            KillDISMProcesses();
            await Task.Delay(1000);
            KillDISMProcesses();
            await Task.Delay(1000);
            KillDISMProcesses();
            await Task.Delay(1000);
            KillDISMProcesses();
            await Task.Delay(1000);
        }

        private void metroTabPage2_Click(object sender, EventArgs e)
        {

        }



        private async void metroButton1_Click(object sender, EventArgs e)
        {
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, false);
            }
            // Show a warning message
            DialogResult result = MessageBox.Show("Warning: Once you start this process, you cannot cancel it until it's done. Canceling it prematurely could cause harm to your system. Are you sure you want to proceed?",
                                                    "Warning",
                                                    MessageBoxButtons.OKCancel,
                                                    MessageBoxIcon.Warning);

            // If user clicks OK, start the process
            if (result == DialogResult.OK)
            {
                await RunBatchScriptAsync();
                metroButton1.Enabled = false;
            }
            else
            {
                MessageBox.Show("Cancled");
            }

        }

        private async Task RunBatchScriptAsync()
        {
            string batchFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.bat");
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "cmd.exe";
            processStartInfo.Arguments = "/c \"" + batchFilePath + "\"";
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;

            using (Process process = new Process())
            {
                process.StartInfo = processStartInfo;

                // Initialize currentStep to -1 so that the first increment brings it to 0
                int currentStep = -1;

                process.OutputDataReceived += async (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        UpdateLabel(args.Data);

                        // Check if the output indicates a step
                        if (args.Data.StartsWith("Step "))
                        {
                            // Increment currentStep only when there's a step-related output
                            currentStep++;
                        }
                    }
                };

                process.Start();
                process.BeginOutputReadLine();

                await Task.Run(() => process.WaitForExit());
            }
        }


        private void UpdateLabel(string output)
        {
            if (!string.IsNullOrEmpty(output))
            {
                Invoke(new Action(() =>
                {
                    lblStatus.Text = output;
                    Console.WriteLine(lblStatus.Text);
                }));
            }
        }
        int tick = 0;
        private void lblStatus_TextChanged(object sender, EventArgs e)
        {
            if (lblStatus.Text == "Completed")
            {
                this.MinimizeBox = true;
                this.MaximizeBox = true;
            }
            // Get the current echo text from the label
            string echoText = lblStatus.Text;

            // Iterate through each item in the CheckedListBox
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                // Get the text of the current item
                string itemText = checkedListBox1.Items[i].ToString();

                // Check if the label text contains the echo text corresponding to the current item
                if (echoText.Contains(itemText))
                {
                    // Check the current item
                    checkedListBox1.SetItemChecked(i, true);
                }
            }

            // Check if all items are checked
            bool allChecked = true;
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                if (!checkedListBox1.GetItemChecked(i))
                {
                    allChecked = false;
                    break;
                }
            }

            // Enable the button if all items are checked
            if (allChecked)
            {
                metroButton1.Enabled = true;
                MessageBox.Show("Please Wait For 1 Min While We Clean Up The Mess");
                timer1.Start();
                timer1.Interval = 1000;

            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            tick++;
            if (tick == 60000)
            {
                this.MinimizeBox = true;
                this.MaximizeBox = true;
                lblStatus.Text = ("Completed");
                for (int i = 0; i < checkedListBox1.Items.Count; i++)
                {
                    checkedListBox1.SetItemChecked(i, false);
                }
                timer1.Stop();
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            string errorMessage;
            bool isNetworkAvailable = NetworkHelper.IsNetworkAvailable(out errorMessage);
            if (isNetworkAvailable)
            {
                metroLabel1.Text = "You are online and ready to start fixing.";
                metroLabel1.ForeColor = Color.Green;
            }
            else
            {
                metroLabel1.Text = "You are currently offline. Please check your internet connection.";
                metroLabel1.ForeColor = Color.Red;
                KillDISMProcesses();
                if (isfixing == true)
                {
                    MessageBox.Show("The process was interrupted due to a lost internet connection. All operations have been canceled.", "Network Status", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    metroLabel1.Text = "You are currently offline. Please check your internet connection.";
                }
            }

        }

        private void Main_Form_Load(object sender, EventArgs e)
        {
            string errorMessage;
            bool isNetworkAvailable = NetworkHelper.IsNetworkAvailable(out errorMessage);
            timer2.Interval = 2000;//2 seconds
            if (isNetworkAvailable)
            {
                metroLabel1.Text = "You are online and ready to start fixing.";
                timer2.Start();
                metroLabel1.ForeColor = Color.Green;
            }
            else
            {
                metroLabel1.Text = "You are currently offline. Please check your internet connection.";
                metroLabel1.ForeColor = Color.Red;
                if (isfixing == true)
                {
                    MessageBox.Show("It appears that you are not connected to the internet. An internet connection is required to start fixing Windows corrupted files.");
                }
                else
                {

                }
            }
        }
    }
}
