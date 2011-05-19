using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Threading;
using System.Data.SQLite;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library.ImageManagement
{
    class SQLiteImageCache : SQLiteRepository, IImageCache
    {
        public SQLiteImageCache(string dbPath)
        {
            if (sqliteAssembly == null)
            {
                sqliteAssembly = System.Reflection.Assembly.LoadFile(System.IO.Path.Combine(ApplicationPaths.AppConfigPath, "system.data.sqlite.dll"));
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(SqliteResolver);
            }

            SQLiteConnectionStringBuilder connectionstr = new SQLiteConnectionStringBuilder();
            connectionstr.PageSize = 4096;
            connectionstr.CacheSize = 4096;
            connectionstr.SyncMode = SynchronizationModes.Normal;
            connectionstr.DataSource = dbPath;
            connection = new SQLiteConnection(connectionstr.ConnectionString);
            connection.Open();

            string[] queries = {"create table if not exists images (guid, width, height, updated, data blob)",
                                "create unique index if not exists idx_images on images(guid, width, height)",
                               };


            foreach (var query in queries) {
                try {

                    connection.Exec(query);
                } catch (Exception e) {
                    Logger.ReportInfo(e.ToString());
                }
            }


            alive = true; // tell writer to keep going
            Async.Queue("ImageCache Writer", DelayedWriter); 

        }

        private string ImagePath(Guid id, int width)
        {
            return "http://localhost:8755/" + id.ToString() + "/" + width;
            //return "http://www.mediabrowser.tv/images/apps.png";
        }

        private System.Drawing.Image ResizeImage(System.Drawing.Image image, int width, int height)
        {
            var newBmp = new System.Drawing.Bitmap(width, height);

            using (System.Drawing.Bitmap bmp = (System.Drawing.Bitmap)image)
            using (System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(newBmp))
            {

                graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphic.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                graphic.DrawImage(bmp, 0, 0, width, height);
                
                return newBmp;
            }
        }

        #region IImageCache Members

        public List<ImageSize> AvailableSizes(Guid id)
        {
            throw new NotImplementedException();
        }

        public string CacheImage(Guid id, System.Drawing.Image image)
        {
            var ms = new MemoryStream();
            //test
            //image.Save("c:\\users\\eric\\my documents\\imagetest\\" + DateTime.Now.Millisecond + image.Width + "x" + image.Height + ".png", System.Drawing.Imaging.ImageFormat.Png);
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return CacheImage(id, ms, image.Width, image.Height);
        }

        public string CacheImage(Guid id, MemoryStream ms, int width, int height) 
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = "replace into images(guid, width, height, updated, data) values (@guid, @width, @height, @updated, @data)";

            SQLiteParameter guidParam = new SQLiteParameter("@guid");
            SQLiteParameter widthParam = new SQLiteParameter("@width");
            SQLiteParameter heightParam = new SQLiteParameter("@height");
            SQLiteParameter updatedParam = new SQLiteParameter("@updated");
            SQLiteParameter dataParam = new SQLiteParameter("@data");

            cmd.Parameters.Add(guidParam);
            cmd.Parameters.Add(widthParam);
            cmd.Parameters.Add(heightParam);
            cmd.Parameters.Add(updatedParam);
            cmd.Parameters.Add(dataParam);

            guidParam.Value = id.ToString();
            widthParam.Value = width;
            heightParam.Value = height;
            updatedParam.Value = DateTime.UtcNow;
            dataParam.Value = ms.ToArray();

            lock (connection)
            {
                //don't use our delayed writer here cuz we need to block until this is done
                cmd.ExecuteNonQuery();
            }
            return ImagePath(id, width);

        }

        public DateTime GetDate(Guid id)
        {
            throw new NotImplementedException();
        }

        public string GetImagePath(Guid id, int width, int height)
        {
            var cmd = connection.CreateCommand();
            if (width > 0)
            {
                cmd.CommandText = "select width from images where guid = @guid and width = @width";
                cmd.AddParam("@guid", id.ToString());
                cmd.AddParam("@width", width);
            }
            else
            {
                cmd.CommandText = "select width from images where guid = @guid order by width desc";
                cmd.AddParam("@guid", id.ToString());
            }


            using (var reader = cmd.ExecuteReader()) {
                if (!reader.HasRows) //need to cache it
                {
                    using (var ms = GetImageStream(id))
                    {
                        if (ms == null)
                        {
                            //no image is cached
                            return null;
                        }
                        else
                        {
                            CacheImage(id, ResizeImage(System.Drawing.Image.FromStream(ms), width, height));
                        }
                    }
                } 

                reader.Close();
            }
            return ImagePath(id, width);
        }

        public string GetImagePath(Guid id)
        {
            ImageInfo info = GetPrimaryImageInfo(id);
            if (info == null) info = new ImageInfo(null);
            return GetImagePath(id, info.Width, info.Height);
        }

        public ImageInfo GetPrimaryImageInfo(Guid id)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = "select width, height, updated from images where width = (select max(width) from images where guid = @guid)";
            cmd.AddParam("@guid", id.ToString());

            ImageInfo info = null;

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    info = new ImageInfo(null);
                    info.Width = Convert.ToInt32(reader[0]);
                    info.Height = Convert.ToInt32(reader[1]);
                    info.Date = DateTime.Parse(reader[2].ToString());
                }
                reader.Close();
            }
            return info;
        }

        public MemoryStream GetImageStream(Guid id)
        {
            return GetImageStream(id, 0);
        }

        public MemoryStream GetImageStream(Guid id, int width)
        {
            var cmd = connection.CreateCommand();
            if (width > 0)
            {
                cmd.CommandText = "select data from images where guid = @guid and width = @width";
                cmd.AddParam("@guid", id.ToString());
                cmd.AddParam("@width", width);
            }
            else
            {
                cmd.CommandText = "select data from images where guid = @guid order by width desc";
                cmd.AddParam("@guid", id.ToString());
            }

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    var data = reader.GetBytes(0);
                    var ms = new MemoryStream(data);
                    return ms;
                }
                else
                {
                    return null;
                }
            }
        }

        public ImageSize GetSize(Guid id)
        {
            throw new NotImplementedException();
        }

        public string Path
        {
            get { throw new NotImplementedException(); }
        }

        public void ClearCache(Guid id)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = "delete from images where guid = @guid";
            cmd.AddParam("@guid", id.ToString());
            cmd.ExecuteNonQuery();
        }

        #endregion
    }
}
