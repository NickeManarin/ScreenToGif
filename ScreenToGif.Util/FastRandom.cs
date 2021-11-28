namespace ScreenToGif.Util;

public class FastRandom
{
    private const double RealUnitInt = 1.0/(int.MaxValue + 1.0);

    private uint _x, _y, _z, _w;

    public FastRandom(uint seed)
    {
        _x = seed;
        _y = 842502087;
        _z = 3579807591;
        _w = 273326509;
    }

    public int Next(int upperBound)
    {
        var t = (_x ^ (_x << 11)); _x = _y; _y = _z; _z = _w;
        return (int) ((RealUnitInt*(int) (0x7FFFFFFF & (_w = (_w ^ (_w >> 19)) ^ (t ^ (t >> 8)))))*upperBound);
    }
}