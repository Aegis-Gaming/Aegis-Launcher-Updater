﻿using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace TechnicLauncher
{
    public partial class Form1 : Form
    {
        public string LauncherURL = "http://206.217.207.1/Technic/";
        private readonly string _launcherFile = Path.Combine(Program.AppPath, Program.LaucherFile);
        private readonly string _launcherBackupFile = Path.Combine(Program.AppPath, Program.LaucherFile + ".bak");
        private readonly string _launcherTempFile = Path.Combine(Program.AppPath, Program.LaucherFile + ".temp");
        private int _hashDownloadCount, _launcherDownloadCount;
        private Exception error;

        public bool IsAddressible(Uri uri)
        {
            try
            {
                using (var client = new MyClient())
                {
                    client.HeadOnly = true;
                    client.DownloadString(uri);
                    return true;
                }
            }
            catch (Exception loi)
            {
                
            }
            return false;
        }

        public Form1()
        {
            InitializeComponent();

            if (File.Exists(_launcherFile))
            {
                DownloadHash();
            }
            else
            {
                DownloadLauncher();
            }
        }

        private void DownloadHash()
        {
            lblStatus.Text = @"Checking Launcher Version..";
            var versionCheck = new WebClient();
            versionCheck.DownloadStringCompleted += DownloadStringCompleted;
            var uri = new Uri(String.Format("{0}CHECKSUM.md5", LauncherURL));
            if (_hashDownloadCount < 3 && IsAddressible(uri))
            {
                _hashDownloadCount++;
                versionCheck.DownloadStringAsync(uri, _launcherFile);
            }
            else
            {
                Program.RunLauncher(_launcherFile);
                Close();
            }
        }

        private void DownloadLauncher()
        {
            lblStatus.Text = String.Format(@"Downloading Launcher ({0}/{1})..", _launcherDownloadCount, 3);
            
            if (_launcherDownloadCount < 3)
            {
                _launcherDownloadCount++;
                var wc = new WebClient();
                wc.DownloadProgressChanged += DownloadProgressChanged;
                wc.DownloadFileCompleted += DownloadFileCompleted;
                wc.DownloadFileAsync(new Uri(String.Format("{0}technic-launcher.jar", LauncherURL)), _launcherTempFile);
            }
            else
            {
                MessageBox.Show("Error", error.Message);
                Program.RunLauncher(_launcherFile);
                Close();
            }
        }

        void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                error = e.Error;
                DownloadLauncher();
                return;
            }
            lblStatus.Text = @"Running Launcher..";
            pbStatus.Value = 100;

            if (File.Exists(_launcherBackupFile))
                File.Delete(_launcherBackupFile);
            if (File.Exists(_launcherFile))
                File.Move(_launcherFile, _launcherBackupFile);
            File.Move(_launcherTempFile, _launcherFile);
            Program.RunLauncher(_launcherFile);
            Close();
        }

        void DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                DownloadHash();
                return;
            }
            MD5 hash = new MD5CryptoServiceProvider();
            String md5 = null, serverMD5 = null;
            var sb = new StringBuilder();

            try
            {

                using (var fs = File.Open(_launcherFile, FileMode.Open, FileAccess.Read))
                {
                    var md5Bytes = hash.ComputeHash(fs);
                    foreach (byte hex in md5Bytes)
                        sb.Append(hex.ToString("x2"));
                    md5 = sb.ToString().ToLowerInvariant();
                }
            }
            catch (IOException ioException)
            {
                Console.WriteLine(ioException.Message);
                Console.WriteLine(ioException.StackTrace);

                MessageBox.Show("Cannot check launcher version, the launcher is currently open!", "Launcher Currently Open", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Application.Exit();
                return;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Console.WriteLine(exception.StackTrace);

                MessageBox.Show("Error checking launcher version", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            var lines = e.Result.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (!line.Contains("technic-launcher.jar")) continue;
                serverMD5 = line.Split('|')[0].ToLowerInvariant();
                break;
            }

            if (serverMD5 != null && serverMD5.Equals(md5)) {
                Program.RunLauncher(_launcherFile);
                Close();
            }
            else
            {
                DownloadLauncher();
            }
        }

        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            lblStatus.Text = String.Format("Downloaded {0}% of launcher..", e.ProgressPercentage);
            pbStatus.Value = e.ProgressPercentage;
        }
    }
}
