using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MusicPlugin.Views
{
    public partial class TextBoxCustom : UserControl
    {
        public TextBoxCustom()
        {
            InitializeComponent();
            CustomTextBox.TextChanged += new EventHandler(CustomTextBox_TextChanged);
        }

        public EventHandler TextChanged;

        void CustomTextBox_TextChanged(object sender, EventArgs e)
        {
            if (TextChanged != null)
                TextChanged.Invoke(this, e);
        }        

        private bool _isFolderDialog = false;
        public bool IsFolderDialog
        {
            set
            { _isFolderDialog = value; }
        }

        public override string Text
        {
            set
            {
                CustomTextBox.Text = value;
            }
            get
            {
                return CustomTextBox.Text;
            }
        }        

        private void CustomButton_Click(object sender, EventArgs e)
        {
            if (!_isFolderDialog)
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.Filter = Ext;
                fileDialog.FileName = CustomTextBox.Text;
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    CustomTextBox.Text = fileDialog.FileName;
                }
            }
            else
            {
                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                folderBrowserDialog.SelectedPath = CustomTextBox.Text;
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    CustomTextBox.Text = folderBrowserDialog.SelectedPath;
                }
            }            
        }

        private string _ext;
        public string Ext
        {
            set
            {
                _ext = value;
            }
            get
            {
                return _ext;
            }
        }
    }
}
