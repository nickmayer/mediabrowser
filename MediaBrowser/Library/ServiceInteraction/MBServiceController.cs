using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library
{
    public class MBServiceController
    {
        public const string MBSERVICE_NAME = "MediaBrowserService";
        private string machineName;
        protected ServiceController myController;

        public bool Connect()
        {
            return Connect(null);
        }

        public bool Connect(string machineName)
        {
            this.machineName = machineName;
            try
            {
                if (machineName == null)
                    myController = new ServiceController(MBSERVICE_NAME);
                else
                    myController = new ServiceController(machineName, MBSERVICE_NAME);

                var ignore = myController.Status;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public string Status
        {
            get
            {
                try
                {
                    if (myController != null)
                    {
                        return myController.Status.ToString();
                    }
                    return "Unknown";
                }
                catch
                {
                    return "Unknown";
                }
            }
        }

        public bool StartService()
        {
            try
            {
                myController.Start();
                myController.WaitForStatus(ServiceControllerStatus.Running,TimeSpan.FromSeconds(5));
            }
            catch (Exception e)
            {
                Logger.ReportError("Error attempting to start service. " + e.Message);
                return false;
            }
            return true;
        }
        public bool StopService()
        {
            try
            {
                myController.Stop();
                myController.WaitForStatus(ServiceControllerStatus.Stopped,TimeSpan.FromSeconds(5));
            }
            catch (Exception e)
            {
                Logger.ReportError("Error attempting to stop service. " + e.Message);
                return false;
            }
            return true;
        }
    }
}
