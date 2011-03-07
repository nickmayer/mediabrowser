using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Security.AccessControl;
using MediaBrowser.Library;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Threading;
using System.IO.Pipes;
using System.IO;

namespace MediaBrowserService
{
    class CoreCommunications
    {

        private static bool connected = false;
        /// <summary>
        /// Listen from service for commands from core or configurator
        /// </summary>
        public static void StartListening()
        {
            if (connected) return; //only one connection...

            Async.Queue("MB Connection", () =>
            {
                using (NamedPipeServerStream pipe = new NamedPipeServerStream(MBServiceController.MBSERVICE_IN_PIPE, PipeDirection.In))
                {
                    connected = true;
                    bool process = true;
                    while (process)
                    {
                        pipe.WaitForConnection(); //wait for the core to tell us something
                        try
                        {
                            // Read the request from the sender.
                            StreamReader sr = new StreamReader(pipe);

                            string command = sr.ReadLine();
                            switch (command.ToLower())
                            {
                                case IPCCommands.ReloadKernel: //don't use this yet - need to figure out how to unload first do a shutdown and restart
                                    //something changed, we need to re-load everything
                                    Logger.ReportInfo("Re-loading kernel due to request from client.");
                                    Kernel.Init(KernelLoadDirective.ShadowPlugins);
                                    break;
                                case IPCCommands.ReloadConfig:
                                    //re-load the main config file
                                    Kernel.Instance.ReLoadConfig();
                                    break;
                                case IPCCommands.Shutdown:
                                    //close Service
                                    Logger.ReportInfo("Shutting down due to request from client.");
                                    MainWindow.Instance.Shutdown();
                                    break;
                                case IPCCommands.Restart:
                                    //restart Service
                                    Logger.ReportInfo("Restarting due to request from client.");
                                    MainWindow.Instance.Restart();
                                    break;
                                case IPCCommands.CancelRefresh:
                                    //cancel any running refresh
                                    MainWindow.Instance.CancelRefresh();
                                    break;
                                case IPCCommands.ForceRebuild:
                                    //force a re-build of the library - used with new version that requires it
                                    Logger.ReportInfo("Forcing re-build of library due to request from client.");
                                    MainWindow.Instance.ForceRebuild();
                                    break;
                                case IPCCommands.CloseConnection:
                                    //exit this connection
                                    Logger.ReportInfo("Client requested we stop listening.");
                                    process = false;
                                    break;

                            }
                            pipe.Disconnect();
                        }
                        catch (IOException e)
                        {
                            Logger.ReportException("Error in MB connection", e);
                        }
                    }
                    pipe.Close();
                    connected = false;
                }
            });
        }
    }
}
