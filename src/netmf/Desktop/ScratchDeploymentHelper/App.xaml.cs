﻿//-------------------------------------------------------------------------
//  (c) 2015 Pervasive Digital LLC
//
//  This file is part of Scratch for .Net Micro Framework
//
//  "Scratch for .Net Micro Framework" is free software: you can 
//  redistribute it and/or modify it under the terms of the 
//  GNU General Public License as published by the Free Software 
//  Foundation, either version 3 of the License, or (at your option) 
//  any later version.
//
//  "Scratch for .Net Micro Framework" is distributed in the hope that
//  it will be useful, but WITHOUT ANY WARRANTY; without even the implied
//  warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See
//  the GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with "Scratch for .Net Micro Framework". If not, 
//  see <http://www.gnu.org/licenses/>.
//
//-------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Deployment.Application;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.ApplicationInsights;

using Ninject;

using PervasiveDigital.Scratch.Common;
using PervasiveDigital.Scratch.DeploymentHelper;
using PervasiveDigital.Scratch.DeploymentHelper.Models;
using PervasiveDigital.Scratch.DeploymentHelper.Server;
using PervasiveDigital.Scratch.DeploymentHelper.Views;

namespace PervasiveDigital.Scratch.DeploymentHelper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static DeploymentLogWindow _logWindow;
        private static IKernel _kernel;

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            _kernel = new StandardKernel(new Scratch.Common.Module(), new AppModule());

            var telemetryClient = new TelemetryClient();
            telemetryClient.InstrumentationKey = "2a8f2947-dfe9-48ef-8c8d-13184f9e46f9";
            telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            telemetryClient.Context.Device.Language = Thread.CurrentThread.CurrentUICulture.Name;
            telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.VersionString;
            telemetryClient.Context.Device.ScreenResolution = string.Format("{0}x{1}", SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
            telemetryClient.Context.Component.Version = typeof(App).Assembly.GetName().Version.ToString();
            telemetryClient.TrackEvent("Application Start");
            _kernel.Bind<TelemetryClient>().ToConstant(telemetryClient).InSingletonScope();
        }

        public static IKernel Kernel { get { return _kernel; } }

        public static void ShowDeploymentLogWindow()
        {
            if (_logWindow == null)
            {
                _logWindow = new DeploymentLogWindow();
                _logWindow.Closed += _logWindow_Closed;
            }
            _logWindow.Show();
            _logWindow.BringIntoView();
        }

        static void _logWindow_Closed(object sender, EventArgs e)
        {
            _logWindow.Closed -= _logWindow_Closed;
            _logWindow = null;
        }

        public static void ClearLogWindow()
        {
            if (_logWindow != null)
                _logWindow.Clear();
        }

        public static void AppendToLogWindow(string msg)
        {
            if (_logWindow!=null)
            {
                _logWindow.WriteLine(msg);
            }
        }

        private void Application_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var tc = _kernel.Get<TelemetryClient>();
            if (tc != null)
            {
                tc.TrackException(e.Exception, new Dictionary<string,string>() {{"type", "DispatcherUnhandledException"}});
                tc.Flush();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            var tc = _kernel.Get<TelemetryClient>();
            if (tc!=null)
            {
                tc.TrackEvent("Application Exit");
                tc.Flush();
            }

            var host = App.Kernel.Get<DeviceServer>();
            if (host != null)
                host.Close();

            var dm = App.Kernel.Get<DeviceModel>();
            if (dm != null)
            {
                try
                {
                    dm.Dispose();
                }
                catch 
                {
                    // bugs in MFDeploy code can cause this to throw
                }
            }
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                var host = App.Kernel.Get<DeviceServer>();
                host.Open();
            }
            catch (Exception ex)
            {
                var tc = App.Kernel.Get<TelemetryClient>();
                if (tc != null)
                {
                    tc.TrackException(ex);
                }
                MessageBox.Show("The Scratch Gateway was unable to configure or open the http port that Scratch needs to use to communicate with your device. The app will have to exit now. Please check the scratch4.net web site for help.","Fatal startup error", MessageBoxButton.OK);
                Application.Current.Shutdown();
            }

            //TODO: show a splash screen

            // Initialize the extensibility pipeline
            var xmgr = App.Kernel.Get<ExtensionManager>();
            await xmgr.Initialize();

            var fwmgr = App.Kernel.Get<FirmwareManager>();
            await fwmgr.Initialize();

            new Views.MainWindow().Show();
        }

        public static string Version
        {
            get
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                    return ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
                else
                {
                    return "LD " + typeof(App).Assembly.GetName().Version.ToString();
                }
            }
        }

    }
}
