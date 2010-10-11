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

namespace WebProxy {

    public class HttpHeaders {
        public HttpHeaders(string headers) {
            var location = headers.Split('\n')[0].Trim().Split(' ');
            this.Path = location[1];
        }

        public string Path { get; private set; }
    }

    public class ProxyInfo {
        public const string ITunesUserAgent = "QuickTime/7.6.5 (qtver=7.6.5;os=Windows NT 6.1)";

        public ProxyInfo(string host, string path, string userAgent, int port) {
            UserAgent = userAgent;
            Path = path;
            Host = host;
            Port = port;

            LocalFilename = string.Format("{0}{1}{2}", Host, Path, port).GetMD5String() + "_" +
                System.IO.Path.GetFileName(path);

            ContentType = "video/quicktime";
        }

        public string UserAgent { get; private set; }
        public string Host { get; private set; }
        public string Path { get; private set; }

        public string ContentType { get; private set; }

        public int BytesRead { get; set; }
        public bool Completed { get; set; }

        public string LocalFilename { get; private set; }


        public int Port { get; private set; }
    }

    public class HttpProxy {

        const int MAX_CONNECTIONS = 10;

        Dictionary<string, ProxyInfo> proxiedFiles = new Dictionary<string, ProxyInfo>();

        int port;
        string cacheDir;
        Thread listenerThread;
        volatile int incomingConnections = 0;

        public string CacheDirectory
        {
            get { return cacheDir; }
        }

        public HttpProxy(string cacheDir, int port) {
            this.port = port;
            this.cacheDir = cacheDir;
        }

        public bool AlreadyRunning()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    socket.Connect(IPAddress.Loopback, port);
                    socket.Disconnect(true);
                    Logger.ReportInfo("MBTrailer Proxy already running on port "+port);
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

            try {

                foreach (var file in Directory.GetFiles(cacheDir, "*.tmp")) {
                    File.Delete(file);
                }
            } catch {
                // well at least we tried
            }


            listenerThread = new Thread(ThreadProc);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        public string ProxyUrl(string host, string path, string userAgent, int port) {
            ProxyInfo info = new ProxyInfo(host, path, userAgent, port);
            lock (proxiedFiles) {
                proxiedFiles[info.LocalFilename] = info;
            }

            var target = Path.Combine(cacheDir, info.LocalFilename);
            return File.Exists(target) ? target : string.Format("http://localhost:{0}/{1}", this.port, info.LocalFilename);
            
        }

        private void ThreadProc() {
            TcpListener listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start(10);
            while (true) {

                var client = listener.AcceptTcpClient();

                while (incomingConnections >= MAX_CONNECTIONS) {
                    Thread.Sleep(10);
                }
                incomingConnections++;

                ThreadPool.QueueUserWorkItem(_ => ServiceClient(client));
            }
        }

        private void ServiceClient(TcpClient client) {
            string requestedPath = "";
            var stream = client.GetStream();

            try {
                byte[] buffer = new byte[8000];
                StringBuilder data = new StringBuilder();
                bool httpRequestComplete = false;
                while (client.Connected) {
                    if (stream.DataAvailable && !httpRequestComplete) {
                        int bytes_read = stream.Read(buffer, 0, buffer.Length);
                        data.Append(ASCIIEncoding.ASCII.GetString(buffer, 0, bytes_read));

                        if (data.ToString().Contains("\r\n\r\n")) {
                            httpRequestComplete = true;
                        }
                    } else {
                        Thread.Sleep(1);
                    }

                    if (httpRequestComplete) {

                        ProxyInfo info;
                        var headers = new HttpHeaders(data.ToString());
                        lock (proxiedFiles) {
                            requestedPath = headers.Path.Replace("/", "");
                            info = proxiedFiles[requestedPath];
                        }
                        var target = Path.Combine(cacheDir, info.LocalFilename);

                        if (File.Exists(target)) {
                            ServeStaticFile(stream, buffer, info, target);
                            return;
                        }

                        string cacheFile = Path.Combine(cacheDir, info.LocalFilename + ".tmp");

                        if (File.Exists(cacheFile)) {
                            ServeCachedFile(stream, cacheFile, info);
                            return;
                        }

                        try {
                            ProxyRemoteFile(stream, buffer, info, target, cacheFile);
                        } catch (Exception ee) {
                            Logger.ReportException("Failed to proxy file : " + requestedPath, ee);
                        }
                    }
                }
            } catch (Exception e) {
                Logger.ReportException("Failed to serve file : " + requestedPath, e);
            } finally {
                incomingConnections--;

                try
                {
                    stream.Close();
                } 
                catch
                {
                    // well we tried
                }
            }
        }

