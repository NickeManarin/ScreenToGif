#region License

/*
    Adapted work from:

    DirectShowLib - Provide access to DirectShow interfaces via .NET
    Copyright (C) 2007
    http://sourceforge.net/projects/directshownet/
    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.
    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.
    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

#endregion

using System;
using System.Runtime.InteropServices;

namespace ScreenToGif.Webcam.DirectShow;

public class Util
{
    [StructLayout(LayoutKind.Sequential), ComVisible(false)]
    public class DsOptInt64
    {
        public DsOptInt64(long value)
        {
            Value = value;
        }

        public long Value;
    }

    [StructLayout(LayoutKind.Sequential), ComVisible(false)]
    public struct DsRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2), ComVisible(false)]
    public struct BitmapInfoHeader
    {
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

    /// <summary>
    /// Free the nested structures and release any COM objects within an AMMediaType struct.
    /// </summary>
    public static void FreeAMMediaType(CoreStreaming.AmMediaType mediaType)
    {
        if (mediaType.formatSize != 0)
            Marshal.FreeCoTaskMem(mediaType.formatPtr);
        if (mediaType.unkPtr != IntPtr.Zero)
            Marshal.Release(mediaType.unkPtr);

        mediaType.formatSize = 0;
        mediaType.formatPtr = IntPtr.Zero;
        mediaType.unkPtr = IntPtr.Zero;
    }

    /// <summary>
    /// DsLong is a wrapper class around a <see cref="System.Int64"/> value type.
    /// </summary>
    /// <remarks>
    /// This class is necessary to enable null parameters passing.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public class DsLong
    {
        private readonly long _value;

        /// <summary>
        /// Constructor
        /// Initialize a new instance of DsLong with the Value parameter
        /// </summary>
        /// <param name="value">Value to assign to this new instance</param>
        public DsLong(long value)
        {
            _value = value;
        }

        /// <summary>
        /// Get a string representation of this DsLong Instance.
        /// </summary>
        /// <returns>A string representing this instance</returns>
        public override string ToString()
        {
            return _value.ToString();
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Define implicit cast between DsLong and System.Int64 for languages supporting this feature.
        /// VB.Net doesn't support implicit cast. <see cref="DsLong.ToInt64"/> for similar functionality.
        /// <code>
        ///   // Define a new DsLong instance
        ///   DsLong dsL = new DsLong(9876543210);
        ///   // Do implicit cast between DsLong and Int64
        ///   long l = dsL;
        ///
        ///   Console.WriteLine(l.ToString());
        /// </code>
        /// </summary>
        /// <param name="value">DsLong to be cast</param>
        /// <returns>A casted System.Int64</returns>
        public static implicit operator long(DsLong value)
        {
            return value._value;
        }

        /// <summary>
        /// Define implicit cast between System.Int64 and DsLong for languages supporting this feature.
        /// VB.Net doesn't support implicit cast.
        /// <code>
        ///   // Define a new Int64 instance
        ///   long l = 9876543210;
        ///   // Do implicit cast between Int64 and DsLong
        ///   DsLong dsl = l;
        ///
        ///   Console.WriteLine(dsl.ToString());
        /// </code>
        /// </summary>
        /// <param name="value">System.Int64 to be cast</param>
        /// <returns>A casted DsLong</returns>
        public static implicit operator DsLong(long value)
        {
            return new DsLong(value);
        }

        /// <summary>
        /// Get the System.Int64 equivalent to this DsLong instance.
        /// </summary>
        /// <returns>A System.Int64</returns>
        public long ToInt64()
        {
            return _value;
        }

        /// <summary>
        /// Get a new DsLong instance for a given System.Int64
        /// </summary>
        /// <param name="value">The System.Int64 to wrap into a DsLong</param>
        /// <returns>A new instance of DsLong</returns>
        public static DsLong FromInt64(long value)
        {
            return new DsLong(value);
        }
    }

    /// <summary>
    /// DsGuid is a wrapper class around a System.Guid value type.
    /// </summary>
    /// <remarks>
    /// This class is necessary to enable null parameters passing.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit)]
    public class DsGuid
    {
        [FieldOffset(0)]
        private Guid guid;

        public static readonly DsGuid Empty = Guid.Empty;

        /// <summary>
        /// Empty constructor.
        /// Initialize it with System.Guid.Empty
        /// </summary>
        public DsGuid()
        {
            guid = Guid.Empty;
        }

        /// <summary>
        /// Constructor.
        /// Initialize this instance with a given System.Guid string representation.
        /// </summary>
        /// <param name="g">A valid System.Guid as string</param>
        public DsGuid(string g)
        {
            guid = new Guid(g);
        }

        /// <summary>
        /// Constructor.
        /// Initialize this instance with a given System.Guid.
        /// </summary>
        /// <param name="g">A System.Guid value type</param>
        public DsGuid(Guid g)
        {
            guid = g;
        }

        /// <summary>
        /// Get a string representation of this DsGuid Instance.
        /// </summary>
        /// <returns>A string representing this instance</returns>
        public override string ToString()
        {
            return guid.ToString();
        }

        /// <summary>
        /// Get a string representation of this DsGuid Instance with a specific format.
        /// </summary>
        /// <param name="format"><see cref="System.Guid.ToString"/> for a description of the format parameter.</param>
        /// <returns>A string representing this instance according to the format parameter</returns>
        public string ToString(string format)
        {
            return guid.ToString(format);
        }

        public override int GetHashCode()
        {
            return guid.GetHashCode();
        }

        /// <summary>
        /// Define implicit cast between DsGuid and System.Guid for languages supporting this feature.
        /// VB.Net doesn't support implicit cast. <see cref="DsGuid.ToGuid"/> for similar functionality.
        /// <code>
        ///   // Define a new DsGuid instance
        ///   DsGuid dsG = new DsGuid("{33D57EBF-7C9D-435e-A15E-D300B52FBD91}");
        ///   // Do implicit cast between DsGuid and Guid
        ///   Guid g = dsG;
        ///
        ///   Console.WriteLine(g.ToString());
        /// </code>
        /// </summary>
        /// <param name="g">DsGuid to be cast</param>
        /// <returns>A casted System.Guid</returns>
        public static implicit operator Guid(DsGuid g)
        {
            return g.guid;
        }

        /// <summary>
        /// Define implicit cast between System.Guid and DsGuid for languages supporting this feature.
        /// VB.Net doesn't support implicit cast. <see cref="DsGuid.FromGuid"/> for similar functionality.
        /// <code>
        ///   // Define a new Guid instance
        ///   Guid g = new Guid("{B9364217-366E-45f8-AA2D-B0ED9E7D932D}");
        ///   // Do implicit cast between Guid and DsGuid
        ///   DsGuid dsG = g;
        ///
        ///   Console.WriteLine(dsG.ToString());
        /// </code>
        /// </summary>
        /// <param name="g">System.Guid to be cast</param>
        /// <returns>A casted DsGuid</returns>
        public static implicit operator DsGuid(Guid g)
        {
            return new DsGuid(g);
        }

        /// <summary>
        /// Get the System.Guid equivalent to this DsGuid instance.
        /// </summary>
        /// <returns>A System.Guid</returns>
        public Guid ToGuid()
        {
            return guid;
        }

        /// <summary>
        /// Get a new DsGuid instance for a given System.Guid
        /// </summary>
        /// <param name="g">The System.Guid to wrap into a DsGuid</param>
        /// <returns>A new instance of DsGuid</returns>
        public static DsGuid FromGuid(Guid g)
        {
            return new DsGuid(g);
        }
    }
}