using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MediaCenter.Hosting;
using MediaBrowser.Library;
using MediaBrowser.Library.Util;

namespace MediaBrowser
{
    public class MyHistoryOrientedPageSession : HistoryOrientedPageSession
    {

        Application myApp;
        BreadCrumbs breadcrumbs = new BreadCrumbs(Config.Instance.BreadcrumbCountLimit); 

        public Application Application
        {
            get { return myApp; }
            set { myApp = value; }
        }


        public void AddBreadcrumb(string breadcrumb) {
            breadcrumbs.Push(breadcrumb,true);
        } 

        protected override void LoadPage(object target, string source, IDictionary<string, object> sourceData, IDictionary<string, object> uiProperties, bool navigateForward)
        {
            this.Application.NavigatingForward = navigateForward;
            if (navigateForward)
            {
                string current = "";

                if (breadcrumbs.Count == 0) {
                    current = Config.Instance.InitialBreadcrumbName;
                }
                else if ((uiProperties != null) && (uiProperties.ContainsKey("Item")))
                {
                    current = ((Item)uiProperties["Item"]).Name;
                } 
                else if ((uiProperties != null) && (uiProperties.ContainsKey("Folder"))) {
                    current = ((FolderModel)uiProperties["Folder"]).Name;
                }

                //check to see if we are going to the PIN page
                else if (source == "resx://MediaBrowser/MediaBrowser.Resources/ParentalPINEntry")
                {
                    //put special breadcrumb in that we will not show
                    current = "PINENTRY";
                }
                breadcrumbs.Push(current);
                
            } else {
                if (breadcrumbs.Count > 0)
                {
                    breadcrumbs.Pop();
                }
                //clear out the protected folder list each time we go back to the root
                if ((uiProperties != null) && (uiProperties.ContainsKey("Folder")))
                {
                    Application.CurrentInstance.CurrentFolder = uiProperties["Folder"] as FolderModel; //keep track of current folder on back
                    if (((FolderModel)uiProperties["Folder"]).IsRoot) {
                        //we're backing into the root folder - clear the protected folder list
                        Kernel.Instance.ClearProtectedAllowedList();
                    }
                }
            }
            
            base.LoadPage(target, source, sourceData, uiProperties, navigateForward);
        }


        public string Breadcrumbs
        {
            get
            {
                return breadcrumbs.ToString();
            }
        }
    }
}
