using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace System_Performance_Monitor
{
    public partial class Form1 : Form
    {
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private DriveInfo driveInfo;
        private Thread monitoringThread;
        private bool isDragging = false;
        private Point dragStartPoint;
        private bool isFullScreen = false;
        Size normalSize = new Size(816, 489);
        public Form1()
        {
            InitializeComponent();

        }
        private void panel2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartPoint = new Point(e.X, e.Y);
            }
        }

        private void panel2_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point currentPoint = PointToScreen(new Point(e.X, e.Y));
                Location = new Point(currentPoint.X - dragStartPoint.X, currentPoint.Y - dragStartPoint.Y);
            }
        }

        private void panel2_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (isFullScreen)
            {
                // Restore the form to the normal size and show the form border
                this.WindowState = FormWindowState.Normal;
                this.Size = normalSize;
            }
            else
            {
                // Set the form to fullscreen mode with no form border
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }

            // Toggle the flag
            isFullScreen = !isFullScreen;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // Set up the chart1 for CPU usage
            ChartArea cpuChartArea = new ChartArea();
            cpuChartArea.AxisX.Interval = 1;
            cpuChartArea.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            cpuChartArea.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chart1.ChartAreas.Add(cpuChartArea);

            Series cpuSeries = new Series("CPU Usage");
            cpuSeries.ChartType = SeriesChartType.Line;
            chart1.Series.Add(cpuSeries);

            // Set up the chart2 for RAM usage
            ChartArea ramChartArea = new ChartArea();
            ramChartArea.AxisX.Interval = 1;
            ramChartArea.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            ramChartArea.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chart2.ChartAreas.Add(ramChartArea);

            Series ramSeries = new Series("RAM Usage");
            ramSeries.ChartType = SeriesChartType.Line;
            chart2.Series.Add(ramSeries);
            string driveLetter = "C:";
            driveInfo = new DriveInfo(driveLetter);
            // Start monitoring thread
            monitoringThread = new Thread(MonitorResourceUsage);
            monitoringThread.Start();
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        }
        private void MonitorResourceUsage()
        {
            PerformanceCounter ramUsedCounter = new PerformanceCounter("Memory", "Committed Bytes");

            while (true)
            {
                float cpuUsage = cpuCounter.NextValue();
                float ramUsageBytes = ramUsedCounter.NextValue();
                float ramUsageMB = ramUsageBytes / (1024 * 1024); // Convert bytes to megabytes
                float ramUsageGB = ramUsageMB / 1024; // Convert megabytes to gigabytes
                float diskUsed = driveInfo.TotalSize - driveInfo.TotalFreeSpace;
                float diskUsagePercentage = (diskUsed / driveInfo.TotalSize) * 100;

                // Update the charts and labels on the main UI thread
                Invoke(new Action(() =>
                {
                    DateTime timestamp = DateTime.Now;

                    label3.Text = string.Format("CPU Usage: {0}%", cpuUsage);
                    label4.Text = string.Format("RAM Usage: {0} MB", ramUsageMB);
                    label6.Text = string.Format("RAM Usage: {0} GB", ramUsageGB.ToString("0.00"));
                    label7.Text = string.Format("Disk Usage: {0} GB ({1}%)",
                        (diskUsed / 1024f / 1024f / 1024f).ToString("0.00"), diskUsagePercentage.ToString("0.00"));

                    // Add data points to the CPU chart
                    chart1.Series["CPU Usage"].Points.AddXY(timestamp, cpuUsage);

                    // Add data points to the RAM chart
                    chart2.Series["RAM Usage"].Points.AddXY(timestamp, ramUsageMB);

                    // Remove old data points if the charts exceed a certain number of points
                    if (chart1.Series["CPU Usage"].Points.Count > 50)
                        chart1.Series["CPU Usage"].Points.RemoveAt(0);

                    if (chart2.Series["RAM Usage"].Points.Count > 50)
                        chart2.Series["RAM Usage"].Points.RemoveAt(0);

                    // Set the X-axis label format to display time
                    chart1.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss";
                    chart2.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss";

                    // Calculate the start time for the visible range (last 10 seconds)
                    DateTime endTime = timestamp;
                    DateTime startTimeVisible = endTime.AddSeconds(-10);

                    // Set the minimum and maximum values of the X-axis to show the last 10 seconds
                    chart1.ChartAreas[0].AxisX.Minimum = startTimeVisible.ToOADate();
                    chart1.ChartAreas[0].AxisX.Maximum = endTime.ToOADate();
                    chart2.ChartAreas[0].AxisX.Minimum = startTimeVisible.ToOADate();
                    chart2.ChartAreas[0].AxisX.Maximum = endTime.ToOADate();
                }));

                // Refresh the charts
                Invoke(new Action(() =>
                {
                    chart1.Update();
                    chart2.Update();
                }));

                // Wait for 1 second
                Thread.Sleep(1000);
            }
        }



        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            // Stop the monitoring thread when the form is closing
            monitoringThread?.Abort();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            panel3.Hide();
            panel4.Hide();
            panel5.Hide();
            chart1.Show();
            chart2.Show();
            splitContainer1.Show();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            panel3.Show();
            panel4.Show();
            panel5.Show();
            chart1.Hide();
            chart2.Hide();
            splitContainer1.Hide();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Created by Digital-S on 5/31/2023","About",MessageBoxButtons.OK,MessageBoxIcon.Information);
            Process.Start("https://github.com/DigitalSerpant/System-Performance-Monitor");
        }
    }
}
