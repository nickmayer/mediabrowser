using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Controls;
using MediaBrowser.Library.Logging;
using System.Windows.Data;
using System.Globalization;

namespace LogViewer {
    class RowBackgroundHighlighter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {

            Brush brush = Brushes.White;

            var item = (ListViewItem)value;
            ListView listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
            int index = listView.ItemContainerGenerator.IndexFromContainer(item);

            
            if (!(listView.Items[index] is LogRow)) return Brushes.White;

            var row = (LogRow)listView.Items[index];
            var messages = listView.ItemsSource as LogMessages;
    
            if (messages.IsHighlighted(row)) {
                brush = Brushes.Yellow;
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return null;
        }

        #endregion
    }
}
