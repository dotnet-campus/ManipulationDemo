using System.Diagnostics;
using CPF.Linux;
using ManipulationDemoCpfX11.Utils;
using SkiaSharp;

using static CPF.Linux.XLib;

XInitThreads();
var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);
var rootWindow = XDefaultRootWindow(display);

XMatchVisualInfo(display, screen, 32, 4, out var info);
var visual = info.visual;

var valueMask =
        //SetWindowValuemask.BackPixmap
        0
        | SetWindowValuemask.BackPixel
        | SetWindowValuemask.BorderPixel
        | SetWindowValuemask.BitGravity
        | SetWindowValuemask.WinGravity
        | SetWindowValuemask.BackingStore
        | SetWindowValuemask.ColorMap
    //| SetWindowValuemask.OverrideRedirect
    ;
var xSetWindowAttributes = new XSetWindowAttributes
{
    backing_store = 1,
    bit_gravity = Gravity.NorthWestGravity,
    win_gravity = Gravity.NorthWestGravity,
    //override_redirect = true, // 设置窗口的override_redirect属性为True，以避免窗口管理器的干预
    colormap = XCreateColormap(display, rootWindow, visual, 0),
    border_pixel = 0,
    background_pixel = 0,
};

var xDisplayWidth = XDisplayWidth(display, screen);
var xDisplayHeight = XDisplayHeight(display, screen);

var width = xDisplayWidth;
var height = xDisplayHeight;

// 忽略 0 宽度高度
bool ignoreZeroWidthHeight = true;

Console.WriteLine($"Display WH={width},{height}");

int physicalWidth = -1;
int physicalHeight = -1;

if (OperatingSystem.IsLinux())
{
    var readEdidInfoResult = EdidInfo.ReadFormLinux();
    if (readEdidInfoResult.IsSuccess)
    {
        var edidInfo = readEdidInfoResult.EdidInfo;
        Console.WriteLine($"读取 Edid 成功，屏幕宽高：{edidInfo.BasicDisplayParameters.MonitorPhysicalWidth.Value}cmx{edidInfo.BasicDisplayParameters.MonitorPhysicalHeight.Value}cm");

        physicalWidth = (int)edidInfo.BasicDisplayParameters.MonitorPhysicalWidth.Value;
        physicalHeight = (int) edidInfo.BasicDisplayParameters.MonitorPhysicalHeight.Value;
    }
    else
    {
        Console.WriteLine($"读取 Edid 失败，错误原因：{readEdidInfoResult.ErrorMessage}");
    }
}


var handle = XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
visual,
(nuint) valueMask, ref xSetWindowAttributes);
XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                         XEventMask.PointerMotionHintMask;
var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
XSelectInput(display, handle, mask);

XMapWindow(display, handle);
XFlush(display);

var gc = XCreateGC(display, handle, 0, 0);
var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
var skCanvas = new SKCanvas(skBitmap);
var xImage = CreateImage(skBitmap);

using var skPaint = new SKPaint();
skPaint.Color = SKColors.Black;
skPaint.StrokeWidth = 2;
skPaint.Style = SKPaintStyle.Stroke;
skPaint.IsAntialias = true;

// 随意用一个支持中文的字体
var typeface = SKFontManager.Default.MatchCharacter('十');
skPaint.TextSize = 20;
skPaint.Typeface = typeface;
skPaint.Color = SKColors.Black;
skCanvas.Clear(SKColors.White.WithAlpha(0x6C));

var touchMajorAtom = XInternAtom(display, "Abs MT Touch Major", false);
var touchMinorAtom = XInternAtom(display, "Abs MT Touch Minor", false);
var pressureAtom = XInternAtom(display, "Abs MT Pressure", false);
var orientationAtom = XInternAtom(display, "Abs MT Orientation",false);

Console.WriteLine($"ABS_MT_TOUCH_MAJOR={touchMajorAtom} Name={XLib.GetAtomName(display, touchMajorAtom)} ABS_MT_TOUCH_MINOR={touchMinorAtom} Name={XLib.GetAtomName(display, touchMinorAtom)} Abs_MT_Pressure={pressureAtom} Name={XLib.GetAtomName(display, pressureAtom)} Abs_MT_Orientation={orientationAtom} Name={XLib.GetAtomName(display, orientationAtom)}");

