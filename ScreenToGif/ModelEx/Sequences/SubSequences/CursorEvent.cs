using System;

namespace ScreenToGif.ModelEx.Sequences.SubSequences
{
    public class CursorEvent
    {
        public byte[] Pixels { get; set; }

        public double Left { get; set; }

        public double Top { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        //DPI, Depth?

        public bool IsLeftButtonDown { get; set; }

        public bool IsRightButtonDown { get; set; }

        public bool IsMiddleButtonDown { get; set; }

        public bool IsFourthButtonDown { get; set; }

        public bool IsFifthButtonDown { get; set; }

        public int MouseWheelDelta { get; set; }

        public bool IsMiddleScrollUp => MouseWheelDelta > 0; 

        public bool IsMiddleScrollDown => MouseWheelDelta < 0;

        public bool IsMiddleScroll => IsMiddleScrollUp || IsMiddleScrollDown;

        public TimeSpan TimeStamp { get; set; }
    }
}