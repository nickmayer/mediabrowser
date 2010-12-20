using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.AccessControl;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Configuration;

namespace MediaBrowser.Library
{
    public class MBServiceController
    {
        public bool Connect()
        {
            return Connect(null);
        }

        public bool Connect(string machineName)
        {
            throw new NotImplementedException();
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
