﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using WindowPlacementUtil;

namespace SteelBotLauncher
{
    /// <summary>
    /// Interaction logic for Help.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        private HelpWindowViewModel _viewModel;
        private DiagnosticsWindow _diagnosticsWindow;

        public HelpWindow(HelpWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            chkShowStartup.IsChecked = TryGetShowHelpAtStart();
            var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            var version = assemblyName.Version;
            var assemblyTitle = assemblyName.Name;
            this.Title = string.Format("Help - {0} {1}", assemblyTitle, version);
            SteelBotLauncher.AppSettings.WpfWindowPlacementSetting.Persist(this);
        }
        private bool TryGetShowHelpAtStart()
        {
            try
            {
                return Properties.Settings.Default.ShowHelpAtStart;
            }
            catch
            {
                return true;
            }
        }

        private void btnDefaultPreferences_Click(object sender, RoutedEventArgs e)
        {
            string pathtoPreferences = Configuration.UserPreferencesFile;

            if (File.Exists(pathtoPreferences))
            {
                Process.Start("notepad.exe", pathtoPreferences);
            }
            else
            {
                MessageBox.Show("Your UserPreferences file is not in the default location.", "File not found.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            // run the updater application if it is where we expect it to be (installation root)
            if (System.IO.File.Exists("updater.exe"))
            {
                int exitCode;
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = "updater.exe";
                using (Process proc = Process.Start(info))
                {
                    proc.WaitForExit();
                    exitCode = proc.ExitCode;
                }
            }
        }
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void btnDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            if (_diagnosticsWindow == null)
            {
                _diagnosticsWindow = new DiagnosticsWindow(_viewModel.GetDiagnosticWindowViewModel());
                _diagnosticsWindow.Closing += _diagnosticsWindow_Closing;
            }
            _diagnosticsWindow.Show();
        }

        void _diagnosticsWindow_Closing(object sender, CancelEventArgs e)
        {
            _diagnosticsWindow = null;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_diagnosticsWindow != null)
            {
                _diagnosticsWindow.Close();
                _diagnosticsWindow = null;
            }
            Properties.Settings.Default.ShowHelpAtStart = chkShowStartup.IsChecked.Value;
            Properties.Settings.Default.Save();
        }
    }
}
