using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Configurator.Code;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Threading;
using System.Windows.Forms;
using System.Threading;
using System.Windows.Threading;

namespace Configurator {
    /// <summary>
    /// Interaction logic for AddPluginWindow.xaml
    /// </summary>
    public partial class AddPluginWindow : Window {
        public AddPluginWindow() {
            InitializeComponent();
            progress.Minimum = 0;
            progress.Maximum = 100;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e) {
            PluginSourcesWindow window = new PluginSourcesWindow();
            window.ShowDialog();
        }

        private void InstallClick(object sender, RoutedEventArgs e) {
            InstallButton.IsEnabled = false;
            this.progress.Visibility = Visibility.Visible;
            FakeProgress();
            IPlugin plugin = pluginList.SelectedItem as IPlugin;
            Async.Queue( () => {
                PluginManager.Instance.InstallPlugin(plugin); 
            },
            () => {
                StopFakeProgress();
                Dispatcher.Invoke(DispatcherPriority.Background,(MethodInvoker)this.Close); 
            });

        }

        ManualResetEvent done = new ManualResetEvent(false);
        ManualResetEvent exited = new ManualResetEvent(false);

        private void StopFakeProgress() {
            done.Set();
            while (!exited.WaitOne()) ; 
        }

        private void FakeProgress() {
            Async.Queue(() => {
                int i = 0;
                while (!done.WaitOne(100,false)) {
                    i += 10;
                    i = i % 100;
                    Dispatcher.Invoke(DispatcherPriority.Background, (MethodInvoker)(() => { progress.Value = i; }));
                }
                exited.Set();
            });
        }
    }
}
