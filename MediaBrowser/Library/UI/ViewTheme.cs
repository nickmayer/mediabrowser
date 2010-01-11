using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Code.ModelItems;

namespace MediaBrowser.Library.UI
{
    public class ViewTheme
    {
        protected string name = "Default";
        protected string detailPage = "resx://MediaBrowser/MediaBrowser.Resources/MovieDetailsPage";
        protected string folderPage = "resx://MediaBrowser/MediaBrowser.Resources/Page";
        protected string pageArea = "resx://MediaBrowser/MediaBrowser.Resources/PageDefault#Page";
        protected string detailArea = "resx://MediaBrowser/MediaBrowser.Resources/ViewMovieMinimal#ViewMovieMinimal";
        protected string rootLayout = "resx://MediaBrowser/MediaBrowser.Resources/LayoutRoot#LayoutRoot";
        protected object configObject;

        public ViewTheme()
        {
            init(null,null,null,null,null,null,null);
        }

        public ViewTheme(string themeName, string pageAreaRef, string detailAreaRef)
        {
            init(themeName,pageAreaRef,detailAreaRef,null,null,null,null);
        }

        public ViewTheme(string themeName, string pageAreaRef, string detailAreaRef, object config)
        {
            init(themeName, pageAreaRef, detailAreaRef, null, null, null, config);
        }

        public ViewTheme(string themeName, string pageAreaRef, string detailAreaRef, string rootLayoutRef)
        {
            init(themeName, pageAreaRef, detailAreaRef, null, null, rootLayoutRef,null);

        }

        public ViewTheme(string themeName, string pageAreaRef, string detailAreaRef, string folderPageRef, string detailPageRef, string rootLayoutRef )
        {
            init(themeName, pageAreaRef, detailAreaRef, rootLayoutRef, folderPageRef, detailPageRef, null);

        }

        private void init(string themeName, string pageAreaRef, string detailAreaRef, string folderPageRef, string detailPageRef, string rootLayoutRef, object config) {
            if (!String.IsNullOrEmpty(themeName))
                name = themeName;
            if (!String.IsNullOrEmpty(pageAreaRef))
                pageArea = pageAreaRef;
            if (!String.IsNullOrEmpty(detailAreaRef))
                detailArea = detailAreaRef;
            if (!String.IsNullOrEmpty(rootLayoutRef))
                rootLayout = rootLayoutRef;
            if (!String.IsNullOrEmpty(folderPageRef))
                folderPage = folderPageRef;
            if (!String.IsNullOrEmpty(detailPageRef))
                detailPage = detailPageRef;
            configObject = config;
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }
        public string DetailPage
        {
            get { return detailPage; }
            set { detailPage = value; }
        }
        public string PageArea
        {
            get { return pageArea; }
            set { pageArea = value; }
        }
        public string DetailArea
        {
            get { return detailArea; }
            set { detailArea = value; }
        }
        public string FolderPage
        {
            get { return folderPage; }
            set { folderPage = value; }
        }
        public string RootLayout
        {
            get { return rootLayout; }
            set { rootLayout = value; }
        }
        public object Config
        {
            get { return configObject; }
            set { configObject = value; }
        }
        
    }
}
