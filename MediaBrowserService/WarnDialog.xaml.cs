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

namespace MediaBrowserService
{
    /// <summary>
    /// Interaction logic for WarnDialog.xaml
    /// </summary>
    public partial class WarnDialog : Window
    {
        public WarnDialog()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public static bool Show(string msg)
        {
            return Show(msg, true);
        }

        public static bool Show(string msg, bool allowDontShow)
        {
            WarnDialog dlg = new WarnDialog();
            dlg.tbMessage.Text = msg;
            dlg.cbxDontShowAgain.Visibility = allowDontShow ? Visibility.Visible : Visibility.Hidden;
            dlg.ShowDialog();
            return dlg.cbxDontShowAgain.IsChecked.Value;
        }
    }
}
