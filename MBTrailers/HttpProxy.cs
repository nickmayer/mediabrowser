using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Runtime.Serialization;
using WebProxy.WCFInterfaces;
using MediaBrowser.Library.Logging;

namespace WebProxy {

    public class HttpHeaders {
        public HttpHeaders(string headers) {
            var location = headers.Split('\n')[0].Trim().Split(' ');
            this.Path = location[1];
        }

        public string Path { get; private set; }
    }


    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults=true)]
    public class ProxyService : ITrailerProxy
    {
        
        Dictionary<string, ProxyInfo> proxiedFiles = new Dictionary<string, ProxyInfo>();
        Dictionary<string, TrailerInfo> myTrailers = new Dictionary<string, TrailerInfo>();
        Dictionary<string, TrailerInfo> mbTrailers = new Dictionary<string, TrailerInfo>();

        MediaBrowser.Library.Ratings ratings = new MediaBrowser.Library.Ratings();

        string cacheDir = "";
        int port = 8752;

        #region ITrailerProxy Members

        public void Init(string cacheDir, int port)
        {
            this.cacheDir = cacheDir;
            this.port = port;
        }


        public ProxyInfo GetProxyInfo(string key)
        {
            if (proxiedFiles.ContainsKey(key))
                return proxiedFiles[key];
            else
                return null;
        }

        public void SetProxyInfo(ProxyInfo info)
        {
            proxiedFiles[info.LocalFilename] = info;
        }

        public void SetTrailerInfo(TrailerInfo info)
        {
            if (info != null)
                switch (info.Type) {
                    case TrailerType.Remote:
                        //convert remote paths to full uri if not cached
                        string key = info.Path;
                        string target = Path.Combine(cacheDir, info.Path);
                        info.Path = File.Exists(target) ? target : string.Format("http://localhost:{0}/{1}", this.port, key);
                        mbTrailers[key] = info;
                        break;
                    case TrailerType.Local:
                        myTrailers[info.Path] = info;
                        break;
                }
        }

        public List<string> GetMatchingTrailers(TrailerInfo searchInfo, float threshhold)
        {
            List<TrailerInfo> trailers = searchInfo.Type == TrailerType.Remote ? 
                trailers = mbTrailers.Count > 0 ? mbTrailers.Values.ToList() : new List<TrailerInfo>() :
                trailers = myTrailers.Count > 0 ? myTrailers.Values.ToList() : new List<TrailerInfo>();
            List<string> foundTrailers = new List<string>();
            if (string.IsNullOrEmpty(searchInfo.Rating) && searchInfo.Genres == null)
            {
                // no search info - return all
                foreach (var info in trailers)
                {
                    if (!info.Path.StartsWith(searchInfo.Path)) foundTrailers.Add(info.Path);
                }
            }
            else
            {
                Logger.ReportVerbose("Searching for matches.  Rating: " + searchInfo.Rating);
                foreach (var genre in searchInfo.Genres)
                {
                    Logger.ReportVerbose("Genre: " + genre);
                }
                Logger.ReportVerbose(trailers.Count + " " + searchInfo.Type + " trailers being searched.");
                foreach (var info in trailers)
                {
                    if (info == null || info.Path == null)
                    {
                        Logger.ReportWarning("MBTrailers - Null item in trailer list...");
                        continue;
                    }
                    if (!string.IsNullOrEmpty(searchInfo.Rating) && ratings.Level(info.Rating) <= ratings.Level(searchInfo.Rating) && GenreMatches(searchInfo, info, threshhold) && !info.Path.StartsWith(searchInfo.Path))
                    {
                        Logger.ReportVerbose("MATCH FOUND: "+info.Path + " Rating: " + info.Rating);
                        foundTrailers.Add(info.Path);
                    }
                    else
                    {
                        Logger.ReportVerbose(info.Path + " doesn't match.  Rating: " + info.Rating);
                        if (info.Genres != null)
                            foreach (var genre in info.Genres)
                            {
                                Logger.ReportVerbose("Genre: " + genre);
                            }
                    }
                }
            }
            Logger.ReportVerbose("Found " + foundTrailers.Count + " trailers.  Returning...");
            return foundTrailers;
        }

        private bool GenreMatches(TrailerInfo searchInfo, TrailerInfo info, float threshhold)
        {
            if (searchInfo.Genres.Count == 0) return true; //no search genres - everything matches
            if (info.Genres == null || info.Genres.Count == 0) return false; //no target genres - no match

            float matches = 0;
            foreach (var genre in searchInfo.Genres){
                if (info.Genres.Contains(genre))
                    matches++;
            }
            return (matches / searchInfo.Genres.Count) > threshhold;
        }
 
        public List<ProxyInfo> GetProxiedFileList()
        {
            return proxiedFiles.Values.ToList();
        }

        public List<TrailerInfo> GetTrailerList()
        {
            return myTrailers.Values.ToList();
        }

        #endregion
    }

    public class HttpProxy {

        const int MAX_CONNECTIONS = 10;


        int port;
        string cacheDir;
        Thread listenerThread;
        int incomingConnections = 0;

        Thread backgroundDownloader;
        Timer downloadGoverner;
        long maxBandwidth = 0;
        DateTime lastAutoDownload = DateTime.MinValue;
        bool downloadInProgress = false;

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
                    // don't disconnect ... its blocking, socket dispose will take care of it
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

        private void DownloadProcGoverner()
        {
            //this thread will re-kick off the download every day
            Thread.Sleep(60000); //wait for proxy info to get filled in
            if (!downloadInProgress && DateTime.Now.Date > lastAutoDownload.Date)
            {
                backgroundDownloader = new Thread(DownloadProc);
                backgroundDownloader.IsBackground = true;
                backgroundDownloader.Start();
            }

        }

        private void DownloadProc()
        {
            //background thread to download all the trailers - with throttling
            downloadInProgress = true;
            Logger.ReportInfo("MBTrailers Starting process to background download all trailers.  Max Bandwidth: " + maxBandwidth);
            List<ProxyInfo> proxiedFiles = new List<ProxyInfo>();
            using (ChannelFactory<ITrailerProxy> factory = new ChannelFactory<ITrailerProxy>(new NetNamedPipeBinding(), "net.pipe://localhost/mbtrailers"))
            {
                ITrailerProxy proxyServer = factory.CreateChannel();
                try
                {
                    proxiedFiles = proxyServer.GetProxiedFileList();
                }
                catch (Exception e)
                {
                    Logger.ReportException("Error getting proxy files in background downloader", e);
                    Logger.ReportError("Inner Exception: " + e.InnerException.Message);
                }
                finally
                {
                    (proxyServer as ICommunicationObject).Close();
                }
            }
            string target;
            foreach (var info in proxiedFiles)
            {
                target = Path.Combine(cacheDir, info.LocalFilename);

                if (File.Exists(target + ".tmp")) continue; //already downloaded or downloading
                if (File.Exists(target)) continue; //already downloaded and committed

                //none of the local copies exist - go get it
                Logger.ReportInfo("MBTrailers Background downloading file: " + target + " Bytes per Second: "+maxBandwidth);

                TcpClient server = new TcpClient();
                server.Connect(info.Host, info.Port);
                
                var serverStream = server.GetStream();
                var writer = new StreamWriter(serverStream);

                writer.Write(string.Format("GET {0} HTTP/1.1\r\n", info.Path));
                if (info.UserAgent != null)
                {
                    writer.Write(string.Format("User-Agent: {0}\r\n", info.UserAgent));
                }
                writer.Write(string.Format("Host: {0}\r\n", info.Host));
                writer.Write("Cache-Control: no-cache\r\n");
                writer.Write("\r\n");
                writer.Flush();



                FileStream fs;
                long bufferSize = maxBandwidth > 8000 ? 8000 : maxBandwidth;
                byte[] buffer = new byte[bufferSize];

                using (fs = new FileStream(target + ".tmp", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read))
                {

                    var bytesRead = serverStream.Read(buffer, 0, buffer.Length);
                    long readThisSecond = bytesRead;


                    StringBuilder header = new StringBuilder();
                    bool gotHeader = false;
                    int contentLength = -1;
                    int totalRead = 0;
                    int headerLength = 0;
                    DateTime start = DateTime.Now;

                    while (bytesRead > 0)
                    {
                        if (!gotHeader)
                        {
                            header.Append(ASCIIEncoding.ASCII.GetString(buffer, 0, bytesRead));
                            string headerString = header.ToString();
                            if (headerString.Contains("\r\n\r\n"))
                            {
                                gotHeader = true;
                                foreach (var line in headerString.Split('\n'))
                                {
                                    if (line.StartsWith("Content-Length:"))
                                    //                   123456789123456
                                    {
                                        contentLength = int.Parse(line.Substring(16).Trim());
                                        //Logger.ReportInfo("Content Length " + contentLength);
                                    }
                                }

                                headerLength = headerString.IndexOf("\r\n\r\n") + 4;
                            }
                        }


                        lock (info)
                        {
                            info.BytesRead += bytesRead;
                        }
                        totalRead += bytesRead;

                        fs.Write(buffer, 0, bytesRead);

                        var amountToRead = buffer.Length;
                        if (contentLength > 0)
                        {
                            amountToRead = Math.Min(buffer.Length, (contentLength + headerLength) - totalRead);
                        }
                        if (amountToRead == 0)
                        {
                            break;
                        }
                        if (readThisSecond >= maxBandwidth)
                        {
                            var sleepTime = (int)(1000 - (DateTime.Now - start).TotalMilliseconds);
                            if (sleepTime > 0)
                            {
                                //Logger.ReportInfo(readThisSecond+"bytes read. Throttling... for " + sleepTime + "ms");
                                Thread.Sleep(sleepTime); //poor-man's throttling...
                            }
                            readThisSecond = 0;
                            start = DateTime.Now;
                        }
                        bytesRead = serverStream.Read(buffer, 0, amountToRead);
                        readThisSecond += bytesRead;
                    }

                    Logger.ReportInfo("Finished Downloading " + target);
                    // file was completely read ... rename it and strip header
                    CommitTempFile(fs, target);

                }
            }
            downloadInProgress = false;
            lastAutoDownload = DateTime.Now;
        }

        public void Start()
        {
            Start(false, 0);
        }

        public void Start(bool backgroundDL, long maxBW) {
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

            //start our background downloader if asked
            if (backgroundDL)
            {
                maxBandwidth = maxBW > 0 ? maxBW : 1000 * 1024;
                downloadGoverner = MediaBrowser.Library.Threading.Async.Every(3600000, () => DownloadProcGoverner());
            }
        }

        public string ProxyUrl(MBTrailers.ITunesTrailer trailer) 
        {
            Uri uri = new Uri(trailer.RealPath);
            ProxyInfo proxyInfo = new ProxyInfo(uri.Host, uri.PathAndQuery, ProxyInfo.ITunesUserAgent, uri.Port);
            TrailerInfo trailerInfo = new TrailerInfo(TrailerType.Remote, proxyInfo.LocalFilename, trailer.ParentalRating, trailer.Genres);
            using (ChannelFactory<ITrailerProxy> factory = new ChannelFactory<ITrailerProxy>(new NetNamedPipeBinding(), "net.pipe://localhost/mbtrailers"))
            {
                ITrailerProxy proxyServer = factory.CreateChannel();
                try
                {
                    proxyServer.SetProxyInfo(proxyInfo);
                    proxyServer.SetTrailerInfo(trailerInfo);
                }
                catch (Exception e)
                {
                    Logger.ReportException("Error setting proxy info", e);
                    Logger.ReportError("Inner Exception: " + e.InnerException.Message);
                }
                finally
                {
                    (proxyServer as ICommunicationObject).Close();
                }
            }

            var target = Path.Combine(cacheDir, proxyInfo.LocalFilename);
            return File.Exists(target) ? target : string.Format("http://localhost:{0}/{1}", this.port, proxyInfo.LocalFilename);

        }

        public void SetTrailerInfo(MediaBrowser.Library.Entities.Show trailer)
        {
            TrailerInfo trailerInfo = new TrailerInfo(TrailerType.Local, trailer.Path.ToLower(), trailer.ParentalRating, trailer.Genres);
            using (ChannelFactory<ITrailerProxy> factory = new ChannelFactory<ITrailerProxy>(new NetNamedPipeBinding(), "net.pipe://localhost/mbtrailers"))
            {
                ITrailerProxy proxyServer = factory.CreateChannel();
                try
                {
                    proxyServer.SetTrailerInfo(trailerInfo);
                }
                catch (Exception e)
                {
                    Logger.ReportException("Error setting trailer info", e);
                    Logger.ReportError("Inner Exception: " + e.InnerException.Message);
                }
                finally
                {
                    (proxyServer as ICommunicationObject).Close();
                }
            }
        }

        public List<TrailerInfo> GetTrailers() {
            List<TrailerInfo> list = new List<TrailerInfo>();
            using (ChannelFactory<ITrailerProxy> factory = new ChannelFactory<ITrailerProxy>(new NetNamedPipeBinding(), "net.pipe://localhost/mbtrailers"))
            {
                ITrailerProxy proxyServer = factory.CreateChannel();
                try
                {
                    list = proxyServer.GetTrailerList();
                }
                catch (Exception e)
                {
                    Logger.ReportException("Error setting trailer info", e);
                    Logger.ReportError("Inner Exception: " + e.InnerException.Message);
                }
                finally
                {
                    (proxyServer as ICommunicationObject).Close();
                }
            }
            return list;
        }


        private void ThreadProc()
        {
            //start our wcf service
            ServiceHost host = new ServiceHost(typeof(ProxyService));
            host.AddServiceEndpoint(typeof(ITrailerProxy), new NetNamedPipeBinding(), "net.pipe://localhost/MBTrailers");
            host.Open();
            //and initialize it
            using (ChannelFactory<ITrailerProxy> factory = new ChannelFactory<ITrailerProxy>(new NetNamedPipeBinding(), "net.pipe://localhost/mbtrailers"))
            {
                ITrailerProxy proxyServer = factory.CreateChannel();
                proxyServer.Init(this.cacheDir, this.port);
                (proxyServer as ICommunicationObject).Close();
            }

            TcpListener listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start(10);
            while (true) {

                var client = listener.AcceptTcpClient();

                while (incomingConnections >= MAX_CONNECTIONS) {
                    Thread.Sleep(10);
                }
                
                Interlocked.Increment(ref incomingConnections);

                Logger.ReportInfo("Request accepted.  Queueing item.");

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
                            Logger.ReportInfo("Request complete");
                            Logger.ReportInfo("Data were: " + data.ToString());
                            httpRequestComplete = true;
                        }
                    } else {
                        Thread.Sleep(1);
                    }

                    if (httpRequestComplete) {

                        ProxyInfo info;
                        var headers = new HttpHeaders(data.ToString());
                        requestedPath = headers.Path.Replace("/", "");
                        using (ChannelFactory<ITrailerProxy> factory = new ChannelFactory<ITrailerProxy>(new NetNamedPipeBinding(), "net.pipe://localhost/mbtrailers"))
                        {
                            ITrailerProxy proxyServer = factory.CreateChannel();
                            info = proxyServer.GetProxyInfo(requestedPath);
                            (proxyServer as ICommunicationObject).Close();
                        }
                        if (info == null)
                        {
                            //probably a request from the player for art - ignore it
                            //Logger.ReportError("Unable to get info for item: " + requestedPath);
                            break;
                        }
                        var target = Path.Combine(cacheDir, info.LocalFilename);

                        if (File.Exists(target)) {
                            ServeStaticFile(stream, buffer, info, target);
                            break;
                        }

                        string cacheFile = Path.Combine(cacheDir, info.LocalFilename + ".tmp");

                        if (File.Exists(cacheFile)) {
                            ServeCachedFile(stream, cacheFile, info);
                            break;
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
                Interlocked.Decrement(ref incomingConnections);

                try
                {
                    Logger.ReportInfo("MB Trailers closing connections.");
                    stream.Close();
                    client.Close();
                } 
                catch
                {
                    // well we tried
                }
            }
        }

        private void ProxyRemoteFile(NetworkStream stream, byte[] buffer, ProxyInfo info, string target, string cacheFile) {

            Logger.ReportInfo("Proxying file: " + target);

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

            Logger.ReportInfo("Serving cached file: " + target);

            StringBuilder header = new StringBuilder();
            header.Append("HTTP/1.1 200 OK\r\n");
            header.AppendFormat("Content-Length: {0}\r\n", new FileInfo(target).Length);
            header.AppendFormat("Content-Type: {0}\r\n", info.ContentType);
            header.Append("Content-Transfer-Encoding: binary\r\n");
            header.Append("\r\n");


            var streamWriter = new StreamWriter(stream);
            streamWriter.Write(header.ToString());

            using (var p = File.Open(target, FileMode.Open, FileAccess.Read, FileShare.Read)) {

                while (true) {

                    var bytes_read = p.Read(buffer, 0, buffer.Length);

                    if (bytes_read > 0)
                    {
                        try
                        {
                            stream.Write(buffer, 0, bytes_read);
                        }
                        catch (IOException)
                        {
                            return; //they probably shut down the request before we were finished
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public static void CommitTempFile(FileStream fs, string path)
        {
            // first strip the headers
            fs.Seek(0, SeekOrigin.Begin);
            // lets use a BinaryReader with special line handling support
            BinaryLineReader sr = new BinaryLineReader(fs);

            // loops until we get a blank line
            while (sr.ReadLine().Trim() != "") ;
            // our file cursor is now at the top of the video file, lets start copying.
            // first delete tempfile
            var tempFile = path + ".stmp";
            if (File.Exists(tempFile)) File.Delete(tempFile);
            // lets copy
            byte[] buffer = new byte[1024 * 32]; //32k buffer
            using (FileStream fw = new FileStream(tempFile, FileMode.CreateNew))
            {
                //make sure we stil read from the BinaryReader sr
                //as they will still be data in it's buffers
                var bytesRead = sr.Read(buffer, 0, buffer.Length);
                while (bytesRead > 0)
                {
                    fw.Write(buffer, 0, bytesRead);
                    bytesRead = sr.Read(buffer, 0, buffer.Length);
                }
            }
            // ok, everything copied OK, lets rename
            if (File.Exists(path)) File.Delete(path);
            File.Move(tempFile, path);
        }

        public void ServeCachedFile(NetworkStream stream, string filename, ProxyInfo info) {
            
            Logger.ReportInfo("Serving temp file: " + filename);

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
                    try
                    {
                        stream.Write(buffer, 0, bytesRead);
                    }
                    catch (IOException)
                    {
                        return; //they probably shut down the request before we were finished
                    }
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