var valuators = new List<XIValuatorClassInfo>();
var scrollers = new List<XIScrollClassInfo>();

XIValuatorClassInfo? touchMajorValuatorClassInfo = null;
XIValuatorClassInfo? touchMinorValuatorClassInfo = null;
XIValuatorClassInfo? pressureValuatorClassInfo = null;
XIValuatorClassInfo? orientationValuatorClassInfo = null;

unsafe
{
    var devices = (XIDeviceInfo*) XIQueryDevice(display,
        (int) XiPredefinedDeviceId.XIAllMasterDevices, out int num);

    XIDeviceInfo? pointerDevice = default;
    for (var c = 0; c < num; c++)
    {
        Console.WriteLine($"XIDeviceInfo [{c}] {devices[c].Deviceid} {devices[c].Use}");

        if (devices[c].Use == XiDeviceType.XIMasterPointer)
        {
            pointerDevice ??= devices[c];
            // 特意不用 break; 多次进入循环，用于输出更多调试信息
            continue;
        }
    }

    if (pointerDevice != null)
    {
        var multiTouchEventTypes = new List<XiEventType>
        {
            XiEventType.XI_TouchBegin,
            XiEventType.XI_TouchUpdate,
            XiEventType.XI_TouchEnd,

            XiEventType.XI_Motion,
            XiEventType.XI_ButtonPress,
            XiEventType.XI_ButtonRelease,
            XiEventType.XI_Leave,
            XiEventType.XI_Enter,
        };

        XiSelectEvents(display, handle, new Dictionary<int, List<XiEventType>> { [pointerDevice.Value.Deviceid] = multiTouchEventTypes });

        for (int i = 0; i < pointerDevice.Value.NumClasses; i++)
        {
            var xiAnyClassInfo = pointerDevice.Value.Classes[i];
            if (xiAnyClassInfo->Type == XiDeviceClass.XIValuatorClass)
            {
                valuators.Add(*((XIValuatorClassInfo**) pointerDevice.Value.Classes)[i]);
            }
            else if (xiAnyClassInfo->Type == XiDeviceClass.XIScrollClass)
            {
                scrollers.Add(*((XIScrollClassInfo**) pointerDevice.Value.Classes)[i]);
            }
        }

        foreach (var xiValuatorClassInfo in valuators)
        {
            if (xiValuatorClassInfo.Label == touchMajorAtom)
            {
                Console.WriteLine($"TouchMajorAtom Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution}");

                touchMajorValuatorClassInfo = xiValuatorClassInfo;
            }
            else if (xiValuatorClassInfo.Label == touchMinorAtom)
            {
                Console.WriteLine($"TouchMinorAtom Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution}");

                touchMinorValuatorClassInfo = xiValuatorClassInfo;
            }
            else if (xiValuatorClassInfo.Label == pressureAtom)
            {
                Console.WriteLine($"PressureAtom Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution}");

                pressureValuatorClassInfo = xiValuatorClassInfo;
            }
            else if (xiValuatorClassInfo.Label == orientationAtom)
            {
                Console.WriteLine($"OrientationAtom Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution}");
                orientationValuatorClassInfo = xiValuatorClassInfo;
            }
            else
            {
                Console.WriteLine($"XiValuatorClassInfo Label={xiValuatorClassInfo.Label}({XLib.GetAtomName(display, xiValuatorClassInfo.Label)} Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution})");
            }
        }

        if (touchMajorValuatorClassInfo is null)
        {
            Console.WriteLine("Can't find TouchMajorAtom 丢失触摸宽度高度");
        }
    }
    else
    {
        Console.WriteLine("pointerDevice==null");
    }
}

var dictionary = new Dictionary<int, TouchInfo>();
bool isSendExposeEvent = false;

