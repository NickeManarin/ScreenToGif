namespace ScreenToGif.Util
{
    public class FastRandom
    {
        private const double RealUnitInt = 1.0/(int.MaxValue + 1.0);

        private uint x, y, z, w;

        public FastRandom(uint seed)
        {
            x = seed;
            y = 842502087;
            z = 3579807591;
            w = 273326509;
        }

        public int Next(int upperBound)
        {
            var t = (x ^ (x << 11)); x = y; y = z; z = w;
            return (int) ((RealUnitInt*(int) (0x7FFFFFFF & (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8)))))*upperBound);
        }
    }
}