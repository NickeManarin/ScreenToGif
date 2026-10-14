using Avalonia;
using System.Runtime.InteropServices;

namespace ScreenToGif.Linux.Services;

internal sealed class X11WindowInputRegion : IDisposable
{
    private const int MaxAncestorDepth = 16;
    private const int ShapeSet = 0;
    private const int ShapeInput = 2;
    private const int Unsorted = 0;

    private IntPtr _display;
    private bool? _shapeAvailable;

    public bool TryApply(IntPtr windowHandle, string? handleDescriptor, Size clientSize, double renderScaling,
        double interactiveBottomHeight)
    {
        if (!OperatingSystem.IsLinux() || windowHandle == IntPtr.Zero ||
            !string.Equals(handleDescriptor, "XID", StringComparison.Ordinal))
            return false;

        try
        {
            if (!EnsureDisplay())
                return false;

            var rectangle = CalculateBottomRegion(clientSize, renderScaling, interactiveBottomHeight);
            var clientWindow = unchecked((nuint)windowHandle);
            if (!TryBuildShapeTargets(clientWindow, rectangle, out var targets))
                return false;

            foreach (var target in targets)
                SetInputShape(target.Window, target.Rectangles);

            XSync(_display, false);
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    internal static XRectangle CalculateBottomRegion(Size clientSize, double renderScaling,
        double interactiveBottomHeight)
    {
        if (!double.IsFinite(renderScaling) || renderScaling <= 0)
            throw new ArgumentOutOfRangeException(nameof(renderScaling));
        if (!double.IsFinite(interactiveBottomHeight) || interactiveBottomHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(interactiveBottomHeight));

        var pixelWidth = ClampToUnsignedShort(Math.Ceiling(Math.Max(0, clientSize.Width) * renderScaling));
        var pixelHeight = ClampToUnsignedShort(Math.Ceiling(Math.Max(0, clientSize.Height) * renderScaling));
        var bottomHeight = Math.Min(pixelHeight,
            ClampToUnsignedShort(Math.Ceiling(interactiveBottomHeight * renderScaling)));
        var top = Math.Min(pixelHeight - bottomHeight, (ushort)short.MaxValue);

        return new XRectangle(0, checked((short)top), pixelWidth, bottomHeight);
    }

    internal static XRectangle[] CalculateFrameRegions(uint frameWidth, uint frameHeight, int clientX, int clientY,
        uint clientWidth, uint clientHeight, ushort interactiveBottomHeight)
    {
        var width = Math.Min(frameWidth, ushort.MaxValue);
        var height = Math.Min(frameHeight, (uint)short.MaxValue);
        var left = Math.Clamp((long)clientX, 0, width);
        var top = Math.Clamp((long)clientY, 0, height);
        var right = Math.Clamp((long)clientX + clientWidth, left, width);
        var bottom = Math.Clamp((long)clientY + clientHeight, top, height);
        var barTop = Math.Max(top, bottom - interactiveBottomHeight);
        var rectangles = new List<XRectangle>(5);

        AddRectangle(rectangles, 0, 0, width, top);
        AddRectangle(rectangles, 0, bottom, width, height - bottom);
        AddRectangle(rectangles, 0, top, left, bottom - top);
        AddRectangle(rectangles, right, top, width - right, bottom - top);
        AddRectangle(rectangles, left, barTop, right - left, bottom - barTop);

        return [.. rectangles];
    }

    internal static XRectangle[] CalculateChildRegions(int childX, int childY, uint childWidth, uint childHeight,
        ushort clientWidth, ushort clientHeight, ushort interactiveBottomHeight)
    {
        var barTop = Math.Max(0, clientHeight - interactiveBottomHeight);
        var intersectionLeft = Math.Max(0L, childX);
        var intersectionTop = Math.Max((long)barTop, childY);
        var intersectionRight = Math.Min((long)clientWidth, (long)childX + childWidth);
        var intersectionBottom = Math.Min((long)clientHeight, (long)childY + childHeight);
        var rectangles = new List<XRectangle>(1);

        AddRectangle(rectangles, intersectionLeft - childX, intersectionTop - childY,
            intersectionRight - intersectionLeft, intersectionBottom - intersectionTop);

        return [.. rectangles];
    }

    public void Dispose()
    {
        if (_display == IntPtr.Zero)
            return;

        XCloseDisplay(_display);
        _display = IntPtr.Zero;
        _shapeAvailable = null;
    }

    private bool EnsureDisplay()
    {
        if (_display == IntPtr.Zero)
            _display = XOpenDisplay(IntPtr.Zero);
        if (_display == IntPtr.Zero)
            return false;

        _shapeAvailable ??= XShapeQueryExtension(_display, out _, out _) != 0;
        return _shapeAvailable.Value;
    }

    private bool TryBuildShapeTargets(nuint clientWindow, XRectangle clientRegion,
        out List<ShapeTarget> targets)
    {
        targets = [new ShapeTarget(clientWindow, [clientRegion])];
        if (!TryQueryTree(clientWindow, out var rootWindow, out var parentWindow, out var children))
            return false;

        if (!TryCollectDescendantTargets(clientWindow, children, clientRegion, targets))
            return false;

        var ancestor = parentWindow;
        for (var depth = 0; ancestor != 0 && ancestor != rootWindow && depth < MaxAncestorDepth; depth++)
        {
            if (!TryGetGeometry(ancestor, out var frameWidth, out var frameHeight) ||
                !TryGetGeometry(clientWindow, out var clientWidth, out var clientHeight) ||
                !TryTranslateCoordinates(clientWindow, ancestor, out var clientX, out var clientY))
                return false;

            targets.Add(new ShapeTarget(ancestor,
                CalculateFrameRegions(frameWidth, frameHeight, clientX, clientY, clientWidth, clientHeight,
                    clientRegion.Height)));

            if (!TryQueryTree(ancestor, out rootWindow, out ancestor, out _))
                return false;
        }

        return ancestor == rootWindow;
    }

    private bool TryCollectDescendantTargets(nuint clientWindow, IReadOnlyList<nuint> children,
        XRectangle clientRegion, ICollection<ShapeTarget> targets)
    {
        foreach (var child in children)
        {
            if (!TryGetGeometry(child, out var childWidth, out var childHeight) ||
                !TryTranslateCoordinates(child, clientWindow, out var childX, out var childY) ||
                !TryQueryTree(child, out _, out _, out var grandchildren))
                return false;

            targets.Add(new ShapeTarget(child,
                CalculateChildRegions(childX, childY, childWidth, childHeight, clientRegion.Width,
                    checked((ushort)(clientRegion.Y + clientRegion.Height)), clientRegion.Height)));

            if (!TryCollectDescendantTargets(clientWindow, grandchildren, clientRegion, targets))
                return false;
        }

        return true;
    }

    private bool TryQueryTree(nuint window, out nuint root, out nuint parent, out nuint[] children)
    {
        children = [];
        if (XQueryTree(_display, window, out root, out parent, out var childrenPointer, out var childCount) == 0)
            return false;

        try
        {
            children = new nuint[childCount];
            for (var index = 0; index < childCount; index++)
                children[index] = unchecked((nuint)Marshal.ReadIntPtr(childrenPointer, index * IntPtr.Size));
            return true;
        }
        finally
        {
            if (childrenPointer != IntPtr.Zero)
                XFree(childrenPointer);
        }
    }

    private bool TryGetGeometry(nuint window, out uint width, out uint height) =>
        XGetGeometry(_display, window, out _, out _, out _, out width, out height, out _, out _) != 0;

    private bool TryTranslateCoordinates(nuint source, nuint destination, out int x, out int y) =>
        XTranslateCoordinates(_display, source, destination, 0, 0, out x, out y, out _) != 0;

    private void SetInputShape(nuint window, XRectangle[] rectangles) =>
        XShapeCombineRectangles(_display, window, ShapeInput, 0, 0, rectangles, rectangles.Length, ShapeSet,
            Unsorted);

    private static void AddRectangle(ICollection<XRectangle> rectangles, long x, long y, long width, long height)
    {
        if (width <= 0 || height <= 0)
            return;

        rectangles.Add(new XRectangle(
            checked((short)Math.Clamp(x, short.MinValue, short.MaxValue)),
            checked((short)Math.Clamp(y, short.MinValue, short.MaxValue)),
            checked((ushort)Math.Clamp(width, 0, ushort.MaxValue)),
            checked((ushort)Math.Clamp(height, 0, ushort.MaxValue))));
    }

    private static ushort ClampToUnsignedShort(double value) =>
        checked((ushort)Math.Clamp(value, 0, ushort.MaxValue));

    private readonly record struct ShapeTarget(nuint Window, XRectangle[] Rectangles);

    [StructLayout(LayoutKind.Sequential)]
    internal readonly record struct XRectangle(short X, short Y, ushort Width, ushort Height);

    [DllImport("libX11.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr XOpenDisplay(IntPtr displayName);

    [DllImport("libX11.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern int XSync(IntPtr display, [MarshalAs(UnmanagedType.Bool)] bool discard);

    [DllImport("libX11.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern int XQueryTree(IntPtr display, nuint window, out nuint rootReturn,
        out nuint parentReturn, out IntPtr childrenReturn, out uint childCountReturn);

    [DllImport("libX11.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern int XGetGeometry(IntPtr display, nuint drawable, out nuint rootReturn, out int xReturn,
        out int yReturn, out uint widthReturn, out uint heightReturn, out uint borderWidthReturn,
        out uint depthReturn);

    [DllImport("libX11.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern int XTranslateCoordinates(IntPtr display, nuint sourceWindow, nuint destinationWindow,
        int sourceX, int sourceY, out int destinationXReturn, out int destinationYReturn, out nuint childReturn);

    [DllImport("libX11.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern int XFree(IntPtr data);

    [DllImport("libXext.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern int XShapeQueryExtension(IntPtr display, out int eventBase, out int errorBase);

    [DllImport("libXext.so.6", CallingConvention = CallingConvention.Cdecl)]
    private static extern void XShapeCombineRectangles(IntPtr display, nuint destinationWindow, int destinationKind,
        int xOffset, int yOffset, [In] XRectangle[]? rectangles, int rectangleCount, int operation, int ordering);
}
