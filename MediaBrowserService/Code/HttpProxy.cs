using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MediaBrowser.Library.Logging;

namespace MediaBrowserService.Code {

    public class HttpHeaders {
        public HttpHeaders(string headers) {
            var location = headers.Split('\n')[0].Trim().Split(' ');
            this.Path = location[1];
        }

        public string Path { get; private set; }
    }


    public class HttpProxy {

        int maxConnections = 10;


        int port;
        Thread listenerThread;
        protected int incomingConnections = 0;

        public HttpProxy(int port, int maxConnections) {
            this.port = port;
            this.maxConnections = maxConnections;
        }


        public bool AlreadyRunning()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    socket.Connect(IPAddress.Loopback, port);
                    // don't disconnect ... its blocking, socket dispose will take care of it
                    Logger.ReportInfo("Proxy already running on port "+port);
                    return true;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        //not there
                        return false;
                    }
                    // some other problem...
                    return false;
                }
            }
        }

        public void Start() {
            Debug.Assert(listenerThread == null);
            if (listenerThread != null) {
                throw new InvalidOperationException("Trying to start an already started server!");
            }

            listenerThread = new Thread(ThreadProc);
            listenerThread.IsBackground = true;
            listenerThread.Start();

        }

        private void ThreadProc()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start(10);
            while (true) {

                var client = listener.AcceptTcpClient();

                while (incomingConnections >= maxConnections) {
                    Thread.Sleep(10);
                }
                
                Interlocked.Increment(ref incomingConnections);

                //Logger.ReportVerbose("Request accepted.  Queueing item.");

                ThreadPool.QueueUserWorkItem(_ => HandleConnection(client));
            }
        }

        private void HandleConnection(TcpClient client)
        {
            try
            {
                ServiceClient(client);
            }
            finally
            {
                client.Close();
                Interlocked.Decrement(ref incomingConnections);
            }
        }

        protected virtual void ServiceClient(TcpClient client)
        {
        }

        public void Stop() {
            listenerThread = null;
        }

    }
}
