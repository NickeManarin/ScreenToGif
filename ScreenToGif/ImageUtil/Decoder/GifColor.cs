namespace ScreenToGif.ImageUtil.Decoder
{
    public struct GifColor
    {
        private readonly byte _r;
        private readonly byte _g;
        private readonly byte _b;

        public GifColor(byte r, byte g, byte b)
        {
            _r = r;
            _g = g;
            _b = b;
        }

        public byte R { get { return _r; } }
        public byte G { get { return _g; } }
        public byte B { get { return _b; } }

        public override string ToString()
        {
            return string.Format("#{0:x2}{1:x2}{2:x2}", _r, _g, _b);
        }
    }
}
