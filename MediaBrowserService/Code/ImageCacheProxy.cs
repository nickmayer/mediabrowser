using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.IO;
using MediaBrowser.Library.Logging;

namespace MediaBrowserService.Code
{
    class ImageCacheProxy : HttpProxy
    {
        public ImageCacheProxy(int port, int maxConnections) : base(port, maxConnections) { }

        protected override void ServiceClient(TcpClient client)
        {
            var stream = client.GetStream();

            try
            {
                byte[] buffer = new byte[8000];
                StringBuilder data = new StringBuilder();
                bool httpRequestComplete = false;
                while (client.Connected)
                {
                    if (stream.DataAvailable && !httpRequestComplete)
                    {
                        int bytes_read = stream.Read(buffer, 0, buffer.Length);
                        data.Append(ASCIIEncoding.ASCII.GetString(buffer, 0, bytes_read));

                        if (data.ToString().Contains("\r\n\r\n"))
                        {
                            //Logger.ReportVerbose("Request complete");
                            //Logger.ReportVerbose("Data were: " + data.ToString());
                            httpRequestComplete = true;
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }

                    if (httpRequestComplete)
                    {
                        var headers = new HttpHeaders(data.ToString());
                        string requestedPath = headers.Path;

                        try
                        {
                            ServeImage(stream, requestedPath);
                        }
                        catch (Exception ee)
                        {
                            Logger.ReportException("Failed to proxy image : " + requestedPath, ee);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.ReportException("Failed to serve image  ", e);
            }
            finally
            {

                try
                {
                    Logger.ReportVerbose("Image proxy closing connections.");
                    stream.Close();
                }
                catch
                {
                    // well we tried
                }
            }
        }


        private void ServeImage(NetworkStream stream, string target) {

            var parms = target.Split('/');
            if (parms.Length < 3)
            {
                Logger.ReportWarning("Image Proxy invalid request: " + target);
                return;
            }
            string id = parms[1];
            int width = 0;
            Int32.TryParse(parms[2], out width);
            if (string.IsNullOrEmpty(id))
            {
                Logger.ReportWarning("Image Proxy invalid request: " + target);
                return;
            }
            int size = 0;
            Int32.TryParse(parms[3], out size);

            //get the image and send it to the client
            var image = MediaBrowser.Library.ImageManagement.ImageCache.Instance.GetImageStream(new Guid(id), width);
            if (image == null)
            {
                Logger.ReportError("Unable to retrieve image: " + target + ". Aborting.");
                return;
            }
            if (size == 0) size = (int)image.Length;
            Logger.ReportVerbose("Serving image: " + id + " width: " + width + " actual size: "+size+" block size: " + image.Length);

            StringBuilder header = new StringBuilder();
            header.Append("HTTP/1.1 200 OK\r\n");
            header.AppendFormat("Content-Length: {0}\r\n", size);
            header.AppendFormat("Content-Type: image/jpeg\r\n");
            //header.AppendFormat("Content-Disposition: inline;filename=\"image.jpg;\"\r\n");
            //header.Append("Content-Transfer-Encoding: binary\r\n");
            header.Append("\r\n");

            var streamWriter = new StreamWriter(stream, System.Text.Encoding.ASCII);
            stream.Write(new ASCIIEncoding().GetBytes(header.ToString()),0, header.Length);
            stream.Write(image.ToArray(), 0, size);

            image.Close();

            stream.Flush();
            stream.Close();

        }
    }
}
