using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.Xml;
using MediaBrowser.Library;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Logging;
using MediaBrowser.Code;
using System.Diagnostics;
using System.Collections;
using Microsoft.MediaCenter;
using MediaBrowser.Util;

namespace MediaBrowser.Library
{
    public class ParentalControl
    {
        public ParentalControl()
        {
            this.Initialize();
        }

        private bool gettingNewPIN = false; // used to signal that we should replace PIN instead of validate
        private Ratings ratings;
        private Timer _relockTimer;
        private DateTime unlockedTime { get; set; }  // time library was unlocked
        private int unlockPeriod { get; set; } //private storage for unlock period
        private string customPIN { get; set; } //local storage for PIN to be checked against
        private List<Folder> enteredProtectedFolders;
        ParentalPromptCompletedCallback pinCallback;
        
        //item and properties to operate upon after pin entered
        private Item anItem;
        protected bool resume;
        protected bool queue;

        public void Initialize()
        {
            // initialize internal settings
            //setup timer for auto re-lock
            //setup timer for auto re-lock - must be done on app thread
            if (!Microsoft.MediaCenter.UI.Application.IsApplicationThread)
                Microsoft.MediaCenter.UI.Application.DeferredInvoke(initTimer);
            else
                initTimer(null);

            // init list of folders we've gained access to
            enteredProtectedFolders = new List<Folder>();
            // construct ratings object
            ratings = new Ratings(Config.Instance.ParentalBlockUnrated);

            Logger.ReportInfo("Parental Control Initialized");
            return;
        }

        private void initTimer(object args)
        {
            _relockTimer = new Timer();
            _relockTimer.Enabled = false; //don't need this until we unlock
            _relockTimer.Interval = 600000; //10 minutes is plenty often enough because re-lock time is in hours
            _relockTimer.Tick += new EventHandler(_relock_Timer_Tick);
        }

        void _relock_Timer_Tick(object sender, EventArgs e)
        {
            if (DateTime.UtcNow >= this.unlockedTime.AddHours(this.unlockPeriod)) this.Relock();
        }

        public void ClearEnteredList()
        {
            //Logger.ReportInfo("Cleared Entered Protected Folder List");
            enteredProtectedFolders.Clear(); //clear out the list
        }

        public bool Enabled
        {
            get
            {
                return (Config.Instance.ParentalControlEnabled);
            }
        }

        public bool Unlocked { get; set; }

        public int MaxAllowed
        {
            get { return Config.Instance.MaxParentalLevel; }
        }

        public string MaxAllowedString
        {
            get
            {
                return ratings.ToString(MaxAllowed) ?? "G"; //return something valid if not there
            }
        }

        private bool addProtectedFolder(FolderModel folder)
        {
            if (folder != null)
            {
                enteredProtectedFolders.Add(folder.Folder);
                return true;
            } else 
                return false;
        }

        public bool ProtectedFolderEntered(Folder folder)
        {
            return enteredProtectedFolders.Contains(folder);
        }

        public bool Allowed(Item item)
        {
            if (this.Enabled && item != null)
            {
                //Logger.ReportInfo("Checking parental status on " + item.Name + " "+item.ParentalRating+" "+this.MaxAllowed.ToString());
                return (ratings.Level(item.ParentalRating) <= this.MaxAllowed);
            }
            else return true;
        }

        public bool Allowed(BaseItem item)
        {
            if (this.Enabled && item != null)
            {
                //Logger.ReportInfo("Checking parental status on " + item.Name + " " + item.ParentalRating + " " + this.MaxAllowed.ToString());
                return (ratings.Level(item.ParentalRating) <= this.MaxAllowed);
            }
            else return true;
        }

        public List<BaseItem> RemoveDisallowed(List<BaseItem> items)
        {
            List<BaseItem> allowedItems = new List<BaseItem>();
            foreach (BaseItem i in items)
            {
                if (this.Allowed(i))
                {
                    allowedItems.Add(i);
                }
                else
                {
                    //Logger.ReportVerbose("Removed Disallowed Item: " + i.Name + ". Rating '" + i.ParentalRating + "' Exceeds Limit of " + this.MaxAllowed.ToString() + ".");
                }
            }
            //Logger.ReportVerbose("Finished Removing PC Items");
            return allowedItems;
        }


        public void StopReLockTimer()
        {
            //called if parental control is turned off - in case the timer was going
            _relockTimer.Stop();
            return;
        }

        public void SwitchUnrated(bool block) {
            ratings.SwitchUnrated(block);
        }