var valuatorDictionary = new Dictionary<int, double>();

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);

    if (xNextEvent != 0)
    {
        break;
    }

    if (@event.type == XEventName.Expose)
    {
        XPutImage(display, handle, gc, ref xImage, @event.ExposeEvent.x, @event.ExposeEvent.y, @event.ExposeEvent.x, @event.ExposeEvent.y, (uint) @event.ExposeEvent.width,
            (uint) @event.ExposeEvent.height);
        isSendExposeEvent = false;
    }
    else if (@event.type == XEventName.GenericEvent)
    {
        unsafe
        {
            void* data = &@event.GenericEventCookie;
            XGetEventData(display, data);

            try
            {
                var xiEvent = (XIEvent*) @event.GenericEventCookie.data;
                if (xiEvent->evtype is
                    XiEventType.XI_ButtonRelease
                    or XiEventType.XI_ButtonRelease
                    or XiEventType.XI_Motion
                    or XiEventType.XI_TouchBegin
                    or XiEventType.XI_TouchUpdate
                    or XiEventType.XI_TouchEnd)
                {
                    var xiDeviceEvent = (XIDeviceEvent*) xiEvent;

                    var x = xiDeviceEvent->event_x;
                    var y = xiDeviceEvent->event_y;
                    if (xiEvent->evtype == XiEventType.XI_TouchBegin)
                    {
                        dictionary[xiDeviceEvent->detail] = new TouchInfo(xiDeviceEvent->detail, x, y, -1, -1, TouchStatus.Down);
                    }
                    else if (xiEvent->evtype == XiEventType.XI_TouchUpdate)
                    {
                        if (dictionary.TryGetValue(xiDeviceEvent->detail, out var t))
                        {
                            t = t with
                            {
                                X = x,
                                Y = y,
                                TouchStatus = TouchStatus.Move,
                            };

                            valuatorDictionary.Clear();
                            var values = xiDeviceEvent->valuators.Values;
                            for (var c = 0; c < xiDeviceEvent->valuators.MaskLen * 8/*一个 Byte 有 8 个 bit，以下 XIMaskIsSet 是按照 bit 进行判断的*/; c++)
                            {
                                if (XIMaskIsSet(xiDeviceEvent->valuators.Mask, c))
                                {
                                    // 只有 Mask 存在值的，才能获取 Values 的值
                                    valuatorDictionary[c] = *values;
                                    values++;
                                }
                            }

                            if (touchMajorValuatorClassInfo.HasValue)
                            {
                                if (valuatorDictionary.TryGetValue(touchMajorValuatorClassInfo.Value.Number, out var value) && (!ignoreZeroWidthHeight || value != 0))
                                {
                                    t = t with
                                    {
                                        TouchMajor = value,
                                    };
                                }
                                else
                                {

                                }
                            }

                            if (touchMinorValuatorClassInfo.HasValue)
                            {
                                if (valuatorDictionary.TryGetValue(touchMinorValuatorClassInfo.Value.Number, out var value) && (!ignoreZeroWidthHeight || value != 0))
                                {
                                    t = t with
                                    {
                                        TouchMinor = value,
                                    };
                                }
                                else
                                {

                                }
                            }

                            if (orientationValuatorClassInfo.HasValue)
                            {
                                if (valuatorDictionary.TryGetValue(orientationValuatorClassInfo.Value.Number,out var value))
                                {
                                    Log($"Abs MT Orientation Value={value} Min={orientationValuatorClassInfo.Value.Min} Max={orientationValuatorClassInfo.Value.Max} Resolution={orientationValuatorClassInfo.Value.Resolution}");
                                }
                            }

                            dictionary[xiDeviceEvent->detail] = t;
                        }
                    }
                    else if (xiEvent->evtype == XiEventType.XI_TouchEnd)
                    {
                        if (dictionary.TryGetValue(xiDeviceEvent->detail, out var t))
                        {
                            dictionary[xiDeviceEvent->detail] = t with
                            {
                                X = x,
                                Y = y,
                                TouchStatus = TouchStatus.Up,
                            };
                        }
                    }

                    Draw();
                }
            }
            finally
            {

            }
        }
    }
}

