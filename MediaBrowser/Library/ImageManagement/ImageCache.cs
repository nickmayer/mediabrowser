using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using MediaBrowser.Library.Filesystem;
using System.Text.RegularExpressions;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Configuration;


namespace MediaBrowser.Library.ImageManagement {
    // this class is used to watch and gather basic info for all the files in the image cache folder 
    public class ImageSize {
        public ImageSize(int width, int height) {
            Width = width;
            Height = height;
        }
        public int Width {get; set;}
        public int Height {get; set;} 
    }


    public class ImageCache {

        static Dictionary<Guid, object> FileLocks = new Dictionary<Guid, object>();

        class Instansiator {
            public static ImageCache Instance = new ImageCache(ApplicationPaths.AppImagePath);
        }
       

        public static ImageCache Instance {
            get {
                return Instansiator.Instance;
            }
        }

        public class ImageSet {
            public ImageSet(ImageCache owner, Guid id) {
                this.Id = id;
                this.Owner = owner;
                this.ResizedImages = new List<ImageInfo>();
            }
            public Guid Id {get; set;}
            public ImageInfo PrimaryImage { get; set; }
            public List<ImageInfo> ResizedImages { get; set; }
            public ImageCache Owner {get; private set;} 
        }

        public class ImageInfo {
            public ImageInfo(ImageSet parent) {
                this.Parent = parent;
            }
            public ImageSet Parent {get; private set;}
            public DateTime Date { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public ImageFormat ImageFormat;


            public ImageSize Size {
                get {
                    return new ImageSize(Width, Height);
                }
            }

            private string ImageFormatExtension {
                get {
                    string ext;
                    if (this.ImageFormat.Guid == ImageFormat.Gif.Guid) {
                        ext = "gif";
                    } else if (this.ImageFormat.Guid == ImageFormat.Png.Guid) {
                        ext = "png";
                    } else if (this.ImageFormat.Guid == ImageFormat.Jpeg.Guid) {
                        ext = "jpg";
                    } else {
                        throw new ApplicationException("Unsupported Image type!");
                    }
                    return ext;
                }
            }

            public string Path {
                get {
                    var filename = new StringBuilder();
                    if (Parent.PrimaryImage == this) {
                        filename.Append("z"); 
                    }
                    filename.Append(Parent.Id.ToString())
                        .Append(".")
                        .Append(Width)
                        .Append("x")
                        .Append(Height)
                        .Append(".")
                        .Append(ImageFormatExtension);

                    return System.IO.Path.Combine(Parent.Owner.Path, filename.ToString());
                } 
            }
        }

        Dictionary<Guid, ImageSet> imageInfoCache = new Dictionary<Guid, ImageSet>();

        public string Path { get; private set; }
        public ImageCache(string path) {
            this.Path = path;
            LoadInfo(); 
        }



        static Regex infoRegex = new Regex(@"(z)?([0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12})\.?([0-9]*)x?([0-9]*)", RegexOptions.Compiled);

        private void LoadInfo() {
            
          
            foreach (var item in Kernel.Instance.GetLocation<IFolderMediaLocation>(Path).Children) {
                try {
                    AddToCache(item);
                } catch (Exception e) {
                    Logger.ReportException("Failed to deal with image: " + item.Path + " please delete it manually", e);
                }
            }
        }

        private void AddToCache(IMediaLocation item) {
            if (item is IFolderMediaLocation) {
                return;
            }

            var extension = System.IO.Path.GetExtension(item.Path).ToLower();
            ImageFormat imageFormat = null;
            if (extension == ".png") {
                imageFormat = ImageFormat.Png;
            } else if (extension == ".jpg") {
                imageFormat = ImageFormat.Jpeg;
            } else if (extension == ".gif") {
                imageFormat = ImageFormat.Gif;
            } else {
                // bad file in image cache
                File.Delete(item.Path);
                return;
            }


            var match = infoRegex.Match(item.Name);
            if (match == null || match.Groups[1].Value == null) {
                // bad file
                File.Delete(item.Path);
                return;
            }

            bool isPrimary = match.Groups[1].Value == "z";
            Guid id = new Guid(match.Groups[2].Value);

            int width = -1;
            int height = -1;

            if (!string.IsNullOrEmpty(match.Groups[3].Value)) {
                width = Int32.Parse(match.Groups[3].Value);
            }
            if (!string.IsNullOrEmpty(match.Groups[4].Value)) {
                height = Int32.Parse(match.Groups[4].Value);
            }

            
            var imageSet = GetImageSet(id);
            if (imageSet == null) {
                imageSet = new ImageSet(this, id);
                imageInfoCache[id] = imageSet;
            }

           

            var info = new ImageInfo(imageSet);
            info.ImageFormat = imageFormat;
            info.Date = item.DateCreated;

            //upgrade logic
            if (width == -1 || height == -1) {
                Image image = Image.FromFile(item.Path);
                isPrimary = true;
                info.Width = image.Width;
                info.Height = image.Height;
                imageSet.PrimaryImage = info;
                image.Dispose();
                File.Move(item.Path, info.Path);
            } else {
                info.Width = width;
                info.Height = height;
            }

            if (isPrimary) {
                imageSet.PrimaryImage = info;
            } else {
                imageSet.ResizedImages.Add(info);
            }
           
        }

