#!/usr/bin/python3
"""Owned changing-content window for the opt-in native Wayland recording check."""
import os
import sys
import gi

gi.require_version("Gtk", "4.0")
gi.require_version("Gdk", "4.0")
gi.require_version("GdkWayland", "4.0")
from gi.repository import Gdk, GdkWayland, Gio, GLib, Gtk

os.environ["GDK_BACKEND"] = "wayland"


class Source(Gtk.Application):
    def __init__(self):
        super().__init__(application_id="io.screentogif.WaylandTestSource", flags=Gio.ApplicationFlags.NON_UNIQUE)
        self.tick = 0
        self.area = None

    def do_activate(self):
        assert isinstance(Gdk.Display.get_default(), GdkWayland.WaylandDisplay), "Source must be native Wayland"
        window = Gtk.ApplicationWindow(application=self, title="ScreenToGif Wayland test source")
        self.area = Gtk.DrawingArea(content_width=640, content_height=360)
        self.area.set_draw_func(self.draw)
        self.area.set_cursor_from_name("crosshair")
        self.pointer_reported = False
        motion = Gtk.EventControllerMotion()
        motion.connect("motion", self.pointer_motion)
        self.area.add_controller(motion)
        window.set_child(self.area)
        window.present()
        print("NATIVE_WAYLAND_SOURCE_READY", flush=True)
        GLib.timeout_add(100, self.advance)

    def pointer_motion(self, controller, x, y):
        if not self.pointer_reported and 40 <= x <= 80 and 40 <= y <= 80:
            self.pointer_reported = True
            print("POINTER_IN_RED", flush=True)

    def advance(self):
        self.tick += 1
        self.area.queue_draw()
        return True

    def draw(self, area, context, width, height):
        if self.tick % 10 == 0:
            print(f"SOURCE_DRAW tick={self.tick} logical={width}x{height} scale={area.get_scale_factor()}", file=sys.stderr, flush=True)
        context.set_source_rgb(0.05, 0.08, 0.12)
        context.paint()
        for column, color in enumerate([(1, 0, 0), (0, 1, 0), (0, 0, 1)]):
            context.set_source_rgb(*color)
            context.rectangle(20 + column * 100, 20, 80, 80)
            context.fill()
        context.set_source_rgb(1, 1, 1)
        context.set_font_size(28)
        context.move_to(20, 160)
        context.show_text(f"Native Wayland frame {self.tick:06d}")
        context.set_source_rgb(1, 0, 1)
        context.rectangle(width - 40, height - 40, 20, 20)
        context.fill()
        context.set_source_rgb(1, 0.7, 0)
        context.rectangle(20 + (self.tick * 7) % max(1, width - 80), height - 80, 40, 40)
        context.fill()


if __name__ == "__main__":
    sys.exit(Source().run(sys.argv))
