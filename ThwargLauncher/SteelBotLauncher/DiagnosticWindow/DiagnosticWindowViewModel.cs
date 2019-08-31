using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using CommonControls;

namespace SteelBotLauncher
{
    public class DiagnosticWindowViewModel
    {
        private readonly Configurator _configurator;
        public ICommand OpenLogsCommand { get; private set; }

        public DiagnosticWindowViewModel(Configurator configurator)
        {
            _configurator = configurator;
            OpenLogsCommand = new DelegateCommand(
                    PerformOpenLogs
                );

        }
        public string DiagnosticInfo { get { return GetDiagnosticString(); } }
        private string GetDiagnosticString()
        {
            StringBuilder text = new StringBuilder();

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            text.AppendFormat("SteelBotLauncher: {0} - {1}",
                assembly.GetName().Version, assembly.Location);
            text.AppendLine();

            foreach (Configurator.GameConfig config in _configurator.GetGameConfigs())
            {
                text.AppendFormat("SteelFilter: {0} - {1}", config.SteelFilterVersion, config.SteelFilterPath);
                text.AppendLine();
            }
            return text.ToString();
        }
        private void PerformOpenLogs()
        {
            string filepath = SteelFilter.FileLocations.GetRunningFolder();
            if (string.IsNullOrEmpty(filepath))
            {
                System.Windows.MessageBox.Show("Empty running folder returned from SteelFilter!");
                return;
            }
            System.Diagnostics.Process.Start(filepath);
        }
    }
}