        public string GetImagePath(Guid id) {
            ImageSet set = GetImageSet(id);
            return set != null && set.PrimaryImage != null ? set.PrimaryImage.Path : null;
        }

        public string GetImagePath(Guid id, int width, int height) {
            ImageSet set = GetImageSet(id);
            if (set != null && set.PrimaryImage != null) {
                var image = set.ResizedImages.FirstOrDefault(info => info.Width == width && info.Height == height);
                if (image != null) {
                    return image.Path;
                } else {
                    return ResizeImage(id, width, height).Path;
                }
            }

            // no such image
            return null;
        }

        public ImageSet GetImageSet(Guid id) {
            lock (this) {
                ImageSet set = null;
                lock (imageInfoCache) {
                    imageInfoCache.TryGetValue(id, out set);
                }
                return set;
            }
        }

        public ImageInfo GetPrimaryImage(Guid id) { 
            var set = GetImageSet(id); 
            if (set != null) {
                return set.PrimaryImage;
            } 
            return null;
        }

        public List<ImageSize> AvailableSizes(Guid id) {
            ImageSet set = GetImageSet(id);
            if (set != null) {
                return set.ResizedImages.Select(_ => _.Size).Concat(new ImageSize[] { set.PrimaryImage.Size }).ToList();
            } else {
                return null;
            }
        }

        public string CacheImage(Guid id, Image image) {

            var imageSet = GetOrCreateImageSet(id);

            lock (imageSet) {
                if (imageSet != null) {
                    DeleteImageSet(imageSet);
                }

                ImageInfo info = new ImageInfo(imageSet);
                info.Width = image.Width;
                info.Height = image.Height;
                info.ImageFormat = image.RawFormat;
                info.Date = DateTime.UtcNow;
                imageSet.PrimaryImage = info;
                image.Save(info.Path);
                return info.Path;
            }
            
        }

        private static void DeleteImageSet(ImageSet imageSet) {
            if (imageSet.PrimaryImage != null) {
                File.Delete(imageSet.PrimaryImage.Path);
            }
            foreach (var resized in imageSet.ResizedImages) {
                File.Delete(resized.Path);
            }
            imageSet.ResizedImages = new List<ImageInfo>();
        }

        private ImageSet GetOrCreateImageSet(Guid id) {
            ImageSet imageSet;
            lock (this) {
                imageSet = GetImageSet(id);

                if (imageSet == null) {
                    imageSet = new ImageSet(this, id);
                    imageInfoCache[id] = imageSet;
                }
            }
            return imageSet;
        }


        // cache info about the image without the actual image - for local images 
        public void CacheImageInfo(Guid id, string path) { 
        }

        public DateTime GetDate(Guid id) {
            return GetImageSet(id).PrimaryImage.Date;
        }

        public ImageSize GetSize(Guid id) {
            var img = GetImageSet(id).PrimaryImage;
            return (new ImageSize(img.Width, img.Height));
        }


        private ImageInfo ResizeImage(Guid id, int width, int height) {
            var set = GetImageSet(id);

            lock (set) {
                ImageInfo info = new ImageInfo(set);
                info.Width = width;
                info.Height = height;
                info.Date = DateTime.UtcNow;
                info.ImageFormat = set.PrimaryImage.ImageFormat;
                set.ResizedImages.Add(info);

                using (System.Drawing.Bitmap bmp = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(set.PrimaryImage.Path))
                using (System.Drawing.Bitmap newBmp = new System.Drawing.Bitmap(width, height))
                using (System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(newBmp)) {

                    graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    graphic.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                    graphic.DrawImage(bmp, 0, 0, width, height);

                    MemoryStream ms = new MemoryStream();
                    newBmp.Save(ms, info.ImageFormat);

                    using (var fs = ProtectedFileStream.OpenExclusiveWriter(info.Path)) {
                        BinaryWriter bw = new BinaryWriter(fs);
                        bw.Write(ms.ToArray());
                    }
                }

                return info;
            }
        }


        internal void ClearCache(Guid id) {
            var set = GetImageSet(id); 
            if (set != null) {
                lock (set) {
                    DeleteImageSet(set);
                }
            }
        }
    }
}
