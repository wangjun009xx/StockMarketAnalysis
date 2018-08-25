﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;

namespace StockMarketAnalysis
{
    public class ChartHandler
    {
        // the chart stuff that the user sees
        public static Chart chart = new Chart();
        ChartArea chartArea, annotationArea;
        public static Series mainSeries;
        public static string ticker = "";

        public ChartHandler()
        {
            chartArea = new ChartArea();
            annotationArea = new ChartArea();
            mainSeries = new Series();

            //make the chart
            chartArea.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chartArea.AxisY.IsStartedFromZero = false;
            chartArea.BackColor = Color.FromArgb(45, 45, 45);
            chartArea.Position = new ElementPosition(0, 0, 100, 100);
            chartArea.AxisY.LabelStyle.Format = "0.00";
            chartArea.Name = "aMainChartArea";


            //set up annotation area
            annotationArea.AxisX.Minimum = 0;
            annotationArea.AxisX.Maximum = 100;
            annotationArea.AxisY.Minimum = 0;
            annotationArea.AxisY.Maximum = 100;
            annotationArea.BackColor = System.Drawing.Color.Transparent;
            annotationArea.Position = chartArea.Position;
            annotationArea.Name = "annotationArea";

            chart = new Chart();
            chart.ChartAreas.Add(chartArea);
            chart.ChartAreas.Add(annotationArea);
            chart.Name = "aMainChart";
            chart.Series.Add(mainSeries);
            chart.Dock = DockStyle.Fill;
            chart.TabIndex = 2;
            chart.BackColor = Color.FromArgb(45, 45, 45);

            chart.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            chart.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
            chart.ChartAreas[0].AxisX.ScrollBar.Enabled = false;
            chart.ChartAreas[0].AxisY.ScrollBar.Enabled = false;
            chart.ChartAreas[1].AxisX.ScaleView.Zoomable = true;
            chart.ChartAreas[1].AxisY.ScaleView.Zoomable = true;

            mainSeries.ChartArea = "aMainChartArea";
            mainSeries.ChartType = SeriesChartType.Candlestick;
            mainSeries.IsXValueIndexed = true;    // this seems to be very important. (removes weekends)
            chart.ChartAreas[0].AxisX.IsReversed = true;   // when the weekends are removed the chart seems to be revesed, this line fixes it
            mainSeries.Name = "aCandleSticks";
            mainSeries.XValueType = ChartValueType.Date;
            mainSeries.YValuesPerPoint = 4;
            mainSeries.SetCustomProperty("PriceUpColor", "Green");
            mainSeries.SetCustomProperty("PriceDownColor", "DarkRed");
            mainSeries.Color = Color.DarkSlateGray;
            mainSeries.BorderColor = Color.FromArgb(0, 0, 0);
        }

        /// <param name = "symbol" > ticker symbol of desired stock (ex. TSLA)</param>
        /// <param name = "rawDataPath" > path for data to be stored</param>
        private static bool getData(string symbol, string rawDataPath)
        {
            string strCmdText;
            strCmdText = "/C alpha-vantage-cli -s " + symbol + " -k TPMQDECWM5ATUR1L -o " + rawDataPath + symbol;

            //if the data hasn't already been downloaded, then do the alpha vantage download:
            if (!File.Exists(rawDataPath + symbol))
            {
                //to execute alpah vantage cli commands in the background
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = strCmdText;
                process.StartInfo = startInfo;
                process.Start();

                //wait for output file to download
                while (!process.HasExited)
                { }
            }

            //at this point the file should be made, if not, then it was an invalid symbol
            if (!File.Exists(rawDataPath + symbol))
            {
                MessageBox.Show("Couldn't find " + symbol);
                return false;
            }

            return true;
        }

        public static void loadStock(string symbol)
        {
            ticker = symbol;
            //get stock market data through alpha vantage
            string rawDataPath = @"C:\Users\Public\Documents\RawData\";
            if (!getData(symbol, rawDataPath))
            {
                return;
            }

            //get rid of previous data
            chart.Series[0].Points.Clear();

            //clearing previous data
            foreach (var series in chart.Series)
            {
                series.Points.Clear();
            }

            //reading the output file:
            using (var reader = new StreamReader(rawDataPath + symbol))
            {
                bool isFirstLine = true;
                while (!reader.EndOfStream)
                {
                    //to get rid of the first line of gaff that alpha-vantage gives
                    if (isFirstLine)
                    {
                        reader.ReadLine();
                        isFirstLine = false;
                    }

                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    //date stuff
                    var stringDateArr = values[0].Split('-');
                    int[] dateInfo = new int[3];
                    dateInfo[0] = Convert.ToInt32(stringDateArr[0]);
                    dateInfo[1] = Convert.ToInt32(stringDateArr[1]);
                    dateInfo[2] = Convert.ToInt32(stringDateArr[2]);

                    chart.Series[0].XValueType = ChartValueType.DateTime;
                    DateTime x = new DateTime(dateInfo[0], dateInfo[1], dateInfo[2]);

                    //candle stick data
                    double open = Convert.ToDouble(values[1]);
                    double high = Convert.ToDouble(values[2]);
                    double low = Convert.ToDouble(values[3]);
                    double close = Convert.ToDouble(values[4]);
                    double[] data = { high, low, open, close };
                    DataPoint candleStick = new DataPoint(x.ToOADate(), data);
                    chart.Series[0].Points.Add(candleStick);
                }
            }
        }
    }
}
