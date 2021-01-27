﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GostDOC
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            DispatcherHelper.Initialize();
        }
        private void ApplicationStart(object sender, StartupEventArgs e)
        {

        }

        private void ApplicationExit(object sender, ExitEventArgs e)
        {
            DispatcherHelper.Reset();
        }
    }
}