using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using System.Windows;
using System.Windows.Media;

namespace ScreenToGif.Domain.Models;

public class Frame : IFrame
{
    public string Path { get; set; }

    public string Name { get; set; }

    public int Delay { get; set; }

    public int CursorX { get; set; }

    public int CursorY { get; set; }

    public MouseButtons ButtonClicked { get; set; }

    public bool WasClicked { get; set; }

    public int Index { get; set; }

    public Int32Rect Rect { get; set; }

    public Color ColorKey { get; set; }

    public List<IKeyGesture> KeyList { get; set; }

    public byte[] Data { get; set; }

    public bool FrameSkipped { get; set; }

    public long DataLength { get; set; }
}
