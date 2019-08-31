﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows;

namespace SteelBotLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += (sender, eargs)
                => HandleExcObject(eargs.ExceptionObject);
            AppCoordinator appcoord = new AppCoordinator();
        }
        void HandleExcObject(object excObj)
        {
            var exc = excObj as Exception;
            if (exc == null)
            {
                exc = new NotSupportedException(
                    "Unhandled exception doesn't derive from System.Exception: "
                    + excObj.ToString());
            }
            HandleExc(exc);
        }
        void HandleExc(Exception exc)
        {
            Logger.WriteError("Fatal Exception: " + exc.ToString());
            MessageBox.Show("Fatal Program Error: See log file");
        }
    }
}
