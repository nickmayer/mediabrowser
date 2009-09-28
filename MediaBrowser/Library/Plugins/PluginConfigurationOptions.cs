using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using MediaBrowser.Library.Logging;
using MediaBrowser.Attributes;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Windows.Media;

namespace MediaBrowser.Library.Plugins
{

    public class PluginConfigurationOptions 
    {
        public void Reset()
        {
            foreach (var property in this.GetType().GetProperties())
            {
                object[] attributes = property.GetCustomAttributes(false);
                DefaultAttribute defaultAttribute = attributes.Select(x => x as DefaultAttribute).Where(i => i != null).First();

                property.SetValue(this, defaultAttribute.Default, null);

            }

        }
    }
    
    // the new base configuration class 
    public class PluginConfiguration<T> where T : PluginConfigurationOptions
    {
        private string initialPath;
        private string pluginID;
        private string configFile;
        private T _instance;
        public T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (T)Activator.CreateInstance(typeof(T));
                    Save();
                }
                return _instance;
            }
        }
        
        public PluginConfiguration(Kernel kernel,Guid pluginID)
        {
            this.initialPath = Path.Combine(kernel.ConfigData.InitialFolder, @"..\Plugins\Configurations");
            this.pluginID = pluginID.ToString();
            //this.parentName = parent.Name;
            this.configFile = Path.Combine(initialPath, string.Format("{0}.xml", pluginID));
            
        }

        //testing constructor
        public PluginConfiguration(string savePath, string fileName)
        {
            this.initialPath = savePath;
            this.pluginID = fileName;
            this.configFile = Path.Combine(initialPath, string.Format("{0}.xml", pluginID));
        }
        
        public void Save()
        {
            if (string.IsNullOrEmpty(initialPath))
            {
                Logger.ReportError(string.Format("{0} plugin configuration save failed, initial path is empty.", pluginID));
                return;
            }

            if (!Directory.Exists(initialPath))
                Directory.CreateDirectory(initialPath);

            try
            {
                XmlSerializer ser = new XmlSerializer(typeof(T));

                TextWriter writer = new StreamWriter(configFile);
                ser.Serialize(writer, Instance);
                writer.Close();
            }
            catch (Exception e)
            {
                Logger.ReportException(string.Format("{0} plugin configuration save failed.", pluginID), e);
            }
        }

        public void Load()
        {
            if (!File.Exists(configFile))
            {
                Instance.Reset();
                Save();
                return;
            }

            XmlSerializer ser = new XmlSerializer(typeof(T));

            TextReader reader = new StreamReader(configFile);
            _instance = (T)ser.Deserialize(reader);
            reader.Close();
        }        

        //public List<ConfigurationOption> Options { get; set; }

        Window _pluginConfigureView = null;
        Dictionary<Control, PropertyInfo> _controlBindings = new Dictionary<Control, PropertyInfo>();
                
        public bool? BuildUI()
        {            
            Type type = Instance.GetType();

            Grid grid = BuildGrid();

            BuildWindow();
            _pluginConfigureView.Content = (grid);

            foreach (var property in type.GetProperties())
                BuildControl(grid, property, Instance);

            PopulateControls(Instance);

            StackPanel panel = BuildButtonPanel();

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });
            Grid.SetRow(panel, grid.RowDefinitions.Count - 1);
            Grid.SetColumn(panel, 2);

            grid.Children.Add(panel);

            grid.Height = grid.RowDefinitions.Sum(o => o.Height.Value);
            grid.Width = grid.ColumnDefinitions.Sum(o => o.Width.Value);
            _pluginConfigureView.Height = grid.Height + 60;
            _pluginConfigureView.Width = grid.Width + 40;

            return _pluginConfigureView.ShowDialog();
        }

        void PopulateControls(PluginConfigurationOptions pluginConfigurationOptions)
        {
            foreach (var item in _controlBindings.Keys)
            {
                if (item is CheckBox)
                    (item as CheckBox).IsChecked = (bool)_controlBindings[item].GetValue(pluginConfigurationOptions, null);
                else if (item is TextBox)
                    (item as TextBox).Text = (string)_controlBindings[item].GetValue(pluginConfigurationOptions, null);
                else if (item is ComboBox)
                    (item as ComboBox).Text = (string)_controlBindings[item].GetValue(pluginConfigurationOptions, null);
            }
        }

        Grid BuildGrid()
        {
            Grid grid = new Grid() { Margin = new Thickness(10) };

            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(200) });
            //grid.ShowGridLines = true;
            return grid;
        }

        void BuildWindow()
        {
            _pluginConfigureView = new Window();
            _pluginConfigureView.WindowStyle = WindowStyle.ToolWindow;
            _pluginConfigureView.Title = "Plugin Options";
            _pluginConfigureView.ShowInTaskbar = false;
            _pluginConfigureView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _pluginConfigureView.ResizeMode = ResizeMode.NoResize;
            _pluginConfigureView.Icon = null;

            LinearGradientBrush linear = new LinearGradientBrush();
            linear.StartPoint = new Point(0.5, 0);
            linear.EndPoint = new Point(0.5, 1);
            linear.GradientStops.Add(new GradientStop(Color.FromArgb(255, 255, 255, 255), 0.853));
            linear.GradientStops.Add(new GradientStop(Color.FromArgb(255, 202, 192, 192), 1));

            _pluginConfigureView.Background = linear;

        }

        StackPanel BuildButtonPanel()
        {
            StackPanel panel = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Height = 40 };
            Button ok = new Button() { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(5, 10, 5, 0), Content = "OK", Height = 25, Width = 60 };
            Button reset = new Button() { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(5, 10, 5, 0), Content = "Reset", Height = 25, Width = 60 };
            Button cancel = new Button() { IsCancel = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(5, 10, 0, 0), Content = "Cancel", Height = 25, Width = 60 };
            ok.Click += new RoutedEventHandler(ok_Click);
            cancel.Click += new RoutedEventHandler(cancel_Click);
            reset.Click += new RoutedEventHandler(reset_Click);
            panel.Children.Add(ok);
            panel.Children.Add(reset);
            panel.Children.Add(cancel);
            return panel;
        }

        void BuildControl(Grid grid, PropertyInfo property, PluginConfigurationOptions pluginConfigurationOptions)
        {
            Control control = null;
            object[] attributes = property.GetCustomAttributes(false);

            if (attributes == null || attributes.Length == 0)
                return;

            LabelAttribute labelAttribute = attributes.Select(x => x as LabelAttribute).Where(i => i != null).First();
            ControlAttribute controlAttribute = attributes.Select(x => x as ControlAttribute).Where(i => i != null).First();

            if (controlAttribute != null)
            {
                if (controlAttribute.Control.Equals(typeof(CheckBox)))
                {
                    control = new CheckBox() { VerticalAlignment = VerticalAlignment.Center, IsChecked = (bool)property.GetValue(pluginConfigurationOptions, null), Name = property.Name };
                    (control as CheckBox).Checked += new RoutedEventHandler(PluginConfigureView_Checked);
                    (control as CheckBox).Unchecked += new RoutedEventHandler(PluginConfigureView_Checked);
                }
                else if (controlAttribute.Control.Equals(typeof(TextBox)))
                {
                    control = new TextBox() { Margin = new Thickness(0, 2, 0, 2), Text = (string)property.GetValue(pluginConfigurationOptions, null), Name = property.Name, Width = 200 };
                    (control as TextBox).TextChanged += new TextChangedEventHandler(PluginConfigureView_TextChanged);
                }
                else if (controlAttribute.Control.Equals(typeof(ComboBox)))
                {
                    control = new ComboBox() { Margin = new Thickness(0, 2, 0, 2), Name = property.Name, Width = 200 };
                    (control as ComboBox).SelectionChanged += new SelectionChangedEventHandler(PluginConfigureView_SelectionChanged);
                    ItemsAttribute itemsAttribute = attributes.Select(x => x as ItemsAttribute).Where(i => i != null).First();
                    foreach (var item in itemsAttribute.Items.Split(','))
                        (control as ComboBox).Items.Add(item);
                    (control as ComboBox).Text = (string)property.GetValue(pluginConfigurationOptions, null);
                }
            }
            else
                return;

            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(30) });
            Label label = new Label() { Margin = new Thickness(0, 0, 10, 0), HorizontalAlignment = HorizontalAlignment.Right, Content = labelAttribute.Label };
            grid.Children.Add(label);
            Grid.SetColumn(label, 0);
            Grid.SetRow(label, grid.RowDefinitions.Count - 1);
            grid.Children.Add(control);
            Grid.SetColumn(control, 1);
            Grid.SetRow(control, grid.RowDefinitions.Count - 1);
            _controlBindings.Add(control, property);
        }


        #region events
        void PluginConfigureView_Checked(object sender, RoutedEventArgs e)
        {
            ValueChanged(sender);
        }

        void reset_Click(object sender, RoutedEventArgs e)
        {
            Instance.Reset();
            PopulateControls(Instance);
        }

        void PluginConfigureView_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValueChanged(sender);
        }

        void PluginConfigureView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValueChanged(sender);
        }

        void cancel_Click(object sender, RoutedEventArgs e)
        {
            _pluginConfigureView.DialogResult = false;
        }

        void ok_Click(object sender, RoutedEventArgs e)
        {
            _pluginConfigureView.DialogResult = true;
        }

        void ValueChanged(object sender)
        {
            if (_controlBindings.Keys.Contains((Control)sender))
            {
                if (sender is CheckBox)
                {
                    _controlBindings[(Control)sender].SetValue(Instance, (sender as CheckBox).IsChecked, null);
                }
                else if (sender is TextBox)
                {
                    _controlBindings[(Control)sender].SetValue(Instance, (sender as TextBox).Text, null);
                }
                else if (sender is ComboBox)
                {
                    _controlBindings[(Control)sender].SetValue(Instance, (sender as ComboBox).SelectedValue, null);
                }
            }
        }
        #endregion
    }
}
