﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;

namespace SteelBotLauncher
{
    /// <summary>
    /// Called on worker thread
    /// </summary>
    class LaunchManager
    {
        public class LaunchManagerResult
        {
            public bool Success;
            public int ProcessId;
            public IntPtr Hwnd;
        }
        public delegate void ReportStatusHandler(string status, LaunchItem launchItem);
        public event ReportStatusHandler ReportStatusEvent;
        private void ReportStatus(string status, LaunchItem launchItem)
        {
            if (ReportStatusEvent != null)
            {
                ReportStatusEvent(status, launchItem);
            }
        }

        private string _launcherLocation;
        private LaunchItem _launchItem;
        private Dictionary<string, DateTime> _accountLaunchTimes;

        public LaunchManager(string launcherLocation, LaunchItem launchItem, Dictionary<string, DateTime> accountLaunchTimes)
        {
            _launcherLocation = launcherLocation;
            _launchItem = launchItem;
            _accountLaunchTimes = accountLaunchTimes;

        }
        public LaunchManagerResult LaunchGameHandlingDelaysAndTitles(BackgroundWorker worker)
        {
            var result = new LaunchManagerResult();
            if (worker.CancellationPending)
            {
                return result;
            }

            GameLaunchResult gameLaunchResult = null;

            ReportStatus("Launching", _launchItem);
            _accountLaunchTimes[_launchItem.AccountName] = DateTime.UtcNow;

            var launcher = new GameLauncher();
            launcher.ReportGameStatusEvent += (o) => { ReportStatus(o, _launchItem); };
            launcher.StopLaunchEvent += (o, eventArgs) => { return worker.CancellationPending; };
            try
            {
                var finder = new ThwargUtils.WindowFinder();
                string launcherPath = GetLaunchItemLauncherLocation(_launchItem);
                OverridePreferenceFile(_launchItem.CustomPreferencePath);
                gameLaunchResult = launcher.LaunchGameClient(
                    launcherPath,
                    _launchItem.ServerName,
                    accountName: _launchItem.AccountName,
                    password: _launchItem.Password,
                    ipAddress: _launchItem.IpAndPort,
                    gameApiUrl: _launchItem.GameApiUrl,
                    loginServerUrl: _launchItem.LoginServerUrl,
                    discordurl: _launchItem.DiscordUrl,
                    emu: _launchItem.EMU,
                    desiredCharacter: _launchItem.CharacterSelected,
                    rodatSetting: _launchItem.RodatSetting,
                    secureSetting: _launchItem.SecureSetting,
                    simpleLaunch: _launchItem.IsSimpleLaunch
                    );
                if (!gameLaunchResult.Success)
                {
                    return result;
                }
                var regex = GetGameWindowCaptionRegex();
                if (regex != null)
                {
                    IntPtr hwnd = finder.FindWindowByCaptionAndProcessId(regex, newWindow: true, processId: gameLaunchResult.ProcessId);
                    if (hwnd != IntPtr.Zero)
                    {
                        result.Hwnd = hwnd;
                        string newGameTitle = GetNewGameTitle(_launchItem);
                        if (!string.IsNullOrEmpty(newGameTitle))
                        {
                            Logger.WriteDebug("Found hwnd: " + newGameTitle);
                            finder.SetWindowTitle(hwnd, newGameTitle);
                        }
                    }
                    else
                    {
                        Logger.WriteDebug("Unable to find hwnd");
                    }
                }
            }
            catch (Exception exc)
            {
                ReportStatus("Exception launching game launcher: " + exc.Message, _launchItem);
                return result;
            }
            if (gameLaunchResult != null && gameLaunchResult.Success)
            {
                result.Success = true;
                result.ProcessId = gameLaunchResult.ProcessId;
            }
            return result;
        }
        private static Regex GetGameWindowCaptionRegex()
        {
            string gameCaptionPattern = ConfigSettings.GetConfigString("GameCaptionPattern", null);
            if (gameCaptionPattern != null)
            {
                var regex = new System.Text.RegularExpressions.Regex(gameCaptionPattern);
                return regex;
            }
            else
            {
                return null;
            }
        }
        private void OverridePreferenceFile(string customPreferencePath)
        {
            // Non-customizing launches need to restore active copy from base
            if (string.IsNullOrEmpty(customPreferencePath))
            {
                if (File.Exists(Configuration.UserPreferencesBaseFile))
                {
                    File.Copy(Configuration.UserPreferencesBaseFile, Configuration.UserPreferencesFile, overwrite: true);
                }
                return;
            }
            // customizing launches:
            if (!File.Exists(customPreferencePath)) { return; }
            // Backup actual file first

            if (!File.Exists(Configuration.UserPreferencesBaseFile))
            {
                File.Copy(Configuration.UserPreferencesFile, Configuration.UserPreferencesBaseFile, overwrite: false);
                if (!File.Exists(Configuration.UserPreferencesBaseFile)) { return; }
            }
            // Now overwrite
            File.Copy(customPreferencePath, Configuration.UserPreferencesFile, overwrite: true);
        }

        private string GetLaunchItemLauncherLocation(LaunchItem item)
        {
            if (!string.IsNullOrEmpty(item.CustomLaunchPath))
            {
                return item.CustomLaunchPath;
            }
            else
            {
                return _launcherLocation;
            }
        }
        private string GetNewGameTitle(LaunchItem launchItem)
        {
            string pattern = ConfigSettings.GetConfigString("NewGameTitle", "");
            if (launchItem.CharacterSelected == "None")
            {
                pattern = ConfigSettings.GetConfigString("NewGameTitleNoChar", "");
            }
            string alias = launchItem.Alias;
            if (string.IsNullOrEmpty(alias)) { alias = launchItem.AccountName; } // fall back to account if no alias
            pattern = pattern.Replace("%ALIAS%", alias);
            pattern = pattern.Replace("%ACCOUNT%", launchItem.AccountName);
            pattern = pattern.Replace("%SERVER%", launchItem.ServerName);
            if (launchItem.CharacterSelected != "None")
            {
                pattern = pattern.Replace("%CHARACTER%", launchItem.CharacterSelected);
            }
            return pattern;
        }
    }
}
