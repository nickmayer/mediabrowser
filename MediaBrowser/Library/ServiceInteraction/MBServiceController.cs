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
    public class MBServiceController
    {
        private static bool connected = false;

        public static void ConnectToService()
        {
            if (connected) return; //only one connection...

            Async.Queue("MBService Connection", () =>
            {
                using (NamedPipeServerStream pipe = new NamedPipeServerStream(Kernel.MBSERVICE_MUTEX_ID,PipeDirection.In))
                {
                    connected = true;
                    bool process = true;
                    while (process)
                    {
                        pipe.WaitForConnection(); //wait for the service to tell us something
                        try
                        {
                            // Read the request from the client. Once the client has
                            // written to the pipe its security token will be available.
                            StreamReader sr = new StreamReader(pipe);

                            // Obtain the filename from the connected client.
                            string command = sr.ReadLine();
                            switch (command.ToLower())
                            {
                                case "reload":
                                    //refresh just finished, we need to re-load everything
                                    Logger.ReportInfo("Re-loading due to request from service.");
                                    Application.CurrentInstance.ReLoad();
                                    break;
                                case "shutdown":
                                    //close MB
                                    Logger.ReportInfo("Shutting down due to request from service.");
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
                        catch (IOException e)
                        {
                            Logger.ReportException("Error in MBService connection", e);
                        }
                    }
                    pipe.Close();
                    connected = false;
                }
            });
        }

        public static bool SendCommandToCore(string command)
        {
            NamedPipeClientStream pipeClient =
                new NamedPipeClientStream("localhost", Kernel.MBSERVICE_MUTEX_ID,
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

        public static bool IsRunning
        {
            get
            {
                using (Mutex mutex = new Mutex(false, Kernel.MBSERVICE_MUTEX_ID))
                {
                    //set up so everyone can access
                    var allowEveryoneRule = new MutexAccessRule("Everyone", MutexRights.FullControl, AccessControlType.Allow);
                    var securitySettings = new MutexSecurity();
                    securitySettings.AddAccessRule(allowEveryoneRule);
                    mutex.SetAccessControl(securitySettings);
                    try
                    {
                        return !(mutex.WaitOne(5000, false));
                    }
                    catch (AbandonedMutexException)
                    {
                        // Log the fact the mutex was abandoned in another process, it will still get acquired
                        Logger.ReportWarning("Previous instance of service ended abnormally...");
                        mutex.ReleaseMutex();
                        return false;
                    }
                }
            }
        }

        public static bool StartService()
        {
            try
            {
                Logger.ReportInfo("Starting Service: " + ApplicationPaths.ServiceExecutableFile);
                System.Diagnostics.Process.Start(ApplicationPaths.ServiceExecutableFile);
            }
            catch (Exception e)
            {
                Logger.ReportError("Error attempting to start service. " + e.Message);
                return false;
            }
            return true;
        }
        public static bool StopService()
        {
            throw new NotImplementedException();
        }
    }
}
