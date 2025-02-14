﻿using Microsoft.Web.WebView2.Core;
using NLog;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace IntermittentIssueDetector
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private Stopwatch stopwatch;

        public Form1()
        {
            InitializeComponent();
            InitializeLogger();
            this.StartPosition = FormStartPosition.CenterScreen; // 设置表单初始位置为屏幕中央
        }

        private void InitializeLogger()
        {
            // Config NLog, log to file
            var config = new NLog.Config.LoggingConfiguration();
            var logfile = new NLog.Targets.FileTarget("csvFile")
            {
                FileName = "log.csv",
                Layout = "${longdate},${level},${message}"
            };
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);
            LogManager.Configuration = config;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                Logger.Debug("Entering Form1_Load");

                InitializeBrowser();
                Logger.Debug("Browser initialized");

                frequencyTextBox.Text = "99999";
                Logger.Debug("Frequency set to 99999 seconds");

                Logger.Debug("Leaving Form1_Load");
            }
            catch (Exception ex)
            {
                Logger.Fatal("Form1_Load failed");
                Logger.Fatal(ex.Message);
                Logger.Fatal(ex.StackTrace);
            }
        }

        async void InitializeBrowser()
        {
            // Create WebView2 environment to have SSO enabled for system primary account
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions()
            {
                AllowSingleSignOnUsingOSPrimaryAccount = true
            };
            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--enable-features=msSingleSignOnOSForPrimaryAccountIsShared");
            CoreWebView2Environment environment = CoreWebView2Environment.CreateAsync(null, null, options).Result;
            await webView21.EnsureCoreWebView2Async(environment);
            webView21.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            webView21.CoreWebView2.WebMessageReceived += webView21_WebMessageReceived;
        }

        /// <summary>
        /// Handle the event when a navigation operation in the WebView2 control is completed. It stops a stopwatch to measure the load time, logs the load time and URL, executes a JavaScript snippet to find a specific element on the page, and logs the results. If any errors occur, they are caught and logged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                Logger.Debug("Entering CoreWebView2_NavigationCompleted");

                stopwatch.Stop();
                long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                Logger.Info($"Page loaded in {elapsedMilliseconds} milliseconds, url: {webView21.CoreWebView2.Source}");

                //// Execute JavaScript to get the number of elements with the class name, and send the number to native with postMessage, how many elements do we have with the same class name
                //webView21.CoreWebView2.ExecuteScriptAsync("" +
                //    "var tag = document.getElementsByClassName('xxx');".Replace("xxx", SuccessTagTextBox.Text) +
                //    "window.chrome.webview.postMessage(tag.length.toString());");

                // Execute JavaScript to find the element with the text, and send the element to native with postMessage, as long as the page contains the text, thie will triggerr the WebMessageReceived event
                webView21.CoreWebView2.ExecuteScriptAsync("" +
                    "const targetText = 'xxx';".Replace("xxx", SuccessTagTextBox.Text) +
                    "const element = Array.from(document.querySelectorAll('*')).find(el => el.textContent.includes(targetText));" +
                    "if (element) { window.chrome.webview.postMessage(element.toString()); } else { window.chrome.webview.postMessage('Element not found'); }" +
                    "");

                Logger.Debug("Leaving CoreWebView2_NavigationCompleted");
            }
            catch (Exception ex)
            {
                Logger.Error("CoreWebView2_NavigationCompleted failed");
                Logger.Error($"{ex.Message}");
                Logger.Error($"{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Handle messages received from the web content in the WebView2 control. It logs the received message and checks if the message indicates a successful page load or a failure, logging appropriate messages for each case. If an error occurs during this process, it logs the error details. This event is triggered by the JavaScript code executed in the CoreWebView2_NavigationCompleted event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void webView21_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                Logger.Debug("Entering webView21_WebMessageReceived");

                string message = e.TryGetWebMessageAsString();

                //// Check if the message is a number, how many elements do we have with the same class name
                //if (int.TryParse(message, out int result))
                //{
                //    // If the number is greater than 0, the page is loaded
                //    if (result > 0)
                //    {
                //        Logger.Debug("Page loaded successfully");
                //    }
                //    else
                //    {
                //        Logger.Debug("Page failed to load");
                //    }
                //}
                //else
                //{
                //    Logger.Error("Message received is not a number");
                //}

                // Check if the message is not empty and not "Element not found", as long as the page contains the text
                if (!string.IsNullOrEmpty(message) && message != "Element not found")
                {
                    Logger.Debug($"Page loaded successfully, url: {webView21.CoreWebView2.Source}, tag: {SuccessTagTextBox.Text}");
                }
                else
                {
                    Logger.Error($"Page failed to load, url: {webView21.CoreWebView2.Source}, tag: {SuccessTagTextBox.Text}");
                }

                Logger.Debug("Leaving webView21_WebMessageReceived");
            }
            catch (Exception ex)
            {
                Logger.Error("webView21_WebMessageReceived failed");
                Logger.Error($"{ex.Message}");
                Logger.Error($"{ex.StackTrace}");
            }
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            try
            {
                Logger.Debug("Entering goButton_Click");

                stopwatch = Stopwatch.StartNew();
                webView21.CoreWebView2.Navigate(urlTextBox.Text);
                // Set the timer interval to frequencyTextBox
                timer1.Interval = Convert.ToInt32(frequencyTextBox.Text) * 1000;
                timer1.Start();
                Logger.Debug("Timer started");

                Logger.Debug("Leaving goButton_Click");
            }
            catch (Exception ex)
            {
                Logger.Error("goButton_Click failed");
                Logger.Error($"{ex.Message}");
                Logger.Error($"{ex.StackTrace}");
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            // Stop the timer
            timer1.Stop();
            Logger.Debug("Timer stopped");
        }

        /// <summary>
        /// Handle the Tick event of the timer1 control. When the timer interval elapses, this method is triggered. It starts a new Stopwatch to measure the time taken for the navigation operation, navigates the WebView2 control to the URL specified in the urlTextBox, and logs the entry and exit of the method. If any errors occur, they are caught and logged. This event is triggered by the timer1 control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                Logger.Debug("Entering timer1_Tick");

                stopwatch = Stopwatch.StartNew();
                webView21.CoreWebView2.Navigate(urlTextBox.Text);

                Logger.Debug("Leaving timer1_Tick");
            }
            catch (Exception ex)
            {
                Logger.Error("timer1_Tick failed");
                Logger.Error($"{ex.Message}");
                Logger.Error($"{ex.StackTrace}");
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // If the form is minimized, restore it to normal state, otherwise minimize it
            if (WindowState == FormWindowState.Minimized)
            {
                Show();
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                WindowState = FormWindowState.Minimized;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // If the form is minimized, hide it and show the notify icon
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.Visible = true;
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                metroContextMenu1.Show(Cursor.Position);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Restore the form to normal state
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void runInBackgroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Minimize the form if it is not minimized
            if (WindowState != FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Minimized;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void openLogButton_Click(object sender, EventArgs e)
        {
            // Open the log file with the default text editor
            Process.Start("log.csv");
        }

        private void urlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                goButton_Click(this, new EventArgs()); // 调用goButton_Click方法
                e.Handled = true;
                e.SuppressKeyPress = true; // 防止“叮”声
            }
        }
    }
}