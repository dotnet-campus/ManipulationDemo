﻿// Copy From: https://gitee.com/csharpui/CPF

using System;
using System.Runtime.InteropServices;
using Bool = System.Boolean;
using Atom = System.IntPtr;

namespace CPF.Linux
{
    [StructLayout(LayoutKind.Sequential)]
    public struct XScreenSaverInfo
    {
        public IntPtr window;
        public int state;
        public int kind;
        public ulong til_or_since;
        public ulong idle;
        public ulong eventMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XIAddMasterInfo
    {
        public int Type;
        public IntPtr Name;
        public Bool SendCore;
        public Bool Enable;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XIRemoveMasterInfo
    {
        public int Type;
        public int Deviceid;
        public int ReturnMode; /* AttachToMaster, Floating */
        public int ReturnPointer;
        public int ReturnKeyboard;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct XIAttachSlaveInfo
    {
        public int Type;
        public int Deviceid;
        public int NewMaster;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct XIDetachSlaveInfo
    {
        public int Type;
        public int Deviceid;
    };

    [StructLayout(LayoutKind.Explicit)]
    struct XIAnyHierarchyChangeInfo
    {
        [FieldOffset(0)]
        public int type; /* must be first element */
        [FieldOffset(4)]
        public XIAddMasterInfo add;
        [FieldOffset(4)]
        public XIRemoveMasterInfo remove;
        [FieldOffset(4)]
        public XIAttachSlaveInfo attach;
        [FieldOffset(4)]
        public XIDetachSlaveInfo detach;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct XIModifierState
    {
        public int Base;
        public int Latched;
        public int Locked;
        public int Effective;
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XIButtonState
    {
        public int MaskLen;
        public byte* Mask;
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XIValuatorState
    {
        public int MaskLen;
        public byte* Mask;
        public double* Values;
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XIEventMask
    {
        public int Deviceid;
        public int MaskLen;
        public int* Mask;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct XIAnyClassInfo
    {
        public XiDeviceClass Type;
        public int Sourceid;
    };

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XIButtonClassInfo
    {
        public int Type;
        public int Sourceid;
        public int NumButtons;
        public IntPtr* Labels;
        public XIButtonState State;
    };

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XIKeyClassInfo
    {
        public int Type;
        public int Sourceid;
        public int NumKeycodes;
        public int* Keycodes;
    };

    [StructLayout(LayoutKind.Sequential)]
    struct XIValuatorClassInfo
    {
        public int Type;
        public int Sourceid;
        public int Number;
        public IntPtr Label;
        public double Min;
        public double Max;
        public double Value;
        public int Resolution;
        public int Mode;
    };

    /* new in XI 2.1 */
    [StructLayout(LayoutKind.Sequential)]
    struct XIScrollClassInfo
    {
        public int Type;
        public int Sourceid;
        public int Number;
        public XiScrollType ScrollType;
        public double Increment;
        public int Flags;
    };

    enum XiScrollType
    {
        Vertical = 1,
        Horizontal = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XITouchClassInfo
    {
        public int Type;
        public int Sourceid;
        public int Mode;
        public int NumTouches;
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XIDeviceInfo
    {
        public int Deviceid;
        public IntPtr Name;
        public XiDeviceType Use;
        public int Attachment;
        public Bool Enabled;
        public int NumClasses;
        public XIAnyClassInfo** Classes;
    }

    public enum XiDeviceType
    {
        XIMasterPointer = 1,
        XIMasterKeyboard = 2,
        XISlavePointer = 3,
        XISlaveKeyboard = 4,
        XIFloatingSlave = 5
    }

    enum XiPredefinedDeviceId : int
    {
        XIAllDevices = 0,
        XIAllMasterDevices = 1
    }

    public enum XiDeviceClass
    {
        XIKeyClass = 0,
        XIButtonClass = 1,
        XIValuatorClass = 2,
        XIScrollClass = 3,
        XITouchClass = 8,
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct XIDeviceChangedEvent
    {
        public int Type; /* GenericEvent */
        public ulong Serial; /* # of last request processed by server */
        public Bool SendEvent; /* true if this came from a SendEvent request */
        public IntPtr Display; /* Display the event was read from */
        public int Extension; /* XI extension offset */
        public int Evtype; /* XI_DeviceChanged */
        public IntPtr Time;
        public int Deviceid; /* id of the device that changed */
        public int Sourceid; /* Source for the new classes. */
        public int Reason; /* Reason for the change */
        public int NumClasses;
        public XIAnyClassInfo** Classes; /* same as in XIDeviceInfo */
    }

    [StructLayout(LayoutKind.Sequential)]
    struct XIDeviceEvent
    {
        public XEventName type; /* GenericEvent */
        public ulong serial; /* # of last request processed by server */
        public Bool send_event; /* true if this came from a SendEvent request */
        public IntPtr display; /* Display the event was read from */
        public int extension; /* XI extension offset */
        public XiEventType evtype;
        public IntPtr time;
        public int deviceid;
        public int sourceid;
        public int detail;
        public IntPtr RootWindow;
        public IntPtr EventWindow;
        public IntPtr ChildWindow;
        public double root_x;
        public double root_y;
        public double event_x;
        public double event_y;
        public XiDeviceEventFlags flags;
        public XIButtonState buttons;
        public XIValuatorState valuators;
        public XIModifierState mods;
        public XIModifierState group;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct XIEnterLeaveEvent
    {
        public XEventName type; /* GenericEvent */
        public UIntPtr serial; /* # of last request processed by server */
        public Bool send_event; /* true if this came from a SendEvent request */
        public IntPtr display; /* Display the event was read from */
        public int extension; /* XI extension offset */
        public XiEventType evtype;
        public IntPtr time;
        public int deviceid;
        public int sourceid;
        public XiEnterLeaveDetail detail;
        public IntPtr RootWindow;
        public IntPtr EventWindow;
        public IntPtr ChildWindow;
        public double root_x;
        public double root_y;
        public double event_x;
        public double event_y;
        public int mode;
        public int focus;
        public int same_screen;
        public XIButtonState buttons;
        public XIModifierState mods;
        public XIModifierState group;
    }
    internal enum XiEnterLeaveDetail
    {
        XINotifyAncestor = 0,
        XINotifyVirtual = 1,
        XINotifyInferior = 2,
        XINotifyNonlinear = 3,
        XINotifyNonlinearVirtual = 4,
        XINotifyPointer = 5,
        XINotifyPointerRoot = 6,
        XINotifyDetailNone = 7

    }

    [Flags]
    public enum XiDeviceEventFlags : int
    {
        None = 0,
        XIPointerEmulated = (1 << 16)
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XIEvent
    {
        public int type; /* GenericEvent */
        public ulong serial; /* # of last request processed by server */
        public Bool send_event; /* true if this came from a SendEvent request */
        public IntPtr display; /* Display the event was read from */
        public int extension; /* XI extension offset */
        public XiEventType evtype;
        public IntPtr time;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XRectangle
    {
        public short x, y;
        public ushort width, height;
    }

    public enum XiEventType
    {
        XI_DeviceChanged = 1,
        XI_KeyPress = 2,
        XI_KeyRelease = 3,
        XI_ButtonPress = 4,
        XI_ButtonRelease = 5,
        XI_Motion = 6,
        XI_Enter = 7,
        XI_Leave = 8,
        XI_FocusIn = 9,
        XI_FocusOut = 10,
        XI_HierarchyChanged = 11,
        XI_PropertyEvent = 12,
        XI_RawKeyPress = 13,
        XI_RawKeyRelease = 14,
        XI_RawButtonPress = 15,
        XI_RawButtonRelease = 16,
        XI_RawMotion = 17,
        XI_TouchBegin = 18 /* XI 2.2 */,
        XI_TouchUpdate = 19,
        XI_TouchEnd = 20,
        XI_TouchOwnership = 21,
        XI_RawTouchBegin = 22,
        XI_RawTouchUpdate = 23,
        XI_RawTouchEnd = 24,
        XI_BarrierHit = 25 /* XI 2.3 */,
        XI_BarrierLeave = 26,
        XI_LASTEVENT = XI_BarrierLeave,
    }
}
