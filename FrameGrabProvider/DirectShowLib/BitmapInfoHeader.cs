﻿// From the directshow.net project http://directshownet.sourceforge.net/)

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace MediaBrowser.Library.Interop.DirectShowLib {
    /// <summary>
    /// From BITMAPINFOHEADER
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public class BitmapInfoHeader {
        public int Size;
        public int Width;
        public int Height;
        public short Planes;
        public short BitCount;
        public int Compression;
        public int ImageSize;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public int ClrUsed;
        public int ClrImportant;
    }
}