        private void ProxyRemoteFile(NetworkStream stream, byte[] buffer, ProxyInfo info, string target, string cacheFile) {

            TcpClient proxy = new TcpClient();
            proxy.Connect(info.Host, info.Port);
            var proxyStream = proxy.GetStream();
            var writer = new StreamWriter(proxyStream);

            writer.Write(string.Format("GET {0} HTTP/1.1\r\n", info.Path));
            if (info.UserAgent != null) {
                writer.Write(string.Format("User-Agent: {0}\r\n", info.UserAgent));
            }
            writer.Write(string.Format("Host: {0}\r\n", info.Host));
            writer.Write("Cache-Control: no-cache\r\n");
            writer.Write("\r\n");
            writer.Flush();

            FileStream fs;

            using (fs = new FileStream(cacheFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read)) {

                var bytesRead = proxyStream.Read(buffer, 0, buffer.Length);

                var badStream = false;

                StringBuilder header = new StringBuilder();
                bool gotHeader = false;
                int contentLength = -1;
                int totalRead = 0;
                int headerLength = 0;

                while (bytesRead > 0) {

                    if (!gotHeader)
                    {
                        header.Append(ASCIIEncoding.ASCII.GetString(buffer, 0, bytesRead));
                        string headerString = header.ToString();
                        if (headerString.Contains("\r\n\r\n"))
                        {
                            Trace.WriteLine(headerString);
                            gotHeader = true;
                            foreach (var line in headerString.Split('\n'))
                            {
                                if (line.StartsWith("Content-Length:"))
                                //                   123456789123456
                                {
                                     contentLength = int.Parse(line.Substring(16).Trim());
                                }
                            }
                            Trace.WriteLine(contentLength);

                            headerLength = headerString.IndexOf("\r\n\r\n") + 4;
                        }
                    }
                    

                    lock (info) {
                        info.BytesRead += bytesRead;
                    }
                    totalRead += bytesRead;

                    fs.Write(buffer, 0, bytesRead);

                    try {
                        if (!badStream) {
                            stream.Write(buffer, 0, bytesRead);
                        }
                    } catch {
                        // just read till the end of the file ... our stream was shut.  
                        badStream = true;
                    }

                    var amountToRead = buffer.Length; 
                    if (contentLength > 0)
                    {
                        amountToRead = Math.Min(buffer.Length, (contentLength + headerLength) - totalRead);
                    }
                    if (amountToRead == 0)
                    {
                        break;
                    }
                    bytesRead = proxyStream.Read(buffer, 0, amountToRead);
                }

                // file was completely read ... rename it and strip header
                CommitTempFile(fs, target);

            }

            info.Completed = true;
        }

        private void ServeStaticFile(NetworkStream stream, byte[] buffer, ProxyInfo info, string target) {
            StringBuilder header = new StringBuilder();
            header.Append("HTTP/1.1 200 OK\r\n");
            header.AppendFormat("Content-Length: {0}\r\n", new FileInfo(target).Length);
            header.AppendFormat("Content-Type: {0}\r\n", info.ContentType);
            header.Append("Connection: keep-alive\r\n\r\n");


            var streamWriter = new StreamWriter(stream);
            streamWriter.Write(header.ToString());

            using (var p = File.Open(target, FileMode.Open, FileAccess.Read, FileShare.Read)) {

                while (true) {

                    var bytes_read = p.Read(buffer, 0, buffer.Length);

                    if (bytes_read > 0) {
                        try {
                            stream.Write(buffer, 0, bytes_read);
                        } catch {
                            return;
                        }
                    } else {
                        break;
                    }
                }
            }
        }

        public static void CommitTempFile(FileStream fs, string path) {
            // read first 64k which should be ample and locate the first \r\n\r\n

            fs.Seek(0, SeekOrigin.Begin);

            byte[] buffer = new byte[8000];
            StringBuilder data = new StringBuilder();

            var totalRead = 0;
            var bytesRead = fs.Read(buffer, 0, buffer.Length);
            var headerEnd = 0;
            while (bytesRead > 0 && totalRead < 64 * 1024 && headerEnd == 0) {
                totalRead += bytesRead;
                data.Append(ASCIIEncoding.ASCII.GetString(buffer, 0, bytesRead));
                bytesRead = fs.Read(buffer, 0, buffer.Length);
                headerEnd = data.ToString().IndexOf("\r\n\r\n");
            }

            if (headerEnd > 0) {
                fs.Seek(headerEnd + 4, SeekOrigin.Begin);
            }

            var tempFile = path + ".stmp";

            if (File.Exists(tempFile)) {
                File.Delete(tempFile);
            }

            using (var destination = new FileStream(tempFile, FileMode.CreateNew)) {
                bytesRead = 0;
                do {
                    if (bytesRead > 0) {
                        destination.Write(buffer, 0, bytesRead);
                    }
                    bytesRead = fs.Read(buffer, 0, buffer.Length);

                } while (bytesRead > 0);

            }

            if (File.Exists(path)) {
                File.Delete(path);
            }

            File.Move(tempFile, path);

        }

        public void ServeCachedFile(NetworkStream stream, string filename, ProxyInfo info) {
            byte[] buffer = new byte[8000];
            FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            int retries = 100;
            int totalRead, bytesRead;
            totalRead = bytesRead = fs.Read(buffer, 0, buffer.Length);
            while (true) {
                if (bytesRead == 0) {
                    bool waitLonger = false;
                    lock (info) {
                        waitLonger = info.BytesRead > totalRead;
                    }

                    if (waitLonger && retries-- > 0) {
                        Thread.Sleep(100);
                    } else {
                        break;
                    }

                } else {
                    stream.Write(buffer, 0, bytesRead);
                }

                bytesRead = fs.Read(buffer, 0, buffer.Length);
                totalRead += bytesRead;
            }
        }

        public void Stop() {
        }

        public void CacheUrl(string localUrlPath, string remotePath) {
        }
    }
}
