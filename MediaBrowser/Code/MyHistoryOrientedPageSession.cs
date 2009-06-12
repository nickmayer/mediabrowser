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

                breadcrumbs.Push(current);
                
            } else if (breadcrumbs.Count > 0) {
                breadcrumbs.Pop();
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