        public void NavigateProtected(FolderModel folder)
        {
            //save parameters where we can get at them after pin entry
            this.anItem = folder;

            //now present pin screen - it will call our callback after finished
            pinCallback = NavPinEntered;
            if (folder.BaseItem.CustomPIN != "" && folder.BaseItem.CustomPIN != null)
                customPIN = folder.BaseItem.CustomPIN; // use custom pin for this item
            else
                customPIN = Config.Instance.ParentalPIN; // use global pin
            Logger.ReportInfo("Request to open protected content "+folder.Name);
            PromptForPin(pinCallback,"Please Enter PIN to View Protected Content");
        }

        public void NavPinEntered(bool pinCorrect)
        {
            MediaCenterEnvironment env = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            if (pinCorrect)
            {
                Logger.ReportInfo("Opening protected content " + anItem.Name);
                //add to list of protected folders we've entered
                addProtectedFolder(anItem as FolderModel);
                Application.CurrentInstance.OpenSecure(anItem as FolderModel);
            }
            else
            {
                env.Dialog("Incorrect PIN Entered", "Content Protected", DialogButtons.Ok, 60, true);
                Logger.ReportInfo("PIN Incorrect attempting to open " + anItem.Name);
                Application.CurrentInstance.Back(); //clear the PIN page
            }
        }

        public void ShuffleProtected(Item folder)
        {
            //save parameters where we can get at them after pin entry
            this.anItem = folder;

            //now present pin screen - it will call our callback after finished
            pinCallback = ShufflePinEntered;
            if (folder.BaseItem.CustomPIN != "" && folder.BaseItem.CustomPIN != null)
                customPIN = folder.BaseItem.CustomPIN; // use custom pin for this item
            else
                customPIN = Config.Instance.ParentalPIN; // use global pin
            Logger.ReportInfo("Request to shuffle protected content " + folder.Name);
            PromptForPin(pinCallback, "Please Enter PIN to Play Protected Content");
        }

        public void ShufflePinEntered(bool pinCorrect)
        {
            Application.CurrentInstance.Back(); //clear the PIN page before playing
            MediaCenterEnvironment env = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            if (pinCorrect)
            {
                Logger.ReportInfo("Shuffling protected content " + anItem.Name);
                //add to list of protected folders we've entered
                addProtectedFolder(anItem as FolderModel);
                Application.CurrentInstance.ShuffleSecure(anItem);
            }
            else
            {
                env.Dialog("Incorrect PIN Entered", "Content Protected", DialogButtons.Ok, 60, true);
                Logger.ReportInfo("PIN Incorrect attempting to shuffle play " + anItem.Name);
            }
        }

        public void PlayUnwatchedProtected(Item folder)
        {
            //save parameters where we can get at them after pin entry
            this.anItem = folder;

            //now present pin screen - it will call our callback after finished
            pinCallback = UnwatchedPinEntered;
            if (folder.BaseItem.CustomPIN != "" && folder.BaseItem.CustomPIN != null)
                customPIN = folder.BaseItem.CustomPIN; // use custom pin for this item
            else
                customPIN = Config.Instance.ParentalPIN; // use global pin
            Logger.ReportInfo("Request to play protected content " + folder.Name);
            PromptForPin(pinCallback, "Please Enter PIN to Play Protected Content");
        }

        public void UnwatchedPinEntered(bool pinCorrect)
        {
            Application.CurrentInstance.Back(); //clear the PIN page before playing
            MediaCenterEnvironment env = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            if (pinCorrect)
            {
                Logger.ReportInfo("Playing protected unwatched content " + anItem.Name);
                //add to list of protected folders we've entered
                addProtectedFolder(anItem as FolderModel);
                Application.CurrentInstance.PlayUnwatchedSecure(anItem);
            }
            else
            {
                env.Dialog("Incorrect PIN Entered", "Content Protected", DialogButtons.Ok, 60, true);
                Logger.ReportInfo("PIN Incorrect attempting to play unwatched in " + anItem.Name);
            }
        }
        public void PlayProtected(Item item, bool resume, bool queue) 
        {
            //save parameters where we can get at them after pin entry
            this.anItem = item;
            this.resume = resume;
            this.queue = queue;

            //now present pin screen - it will call our callback after finished
            pinCallback = PlayPinEntered;
            if (item.BaseItem.CustomPIN != "" && item.BaseItem.CustomPIN != null)
                customPIN = item.BaseItem.CustomPIN; // use custom pin for this item
            else
                customPIN = Config.Instance.ParentalPIN; // use global pin
            Logger.ReportInfo("Request to play protected content");
            PromptForPin(pinCallback,"Please Enter PIN to Play Protected Content");
        }

