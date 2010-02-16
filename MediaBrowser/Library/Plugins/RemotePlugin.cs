﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Plugins {
    public class RemotePlugin : IPlugin {

        public void Init(MediaBrowser.Library.Kernel kernel) {
        }

        public string Filename {
            get;
            set;
        }

        public string Name {
            get;
            set;
        }

        public string Description {
            get;
            set;
        }

        public string RichDescURL
        {
            get;
            set;
        }
        public System.Version Version
        {
            get;
            set;
        }

        public System.Version RequiredMBVersion
        {
            get;
            set;
        }

        public System.Version TestedMBVersion
        {
            get;
            set;
        }

        public string BaseUrl
        {
            get;
            set;
        }

        public virtual bool IsConfigurable
        {
            get
            {
                return false;
            }
        }

        public virtual void Configure()
        {
        }

        public virtual bool InstallGlobally
        {
            get;
            set;
        }
    }
}
