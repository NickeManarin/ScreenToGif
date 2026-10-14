#!/usr/bin/env python3
"""Inspect X Shape input regions across a client's complete native hierarchy."""

import ctypes
import os
import sys


SHAPE_INPUT = 2


class XRectangle(ctypes.Structure):
    _fields_ = [
        ("x", ctypes.c_short),
        ("y", ctypes.c_short),
        ("width", ctypes.c_ushort),
        ("height", ctypes.c_ushort),
    ]


def main() -> int:
    if len(sys.argv) != 2:
        print(f"Usage: {sys.argv[0]} <window-id>", file=sys.stderr)
        return 2

    try:
        window = int(sys.argv[1], 0)
    except ValueError:
        print(f"Invalid window ID: {sys.argv[1]!r}. Use decimal or 0x-prefixed hexadecimal.", file=sys.stderr)
        return 2

    try:
        x11 = ctypes.CDLL("libX11.so.6")
        xext = ctypes.CDLL("libXext.so.6")
    except OSError as error:
        print(f"Could not load X11 shape libraries: {error}", file=sys.stderr)
        return 1

    configure_abi(x11, xext)

    display_name = os.environ.get("DISPLAY")
    display = x11.XOpenDisplay(display_name.encode() if display_name else None)
    if not display:
        target = display_name or "the default display"
        print(f"Could not open {target}.", file=sys.stderr)
        return 1

    try:
        root, _, _ = query_tree(x11, display, window)
        ancestors = collect_ancestors(x11, display, window, root)
        descendants = collect_descendants(x11, display, window)

        print_window(x11, xext, display, "client", window, window, root)
        for index, ancestor in enumerate(ancestors):
            print_window(x11, xext, display, f"ancestor[{index}]", ancestor, window, root)
        for path, descendant in descendants:
            print_window(x11, xext, display, f"descendant[{path}]", descendant, window, root)
        return 0
    except RuntimeError as error:
        print(f"Inspection failed: {error}", file=sys.stderr)
        return 1
    finally:
        x11.XCloseDisplay(display)


def configure_abi(x11, xext):
    x11.XOpenDisplay.argtypes = [ctypes.c_char_p]
    x11.XOpenDisplay.restype = ctypes.c_void_p
    x11.XCloseDisplay.argtypes = [ctypes.c_void_p]
    x11.XFree.argtypes = [ctypes.c_void_p]
    x11.XFree.restype = ctypes.c_int
    x11.XQueryTree.argtypes = [
        ctypes.c_void_p,
        ctypes.c_ulong,
        ctypes.POINTER(ctypes.c_ulong),
        ctypes.POINTER(ctypes.c_ulong),
        ctypes.POINTER(ctypes.POINTER(ctypes.c_ulong)),
        ctypes.POINTER(ctypes.c_uint),
    ]
    x11.XQueryTree.restype = ctypes.c_int
    x11.XGetGeometry.argtypes = [
        ctypes.c_void_p,
        ctypes.c_ulong,
        ctypes.POINTER(ctypes.c_ulong),
        ctypes.POINTER(ctypes.c_int),
        ctypes.POINTER(ctypes.c_int),
        ctypes.POINTER(ctypes.c_uint),
        ctypes.POINTER(ctypes.c_uint),
        ctypes.POINTER(ctypes.c_uint),
        ctypes.POINTER(ctypes.c_uint),
    ]
    x11.XGetGeometry.restype = ctypes.c_int
    x11.XTranslateCoordinates.argtypes = [
        ctypes.c_void_p,
        ctypes.c_ulong,
        ctypes.c_ulong,
        ctypes.c_int,
        ctypes.c_int,
        ctypes.POINTER(ctypes.c_int),
        ctypes.POINTER(ctypes.c_int),
        ctypes.POINTER(ctypes.c_ulong),
    ]
    x11.XTranslateCoordinates.restype = ctypes.c_int
    xext.XShapeGetRectangles.argtypes = [
        ctypes.c_void_p,
        ctypes.c_ulong,
        ctypes.c_int,
        ctypes.POINTER(ctypes.c_int),
        ctypes.POINTER(ctypes.c_int),
    ]
    xext.XShapeGetRectangles.restype = ctypes.POINTER(XRectangle)


