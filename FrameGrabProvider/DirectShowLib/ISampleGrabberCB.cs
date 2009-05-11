﻿// From the directshow.net project http://directshownet.sourceforge.net/)

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace MediaBrowser.Library.Interop.DirectShowLib {
    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
          Guid("0579154A-2B53-4994-B0D0-E773148EFF85"),
          InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  
    public interface ISampleGrabberCB {
        /// <summary>
        /// When called, callee must release pSample
        /// </summary>
        [PreserveSig]
        int SampleCB(double SampleTime, IMediaSample pSample);

        [PreserveSig]
        int BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen);
    }
}