void Draw()
{
    skCanvas.Clear(SKColors.White.WithAlpha(0x6C));

    foreach (var value in dictionary.Values)
    {
        string logMessage = $"Id={value.Id};X={value.X} Y={value.Y};TouchMajor={value.TouchMajor} TouchMinor={value.TouchMinor}";

        if (touchMajorValuatorClassInfo != null)
        {
            var touchMajorScale = value.TouchMajor / touchMajorValuatorClassInfo.Value.Max;
            double pixelWidth = touchMajorScale * xDisplayWidth;
            double pixelHeight;
            double physicalWidthValue = double.NaN;
            double physicalHeightValue = double.NaN;

            if (physicalWidth > 0)
            {
                physicalWidthValue = touchMajorScale * physicalWidth;
            }

            if (touchMinorValuatorClassInfo is null)
            {
                pixelHeight = pixelWidth;
            }
            else
            {
                var touchMinorScale = value.TouchMinor / touchMinorValuatorClassInfo.Value.Max;
                pixelHeight = touchMinorScale * xDisplayHeight;

                if (physicalHeight > 0)
                {
                    physicalHeightValue = touchMinorScale * physicalHeight;
                }
            }

            skCanvas.DrawRect((float) (value.X - pixelWidth / 2), (float) (value.Y - pixelHeight / 2), (float) pixelWidth, (float) pixelHeight, skPaint);

            logMessage += $" W={pixelWidth}px,{physicalWidthValue}cm H={pixelHeight}px,{physicalHeightValue}cm MajorValuator={touchMajorValuatorClassInfo.Value.Max} MinorValuator={touchMinorValuatorClassInfo?.Max}";
        }

        skPaint.IsLinearText = false;
        var text = $"""
                    Id={value.Id};X={value.X} Y={value.Y};W={value.TouchMajor} H={value.TouchMinor}
                    """;
        if (value.TouchStatus == TouchStatus.Up)
        {
            text = "[已抬起];" + text;
        }

        skCanvas.DrawText(text, (float) value.X, (float) value.Y, skPaint);

        Log(logMessage);
    }

    if (isSendExposeEvent)
    {
        return;
    }

    SendExposeEvent(display, handle, 0, 0, width, height);
    isSendExposeEvent = true;
}

void Log(string message)
{
    Console.WriteLine(message);
    var logFile = Path.Join(AppContext.BaseDirectory, $"Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
    File.AppendAllLines(logFile, [$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff}] {message}"]);
}


static XImage CreateImage(SKBitmap skBitmap)
{
    const int bytePerPixelCount = 4; // RGBA 一共4个 byte 长度
    var bitPerByte = 8;

    var bitmapWidth = skBitmap.Width;
    var bitmapHeight = skBitmap.Height;

    var img = new XImage();
    int bitsPerPixel = bytePerPixelCount * bitPerByte;
    img.width = bitmapWidth;
    img.height = bitmapHeight;
    img.format = 2; //ZPixmap;
    img.data = skBitmap.GetPixels();
    img.byte_order = 0; // LSBFirst;
    img.bitmap_unit = bitsPerPixel;
    img.bitmap_bit_order = 0; // LSBFirst;
    img.bitmap_pad = bitsPerPixel;
    img.depth = bitsPerPixel;
    img.bytes_per_line = bitmapWidth * bytePerPixelCount;
    img.bits_per_pixel = bitsPerPixel;
    XInitImage(ref img);

    return img;
}

static void SendExposeEvent(IntPtr display, IntPtr window, int x, int y, int width, int height)
{
    var exposeEvent = new XExposeEvent
    {
        type = XEventName.Expose,
        display = display,
        window = window,
        x = x,
        y = y,
        width = width,
        height = height,
        count = 1,
    };

    var xEvent = new XEvent
    {
        ExposeEvent = exposeEvent
    };

    XSendEvent(display, window, false, new IntPtr((int) (EventMask.ExposureMask)), ref xEvent);
    XFlush(display);
}

record TouchInfo(int Id, double X, double Y, double TouchMajor, double TouchMinor, TouchStatus TouchStatus)
{
}

enum TouchStatus
{
    Down,
    Move,
    Up,
}