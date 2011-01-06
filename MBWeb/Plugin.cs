using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;
using System.Net;
using System.Reflection;
using MediaBrowser.Web.Framework;
using HttpServer;

namespace MediaBrowser.Web {
    public class Plugin : BasePlugin {

        internal const string PluginName = "Media Browser Web";
        internal const string PluginDescription = "Media Browser Web.\n\nUnrestricted version is available to supporters.";

        private Server server; 

        public static PluginConfiguration<PluginOptions>  PluginOptions {get;set;}
        
        public override void Init(Kernel kernel) {

            

            PluginOptions = new PluginConfiguration<PluginOptions>(kernel, this.GetType().Assembly);
            PluginOptions.Load();
            Logger.ReportInfo("MBWeb (version "+Version+") Plug-in loaded.");

            server = new Server();
            server.Add(HttpServer.HttpListener.Create(IPAddress.Any, 9999));
            var module = new TinyWebModule();
            module.MapPath("/", "/index.html");
            server.Add(module);

            server.Start(5);
        }



        public override PluginInitContext InitDirective
        {
            get
            {
                  return PluginInitContext.Service;
            }
        }

        public override IPluginConfiguration PluginConfiguration {
            get {
                return PluginOptions;
            }
        }

        public override void Configure()
        {
            base.Configure();
        }

        public override string Name {
            get { return PluginName; }
        }

        public override string Description {
            get { return PluginDescription; }
        }

        public override bool IsConfigurable
        {
            get
            {
                return true;
            }
        }

        public override Version TestedMBVersion
        {
            get
            {
                return new Version("2.3.0.0");
            }
        }

        public override Version RequiredMBVersion
        {
            get
            {
                return new Version("2.3.0.0");
            }
        }

    }
}
