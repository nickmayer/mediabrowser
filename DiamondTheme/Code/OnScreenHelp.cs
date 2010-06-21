using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MediaBrowser;
using System.Timers;

namespace Diamond
{
    internal class OnScreenHelp
    {
        string[] Help = {   "Change the display settings by pressing on the 'gears' icon.", 
                            "View the last 20 recently added media items on the home screen. (EHS)", 
                            "Pressing '*' on your remote launches the new Item Menu.", 
                            "You can add more plugins through the configurator application.", 
                            "If you are experiencing slowness, try turning off fan art [Tier 1].",
                            "MediaInfo plugin adds details on resolution, codecs, and more.",
                            "You can turn off help notifications in diamond config."                       
                        };
       
        private const int CycleInterval = 30000;
        Timer cycle;
        int counter = 0;
        int maxMessages = 0;

        public OnScreenHelp()
        { 
            maxMessages = Help.Length;
            Begin();
        }

        private void Begin()
        {
            if (Help.Length > 0)
            {
                DisplayItem();

                cycle = new Timer(CycleInterval);
                cycle.Elapsed += delegate { OnRefresh(); };
                cycle.Enabled = true;
            }            
        }

        private void OnRefresh()
        {
            if (counter > maxMessages)
            {
                cycle.Enabled = false;
            }
            else
            {
                DisplayItem();
            }
            counter++;
        }

        private void DisplayItem()
        {
            Random r = new Random();
            int index = r.Next(0, maxMessages - 1);
            string msg = string.Format("{0}. {1}", index, Help[index]);
            Application.CurrentInstance.Information.AddInformation(new InfomationItem(msg, 2));
        }

    }
}
