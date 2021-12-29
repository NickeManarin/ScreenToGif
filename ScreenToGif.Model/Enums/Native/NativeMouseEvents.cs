namespace ScreenToGif.Domain.Enums.Native;

public enum NativeMouseEvents
{
    MouseMove = 0x200,
    MouseDragStart = 0x00AE,
    MouseDragEnd = 0x00AF,

    LeftButtonDown = 0x201,
    LeftButtonUp = 0x202,
    LeftButtonDoubleClick = 0x203,
    OutsideLeftButtonDown = 0x00A1,
    OutsideLeftButtonUp = 0x00A2,
    OutsideLeftButtonDoubleClick = 0x00A3,

    RightButtonDown = 0x204,
    RightButtonUp = 0x205,
    RightButtonDoubleClick = 0x206,
    OutsideRightButtonDown = 0x00A4,
    OutsideRightButtonUp = 0x00A5,
    OutsideRightButtonDoubleClick = 0x00A6,

    MiddleButtonDown = 0x207,
    MiddleButtonUp = 0x208,
    MiddleButtonDoubleClick = 0x209,
    OutsideMiddleButtonDown = 0x00A7,
    OutsideMiddleButtonUp = 0x00A8,
    OutsideMiddleButtonDoubleClick = 0x00A9,

    MouseWheel = 0x020A,
    MouseWheelHorizontal = 0x020E,

    ExtraButtonDown = 0x020B,
    ExtraButtonUp = 0x020C,
    ExtraButtonDoubleClick = 0x020D,
    OutsideExtraButtonDown = 0x00AB,
    OutsideExtraButtonUp = 0x00AC,
    OutsideExtraButtonDoubleClick = 0x00AD
}