using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MusicPlugin.Util;
using System.Reflection;
using MusicPlugin.Code.Attributes;

namespace MusicPlugin.Views
{  
    public partial class ConfigureView : Form
    {
        public ConfigureView()
        {
            InitializeComponent();
            this.AutoSize = true;
        }

        static ConfigureView _configureView = null;
        static Dictionary<Control, PropertyInfo> _controlBindings = new Dictionary<Control, PropertyInfo>();

        public static DialogResult BuildUI(TheSettings theSettings)
        {
            Type type = typeof(TheSettings);

            if (_configureView == null)
            {
                _configureView = new ConfigureView();

                foreach (var property in type.GetProperties())
                    BuildControl(_configureView, property, theSettings);

                _configureView.Height = 541;
                _configureView.Margin = new Padding(10);
                _configureView.Padding = new Padding(10);
            }
            else
            {
                foreach (var item in _controlBindings.Keys)
                {
                    if (item is CheckBox)
                        (item as CheckBox).Checked = (bool)_controlBindings[item].GetValue(Settings.Instance, null);
                    else
                        item.Text = (string)_controlBindings[item].GetValue(Settings.Instance, null);
                }
            }

            
            return _configureView.ShowDialog();
        }

        private static void BuildControl(Form form, PropertyInfo property,TheSettings theSettings)
        {
            Control control = null;
            object[] attributes = property.GetCustomAttributes(false);

            MusicPlugin.Code.Attributes.DescriptionAttribute descriptionAttribute = attributes.Select(x => x as MusicPlugin.Code.Attributes.DescriptionAttribute).Where(i => i != null).First(); ;            
            ControlAttribute controlAttribute = attributes.Select(x => x as ControlAttribute).Where(i=>i != null).First();
            HiddenAttribute hiddenAttribute = attributes.Select(x => x as HiddenAttribute).Where(i => i != null).First();
            GroupAttribute groupAttribute = attributes.Select(x => x as GroupAttribute).Where(i => i != null).First();

            if (hiddenAttribute != null)
                if (hiddenAttribute.Hidden)
                    return;

            GroupBox groupBox = null;
            TableLayoutPanel tableLayoutPanel = null;

            if (controlAttribute != null)
            {
                if (form.Controls[groupAttribute.Group + "GroupBoxPanel"] != null)
                {
                    groupBox = (GroupBox)form.Controls[groupAttribute.Group + "GroupBoxPanel"].Controls[groupAttribute.Group + "GroupBox"];
                    tableLayoutPanel = (TableLayoutPanel)groupBox.Controls[0];
                }
                else
                {
                    Panel panel = new Panel() { Padding = new Padding(0,5,0,5), Name = groupAttribute.Group + "GroupBoxPanel", AutoSize = true, Dock = DockStyle.Top };
                    groupBox = new GroupBox() { FlatStyle = FlatStyle.Flat, Text = groupAttribute.Group, Name = groupAttribute.Group + "GroupBox", AutoSize = true, Dock = DockStyle.Fill };
                    panel.Controls.Add(groupBox);
                    tableLayoutPanel = new TableLayoutPanel() { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
                    tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = 45 });
                    tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = 55 });
                    groupBox.Controls.Add(tableLayoutPanel);
                    form.Controls.Add(panel);
                }               
                

                if (controlAttribute.Control.Equals(typeof(CheckBox)))
                {
                    control = new CheckBox() { FlatStyle = FlatStyle.Flat, Checked = (bool)property.GetValue(theSettings, null), Name = property.Name };
                    (control as CheckBox).CheckedChanged += new EventHandler(ConfigureView_Changed);
                }
                else if (controlAttribute.Control.Equals(typeof(FolderTextBox)))
                {
                    control = new FolderTextBox() { Text = (string)property.GetValue(theSettings, null), Name = property.Name };
                    (control as FolderTextBox).TextChanged += new EventHandler(ConfigureView_Changed);
                }
                else if (controlAttribute.Control.Equals(typeof(FileTextBox)))
                {
                    control = new FileTextBox() { Text = (string)property.GetValue(theSettings, null), Name = property.Name };
                    (control as FileTextBox).TextChanged += new EventHandler(ConfigureView_Changed);
                    ExtAttribute extAttribute = attributes.Select(x => x as ExtAttribute).Where(i => i != null).First();
                    (control as FileTextBox).Ext = extAttribute.Ext;
                }
                else if (controlAttribute.Control.Equals(typeof(TextBox)))
                {
                    control = new TextBox() { BorderStyle = BorderStyle.FixedSingle, Text = (string)property.GetValue(theSettings, null), Name = property.Name, Width = 200 };
                    (control as TextBox).TextChanged += new EventHandler(ConfigureView_Changed);
                }
            }
            else
                return;

            tableLayoutPanel.Controls.Add(new Label() { Text = descriptionAttribute.Description, AutoSize = true, Anchor = AnchorStyles.Right });
            tableLayoutPanel.Controls.Add(control);
            _controlBindings.Add(control, property);
        }

        static void ConfigureView_Changed(object sender, EventArgs e)
        {
            if (_controlBindings.Keys.Contains((Control)sender))
            {
                if (_controlBindings[(Control)sender].PropertyType.Equals(typeof(bool)))
                {
                    _controlBindings[(Control)sender].SetValue(Settings.Instance, (sender as CheckBox).Checked, null);
                }              
                else if (_controlBindings[(Control)sender].PropertyType.Equals(typeof(string)))
                {
                    _controlBindings[(Control)sender].SetValue(Settings.Instance, (sender as Control).Text, null);
                }
            }
        }

        private void ConfigureView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                if (!Settings.ValidateNormalLibrary(false) || !Settings.ValidateiTunesLibrary(false) || !Settings.ValidateSettings(Settings.Instance.InitialPath, false))
                {
                    this.DialogResult = DialogResult.Cancel;
                    e.Cancel = true;
                }
            }
            else
                this.DialogResult = DialogResult.Cancel;


        }
    }
}