        public void PlayPinEntered(bool pinCorrect)
        {
            Application.CurrentInstance.Back(); //clear the PIN page before playing
            MediaCenterEnvironment env = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            if (pinCorrect)
            {
                Logger.ReportInfo("Playing protected content "+anItem.Name);
                this.anItem.PlaySecure(resume, queue);
            }
            else
            {
                env.Dialog("Incorrect PIN Entered", "Content Protected", DialogButtons.Ok, 60, true);
                Logger.ReportInfo("Pin Incorrect attempting to play " + anItem.Name);
            }
        }


        public void EnterNewPIN()
        {
            //now present pin screen - it will call our callback after finished
            pinCallback = NewPinEntered;
            customPIN = Config.Instance.ParentalPIN; // use global pin
            Logger.ReportInfo("Request to change PIN");
            PromptForPin(pinCallback, "Please Enter CURRENT PIN.");
        }

        public void NewPinEntered(bool pinCorrect)
        {
            MediaCenterEnvironment env = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            if (pinCorrect)
            {
                Logger.ReportInfo("Entering New PIN");
                gettingNewPIN = true; //set flag
                Application.CurrentInstance.OpenSecurityPage("Please Enter NEW PIN (exactly 4 digits).");
            }
            else
            {
                env.Dialog("Incorrect PIN Entered", "Cannot Change PIN", DialogButtons.Ok, 60, true);
                Logger.ReportInfo("PIN Incorrect attempting change PIN ");
            }
        }

        public void UnlockPinEntered(bool pinCorrect)
        {
            MediaCenterEnvironment env = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            if (pinCorrect)
            {
                Config.Instance.ParentalControlUnlocked = true;
                this.unlockedTime = DateTime.UtcNow;
                this.unlockPeriod = Config.Instance.ParentalUnlockPeriod;
                _relockTimer.Start(); //start our re-lock timer
                env.Dialog("Library Temporarily Unlocked.  Will Re-Lock in "+this.unlockPeriod.ToString()+" Hour(s) or on Application Re-Start", "Unlock", DialogButtons.Ok, 60, true);
                Application.CurrentInstance.Back(); //clear PIN screen
                if (Config.Instance.HideParentalDisAllowed)
                {
                    Application.CurrentInstance.CurrentFolder.RefreshUI();
                    Application.CurrentInstance.RootFolderModel.RefreshUI();
                }
            }
            else
            {
                env.Dialog("Incorrect PIN Entered", "Unlock", DialogButtons.Ok, 60, true);
                Application.CurrentInstance.Back(); //clear PIN screen
                Logger.ReportInfo("PIN Incorrect attempting to unlock library.");
            }
        }

        public void Unlock()
        {
            // just kick off the enter pin page - it will call our function when complete
            pinCallback = UnlockPinEntered;
            customPIN = Config.Instance.ParentalPIN; // use global pin
            Logger.ReportInfo("Request to unlock PC");
            PromptForPin(pinCallback,"Please Enter PIN to Unlock Library");
        }

        private void PromptForPin(ParentalPromptCompletedCallback pe)
        {
            PromptForPin(pe, "");
        }

        private void PromptForPin(ParentalPromptCompletedCallback pe, string prompt)
        {
            gettingNewPIN = false;
            if (!Microsoft.MediaCenter.UI.Application.IsApplicationThread)
                Microsoft.MediaCenter.UI.Application.DeferredInvoke(Application.CurrentInstance.OpenSecurityPage, prompt);
            else
                Application.CurrentInstance.OpenSecurityPage(prompt);
        }
                

        public void CustomPINEntered(string aPIN)
        {
            //Logger.ReportInfo("Custom PIN entered: " + aPIN);
            if (gettingNewPIN)
            {
                gettingNewPIN = false;
                Config.Instance.ParentalPIN = aPIN;
                MediaCenterEnvironment env = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                env.Dialog("PIN Successfully Changed", "PIN Change", DialogButtons.Ok, 60, true);
                Application.CurrentInstance.Back(); //clear PIN entry screen
            }
            else
                pinCallback(aPIN == customPIN);
        }

        public void Relock()
        {
            //MediaCenterEnvironment env = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            Logger.ReportInfo("Library Re-Locked");
            _relockTimer.Stop(); //stop our re-lock timer
            Config.Instance.ParentalControlUnlocked = false;
            if (Config.Instance.HideParentalDisAllowed)
            {
                Application.CurrentInstance.BackToRoot(); //back up to home screen
                Application.CurrentInstance.CurrentFolder.RefreshUI();
            }
            Application.CurrentInstance.Information.AddInformationString("Library Re-Locked"); //and display a message
            //env.Dialog("Library Has Been Re-Locked for Parental Control.", "Unlock Time Expired", DialogButtons.Ok, 60, true);
        }

    }
}
