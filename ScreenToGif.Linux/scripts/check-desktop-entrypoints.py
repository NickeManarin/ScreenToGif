#!/usr/bin/env python3
"""Close test-owned CLI/startup windows through their real window-manager protocol."""

import ctypes
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time


class ClientMessage(ctypes.Structure):
    _fields_ = [("type", ctypes.c_int), ("serial", ctypes.c_ulong),
                ("send_event", ctypes.c_int), ("display", ctypes.c_void_p),
                ("window", ctypes.c_ulong), ("message_type", ctypes.c_ulong),
                ("format", ctypes.c_int), ("data", ctypes.c_long * 5)]


class XEvent(ctypes.Union):
    _fields_ = [("client", ClientMessage), ("padding", ctypes.c_long * 24)]


def windows(process, title):
    result = subprocess.run(
        ["xdotool", "search", "--all", "--onlyvisible", "--pid", str(process.pid), "--name", title],
        capture_output=True, text=True, check=False, timeout=5)
    if result.returncode not in (0, 1):
        raise RuntimeError(result.stderr)
    return result.stdout.split()


def wait_for(process, title):
    until = time.monotonic() + 20
    while time.monotonic() < until:
        found = windows(process, title)
        if found:
            assert len(found) == 1, f"Duplicate windows: {title}: {found}"
            return int(found[0])
        assert process.poll() is None, f"Application exited while waiting for {title}"
        time.sleep(0.05)
    raise TimeoutError(f"Window did not appear: {title}")


def close_owned_window(process, window):
    owner = subprocess.check_output(["xdotool", "getwindowpid", str(window)], text=True, timeout=5).strip()
    assert owner == str(process.pid), "Refusing to close another application's window"
    x11 = ctypes.CDLL("libX11.so.6")
    x11.XOpenDisplay.argtypes = [ctypes.c_char_p]
    x11.XOpenDisplay.restype = ctypes.c_void_p
    x11.XInternAtom.argtypes = [ctypes.c_void_p, ctypes.c_char_p, ctypes.c_int]
    x11.XInternAtom.restype = ctypes.c_ulong
    x11.XSendEvent.argtypes = [ctypes.c_void_p, ctypes.c_ulong, ctypes.c_int, ctypes.c_long, ctypes.POINTER(XEvent)]
    x11.XFlush.argtypes = [ctypes.c_void_p]
    x11.XCloseDisplay.argtypes = [ctypes.c_void_p]
    display = x11.XOpenDisplay(None)
    if not display:
        raise RuntimeError("Could not open the test display")
    try:
        event = XEvent()
        event.client.type = 33
        event.client.send_event = 1
        event.client.display = display
        event.client.window = window
        event.client.message_type = x11.XInternAtom(display, b"WM_PROTOCOLS", 0)
        event.client.format = 32
        event.client.data[0] = x11.XInternAtom(display, b"WM_DELETE_WINDOW", 0)
        if not x11.XSendEvent(display, window, 0, 0, ctypes.byref(event)):
            raise RuntimeError("The test window did not accept WM_DELETE_WINDOW")
        x11.XFlush(display)
    finally:
        x11.XCloseDisplay(display)


def main():
    executable, evidence_path = sys.argv[1:]
    evidence = Path(evidence_path)
    evidence.mkdir(parents=True, exist_ok=True)
    for route, arguments, startup in [("command-line", ["--editor"], "Startup"),
                                      ("settings", [], "Editor")]:
        for tray in (False, True):
            name = f"{route}-tray-{tray}"
            with tempfile.TemporaryDirectory(prefix="screentogif-entrypoint-") as config:
                settings = Path(config) / "ScreenToGif" / "settings.json"
                settings.parent.mkdir()
                settings.write_text(json.dumps({"StartupWindow": startup, "ShowNotificationIcon": tray,
                                               "KeepOpen": tray, "AskBeforeCloseEditor": False,
                                               "RemoveOldProjects": False, "DeleteCacheOnClose": False}))
                with (evidence / f"{name}.log").open("w") as log:
                    process = subprocess.Popen([executable, *arguments, "--new-instance"],
                                               env={**os.environ, "XDG_CONFIG_HOME": config}, stdout=log, stderr=log)
                    try:
                        editor = wait_for(process, "ScreenToGif - Editor")
                        close_owned_window(process, editor)
                        wait_for(process, "ScreenToGif - StartUp")
                        until = time.monotonic() + 20
                        while windows(process, "ScreenToGif - Editor"):
                            if time.monotonic() >= until:
                                raise TimeoutError("Editor remained after close")
                            time.sleep(0.05)
                        with (evidence / "entrypoints.txt").open("a") as report:
                            report.write(f"PASS: {name}: WM_DELETE_WINDOW returns to exactly one StartUp\n")
                    finally:
                        if process.poll() is None:
                            process.terminate()
                            try:
                                process.wait(timeout=10)
                            except subprocess.TimeoutExpired:
                                process.kill()
                                process.wait(timeout=5)


if __name__ == "__main__":
    main()
