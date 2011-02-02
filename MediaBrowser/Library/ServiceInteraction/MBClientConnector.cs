using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.AccessControl;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Threading;
using System.IO.Pipes;
using System.IO;

namespace MediaBrowser.Library
{
    class MBClientConnector
    {

        private static bool connected = false;

        public static bool StartListening()
        {
            if (connected) return false; //only one connection...
            if (Application.RunningOnExtender)
            {
                Logger.ReportInfo("Running on an extender.  Not starting client listener.");
                return true; //no comms for extenders
            }

            NamedPipeServerStream pipe;
            try
            {
                pipe = new NamedPipeServerStream(Kernel.MBCLIENT_MUTEX_ID);
            }
            catch (IOException)
            {
                Logger.ReportInfo("Client listener already going - activating that instance of MB...");
                //already started - must be another instance of MB Core - tell it to come to front
                string entryPoint = EntryPointResolver.EntryPointPath;
                if (string.IsNullOrEmpty(entryPoint))
                {
                    SendCommandToCore("activate");
                }
                else //nav to the proper entrypoint
                {
                    Logger.ReportInfo("Navigating current instance to entrypoint " + entryPoint);
                    SendCommandToCore("activateentrypoint," + entryPoint);
                }
                //and exit
                return false;
            }

            connected = true;

            Async.Queue("MBClient Listener", () =>
            {

                bool process = true;
                while (process)
                {
                    pipe.WaitForConnection(); //wait for someone to tell us something

                    // Read the request from the client. 
                    StreamReader sr = new StreamReader(pipe);

                    string[] commandAndArgs = sr.ReadLine().Split(',');
                    string command = commandAndArgs[0];
                    switch (command.ToLower())
                    {
                        case "play":
                            //request to play something - our argument will be the GUID of the item to play
                            Guid id = new Guid(commandAndArgs[1]);
                            Logger.ReportInfo("Playing ...");
                            //to be implemented...
                            break;
                        case "activateentrypoint":
                            //re-load ourselves and nav to the entrypoint
                            Kernel.Instance.ReLoadRoot();
                            Microsoft.MediaCenter.UI.Application.DeferredInvoke(_ =>
                            {
                                MediaBrowser.Application.CurrentInstance.LaunchEntryPoint(commandAndArgs[1]);
                            });
                            //and tell MC to navigate to us
                            Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.ReturnToApplication();
                            break;
                        case "activate":
                            //if we were in an entrypoint and we just got told to activate - we need to re-load and go to real root
                            if (Application.CurrentInstance.IsInEntryPoint)
                            {
                                Kernel.Instance.ReLoadRoot();
                                Microsoft.MediaCenter.UI.Application.DeferredInvoke(_ =>
                                {
                                    MediaBrowser.Application.CurrentInstance.LaunchEntryPoint(""); //this will start at root
                                });
                            }

                            //tell MC to navigate to us
                            Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.ReturnToApplication();
                            break;
                        case "shutdown":
                            //close MB
                            Logger.ReportInfo("Shutting down due to request from a client (possibly new instance of MB).");
                            Application.CurrentInstance.Close();
                            break;
                        case "closeconnection":
                            //exit this connection
                            Logger.ReportInfo("Service requested we stop listening.");
                            process = false;
                            break;

                    }
                    pipe.Disconnect();
                }
                pipe.Close();
                connected = false;
            });
            return true;
        }

        public static bool SendCommandToCore(string command)
        {
            return SendCommandToCore("localhost", command);
        }

        public static bool SendCommandToCore(string machine, string command)
        {
            NamedPipeClientStream pipeClient =
                new NamedPipeClientStream(machine, Kernel.MBCLIENT_MUTEX_ID,
                PipeDirection.Out, PipeOptions.None);
            StreamWriter sw = new StreamWriter(pipeClient);
            try
            {
                pipeClient.Connect(2000);
            }
            catch (TimeoutException)
            {
                Logger.ReportWarning("Unable to send command to core (may not be running).");
                return false;
            }
            try
            {
                sw.AutoFlush = true;
                sw.WriteLine(command);
                pipeClient.Close();
            }
            catch (Exception e)
            {
                Logger.ReportException("Error sending commmand to core", e);
                return false;
            }
            return true;
        }
    }
}