def query_tree(x11, display, window):
    root = ctypes.c_ulong()
    parent = ctypes.c_ulong()
    children = ctypes.POINTER(ctypes.c_ulong)()
    child_count = ctypes.c_uint()
    if not x11.XQueryTree(
        display,
        window,
        ctypes.byref(root),
        ctypes.byref(parent),
        ctypes.byref(children),
        ctypes.byref(child_count),
    ):
        raise RuntimeError(f"XQueryTree failed for {hex(window)}")

    try:
        values = [children[index] for index in range(child_count.value)]
        return root.value, parent.value, values
    finally:
        if children:
            x11.XFree(children)


def collect_ancestors(x11, display, window, root):
    ancestors = []
    _, parent, _ = query_tree(x11, display, window)
    while parent and parent != root:
        ancestors.append(parent)
        _, parent, _ = query_tree(x11, display, parent)
    return ancestors


def collect_descendants(x11, display, window):
    descendants = []

    def visit(parent, prefix):
        _, _, children = query_tree(x11, display, parent)
        for index, child in enumerate(children):
            path = f"{prefix}.{index}" if prefix else str(index)
            descendants.append((path, child))
            visit(child, path)

    visit(window, "")
    return descendants


def get_geometry(x11, display, window):
    root = ctypes.c_ulong()
    x = ctypes.c_int()
    y = ctypes.c_int()
    width = ctypes.c_uint()
    height = ctypes.c_uint()
    border_width = ctypes.c_uint()
    depth = ctypes.c_uint()
    if not x11.XGetGeometry(
        display,
        window,
        ctypes.byref(root),
        ctypes.byref(x),
        ctypes.byref(y),
        ctypes.byref(width),
        ctypes.byref(height),
        ctypes.byref(border_width),
        ctypes.byref(depth),
    ):
        raise RuntimeError(f"XGetGeometry failed for {hex(window)}")
    return width.value, height.value, border_width.value


def translate_origin(x11, display, window, destination):
    x = ctypes.c_int()
    y = ctypes.c_int()
    child = ctypes.c_ulong()
    if not x11.XTranslateCoordinates(
        display,
        window,
        destination,
        0,
        0,
        ctypes.byref(x),
        ctypes.byref(y),
        ctypes.byref(child),
    ):
        raise RuntimeError(f"XTranslateCoordinates failed for {hex(window)}")
    return x.value, y.value


def get_shape(x11, xext, display, window):
    count = ctypes.c_int()
    ordering = ctypes.c_int()
    rectangles = xext.XShapeGetRectangles(
        display,
        window,
        SHAPE_INPUT,
        ctypes.byref(count),
        ctypes.byref(ordering),
    )
    try:
        values = [
            (rectangles[index].x, rectangles[index].y, rectangles[index].width, rectangles[index].height)
            for index in range(count.value)
        ]
        return ordering.value, values
    finally:
        if rectangles:
            x11.XFree(rectangles)


def print_window(x11, xext, display, label, window, client, root):
    width, height, border_width = get_geometry(x11, display, window)
    client_x, client_y = translate_origin(x11, display, window, client)
    root_x, root_y = translate_origin(x11, display, window, root)
    ordering, rectangles = get_shape(x11, xext, display, window)
    print(
        f"{label}={hex(window)} geometry={width}x{height}+{border_width} "
        f"client-origin=({client_x},{client_y}) root-origin=({root_x},{root_y}) "
        f"ordering={ordering} rectangles={rectangles}"
    )


if __name__ == "__main__":
    raise SystemExit(main())
