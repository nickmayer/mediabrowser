using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Configurator.Code;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Threading;
using System.Threading;
using System.Windows.Threading;

namespace Configurator.Code
{
    public class PluginInstaller
    {

        public void InstallPlugin(IPlugin plugin, System.Windows.Controls.ProgressBar progress, Window window, Delegate done)
        {
            if (plugin != null)
            {
                progress.Visibility = Visibility.Visible;
                FakeProgress(progress, window);
                Async.Queue(() =>
                {
                    PluginManager.Instance.InstallPlugin(plugin);
                },
                () =>
                {
                    StopFakeProgress();
                    window.Dispatcher.Invoke(DispatcherPriority.Background, done);
                });
            }

        }

        ManualResetEvent done = new ManualResetEvent(false);
        ManualResetEvent exited = new ManualResetEvent(false);

        private void StopFakeProgress()
        {
            done.Set();
            while (!exited.WaitOne()) ;
        }

        private void FakeProgress(System.Windows.Controls.ProgressBar progress, Window window)
        {
            Async.Queue(() =>
            {
                int i = 0;
                while (!done.WaitOne(100, false))
                {
                    i += 10;
                    i = i % 100;
                    window.Dispatcher.Invoke(DispatcherPriority.Background, (MethodInvoker)(() => { progress.Value = i; }));
                }
                exited.Set();
            });
        }
    }
}
