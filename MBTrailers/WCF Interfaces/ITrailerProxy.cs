using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;


namespace WebProxy.WCFInterfaces
{
    [ServiceContract]
    public interface ITrailerProxy
    {
        [OperationContract]
        void Init(string cacheDir, int port);
        [OperationContract]
        ProxyInfo GetProxyInfo(string key);
        [OperationContract]
        void SetProxyInfo(ProxyInfo info);
        [OperationContract]
        string GetRandomTrailer();
    }

    [DataContract]
    public class ProxyInfo
    {
        public const string ITunesUserAgent = "QuickTime/7.6.5 (qtver=7.6.5;os=Windows NT 6.1)";

        public ProxyInfo(string host, string path, string userAgent, int port)
        {
            UserAgent = userAgent;
            Path = path;
            Host = host;
            Port = port;

            LocalFilename = string.Format("{0}{1}{2}", Host, Path, port).GetMD5String() + "_" +
                System.IO.Path.GetFileName(path);

            ContentType = "video/quicktime";
        }

        [DataMember]
        public string UserAgent { get; private set; }
        [DataMember]
        public string Host { get; private set; }
        [DataMember]
        public string Path { get; private set; }

        [DataMember]
        public string ContentType { get; private set; }

        [DataMember]
        public int BytesRead { get; set; }
        [DataMember]
        public bool Completed { get; set; }

        [DataMember]
        public string LocalFilename { get; private set; }


        [DataMember]
        public int Port { get; private set; }
    }


}
