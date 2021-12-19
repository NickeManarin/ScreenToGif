#region Usings

#region Used Namespaces

using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using KGySoft.Drawing.Imaging;

#endregion

#region Used Aliases

using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

#endregion

#endregion

namespace ScreenToGif.ImageUtil.Imaging
{
    /// <summary>
    /// Provides an <see cref="IReadWriteBitmapData"/> implementation for <see cref="WriteableBitmapData"/>.
    /// It makes possible that also WPF images can use all <see cref="BitmapDataExtensions"/> and other bitmap data features
    /// such as quantizing, dithering and other image manipulations.
    /// </summary>
    internal sealed class WriteableBitmapData : IReadWriteBitmapData
    {
        #region Nested classes

        private sealed unsafe class Row32 : IReadWriteBitmapDataRow
        {
            #region Fields

            private readonly WriteableBitmapData _bitmapData;
            private readonly Color32* _address;

            #endregion

            #region Properties and Indexers

            #region Properties

            public int Index { get; private set; }
            public int Width => _bitmapData.Width;
            public int Size => _bitmapData.RowSize;

            #endregion

            #region Indexers

            public Color32 this[int x]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if ((uint)x >= Width)
                        ThrowXOutOfRange();
                    return _address[x];
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    if ((uint)x >= Width)
                        ThrowXOutOfRange();
                    _address[x] = value;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal Row32(WriteableBitmapData bitmapData, int y)
            {
                _bitmapData = bitmapData;
                Index = y;
                _address = (Color32*)(bitmapData._scan0 + bitmapData.RowSize * y);
            }

            #endregion

            #region Methods

            public bool MoveNextRow()
            {
                if (Index == _bitmapData.Height - 1)
                    return false;
                Index += 1;
                return true;
            }

            public Color GetColor(int x) => this[x].ToColor();
            public void SetColor(int x, Color color) => this[x] = new Color32(color);
            public int GetColorIndex(int x) => throw new NotSupportedException("Not an indexed bitmap data");
            public void SetColorIndex(int x, int colorIndex) => throw new NotSupportedException("Not an indexed bitmap data");

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T ReadRaw<T>(int x) where T : unmanaged
            {
                if ((x + 1) * sizeof(T) > Size)
                    ThrowXOutOfRange();
                return ((T*)_address)[x];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteRaw<T>(int x, T data) where T : unmanaged
            {
                if ((x + 1) * sizeof(T) > Size)
                    ThrowXOutOfRange();
                ((T*)_address)[x] = data;
            }

            #endregion
        }

        #endregion

        #region Fields

        private readonly IntPtr _scan0;

        private WriteableBitmap _bitmap;

        /// <summary>
        /// The cached lastly accessed row. Though may be accessed from multiple threads it is intentionally not volatile
        /// so it has a bit higher chance that every thread sees the last value was set by itself and no recreation is needed.
        /// </summary>
        private Row32 lastRow;

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties

        public int Width { get; }
        public int Height { get; }
        public PixelFormat PixelFormat { get; }
        public Palette Palette => null;
        public int RowSize { get; }
        public Color32 BackColor => default;
        public byte AlphaThreshold => default;
        public bool IsDisposed => _bitmap == null;
        public IReadWriteBitmapDataRow FirstRow => this[0];

        #endregion

        #region Explicitly Implemented Interface Properties

        IWritableBitmapDataRow IWritableBitmapData.FirstRow => FirstRow;
        IReadableBitmapDataRow IReadableBitmapData.FirstRow => FirstRow;

        #endregion

        #endregion

        #region Indexers

        #region Public Indexers

        public IReadWriteBitmapDataRow this[int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Exceptions are thrown from methods so JIT can inline this method
                if (IsDisposed)
                    ThrowDisposed();
                if ((uint)y > (uint)Height)
                    ThrowYOutOfRange();

                // If the same row is accessed repeatedly we return the cached last row.
                var result = lastRow;
                if (result?.Index == y)
                    return result;

                // Otherwise, we create and cache the result.
                return lastRow = new Row32(this, y);
            }
        }

        #endregion

        #region Explicitly Implemented Interface Indexers

        IWritableBitmapDataRow IWritableBitmapData.this[int y] => this[y];
        IReadableBitmapDataRow IReadableBitmapData.this[int y] => this[y];

        #endregion

        #endregion

        #endregion

        #region Constructors

        internal WriteableBitmapData(WriteableBitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            var pixelFormat = bitmap.Format;
            PixelFormat = pixelFormat == PixelFormats.Bgra32 ? PixelFormat.Format32bppArgb
                : pixelFormat == PixelFormats.Pbgra32 ? PixelFormat.Format32bppArgb
                : pixelFormat == PixelFormats.Bgr32 ? PixelFormat.Format32bppRgb
                : throw new NotSupportedException(bitmap.Format.ToString());
            Width = bitmap.PixelWidth;
            Height = bitmap.PixelHeight;
            RowSize = bitmap.BackBufferStride;

            _bitmap = bitmap;
            if (bitmap.IsFrozen)
                throw new ArgumentException("Bitmap must not be frozen");

            bitmap.Lock();
            _scan0 = bitmap.BackBuffer;
        }

        #endregion

        #region Methods

        #region Static Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowXOutOfRange() => throw new ArgumentOutOfRangeException("x");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowYOutOfRange() => throw new ArgumentOutOfRangeException("y");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowDisposed() => throw new ObjectDisposedException(null);

        #endregion

        #region Instance Methods

        public void Dispose()
        {
            if (IsDisposed)
                return;
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, Width, Height));
            _bitmap.Unlock();
            _bitmap = null;
        }

        public Color GetPixel(int x, int y) => this[y][x].ToColor();
        public void SetPixel(int x, int y, Color color) => this[y][x] = new Color32(color);

        #endregion

        #endregion
    }
}
