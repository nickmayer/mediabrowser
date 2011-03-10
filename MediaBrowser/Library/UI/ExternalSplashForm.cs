﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using MediaBrowser.Library.Threading;
using MediaBrowser.Library.Logging;
using System.Drawing;

namespace MediaBrowser.Library
{
    class ExternalSplashForm
    {
        private delegate void VoidDelegate();
        static Form theForm;

        public static void Display()
        {
            var us = new ExternalSplashForm();
            Async.Queue("Ext Splash Show", () =>
            {
                us.Show();
            });
        }

        public static void Hide() {
            if (theForm != null)
            {
                theForm.Invoke(new VoidDelegate(delegate()
                    {
                        theForm.Close();
                    }));
            }
        }

        void Show()
        {
            try
            {
                theForm = new Form();
                theForm.BackColor = Color.Black;
                theForm.BackgroundImageLayout = ImageLayout.Stretch;
                theForm.BackgroundImage = Config.Instance.Theme == "Black" ? new Bitmap(Resources.splashscreen) : new Bitmap(Resources.splashscreen_blue);
                theForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                theForm.WindowState = System.Windows.Forms.FormWindowState.Maximized;
                Cursor.Hide();
                System.Windows.Forms.Application.Run(theForm);
            }
            catch (Exception e)
            {
                Logger.ReportException("Error showing external player splash form", e);
            }
        }

        

    }
}
