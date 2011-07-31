using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace MediaBrowserService
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string[] Args;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //get command line args
            Args = e.Args;
        }
    }
}
