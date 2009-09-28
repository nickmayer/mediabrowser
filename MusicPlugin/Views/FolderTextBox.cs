using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MusicPlugin.Views
{
    public partial class FolderTextBox : MusicPlugin.Views.TextBoxCustom
    {
        public FolderTextBox()
        {
            InitializeComponent();
            base.IsFolderDialog = true;
        }
    }
}
