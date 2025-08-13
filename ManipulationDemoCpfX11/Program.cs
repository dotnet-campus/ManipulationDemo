using CPF.Linux;

using ManipulationDemoCpfX11.TouchFramework;
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

        physicalWidth = (int) edidInfo.BasicDisplayParameters.MonitorPhysicalWidth.Value;
        physicalHeight = (int) edidInfo.BasicDisplayParameters.MonitorPhysicalHeight.Value;
    }
    else
    {
        Console.WriteLine($"读取 Edid 失败，错误原因：{readEdidInfoResult.ErrorMessage}");
    }
}
else
{
    Console.WriteLine("当前程序只能在 Linux 上运行");
    return;
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
SKTypeface typeface = SKFontManager.Default.MatchCharacter('十');
Console.WriteLine($"选用字体： SKTypeface={typeface?.FamilyName ?? "<null>"}");
skPaint.TextSize = 20;
skPaint.Typeface = typeface;
skPaint.Color = SKColors.Black;
skCanvas.Clear(SKColors.White.WithAlpha(0x5C));

var xiValuatorManager = new XIValuatorManager(display, handle);
xiValuatorManager.UpdateValuator();

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
        Draw();
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
                        if (dictionary.TryGetValue(xiDeviceEvent->detail, out var touchInfo))
                        {
                            touchInfo = touchInfo with
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

                            if (xiValuatorManager.TouchMajorValuatorClassInfo.HasValue)
                            {
                                if (valuatorDictionary.TryGetValue(xiValuatorManager.TouchMajorValuatorClassInfo.Value.Number, out var value) && (!ignoreZeroWidthHeight || value != 0))
                                {
                                    touchInfo = touchInfo with
                                    {
                                        TouchMajor = value,
                                    };
                                }
                                else
                                {

                                }
                            }

                            if (xiValuatorManager.TouchMinorValuatorClassInfo.HasValue)
                            {
                                if (valuatorDictionary.TryGetValue(xiValuatorManager.TouchMinorValuatorClassInfo.Value.Number, out var value) && (!ignoreZeroWidthHeight || value != 0))
                                {
                                    touchInfo = touchInfo with
                                    {
                                        TouchMinor = value,
                                    };
                                }
                                else
                                {

                                }
                            }

                            //if (orientationValuatorClassInfo.HasValue)
                            //{
                            //    if (valuatorDictionary.TryGetValue(orientationValuatorClassInfo.Value.Number,out var value))
                            //    {
                            //        Log($"Abs MT Orientation Value={value} Min={orientationValuatorClassInfo.Value.Min} Max={orientationValuatorClassInfo.Value.Max} Resolution={orientationValuatorClassInfo.Value.Resolution}");
                            //    }
                            //}

                            dictionary[xiDeviceEvent->detail] = touchInfo;
                            LogTouchInfo(touchInfo);
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
                    
                    InvalidateVisual();
                }
            }
            finally
            {
                XFreeEventData(display, data);
            }
        }
    }
}

void LogTouchInfo(TouchInfo value)
{
    string logMessage = $"Id={value.Id};X={value.X} Y={value.Y};TouchMajor={value.TouchMajor} TouchMinor={value.TouchMinor}";

    var touchSize = value.GetTouchSize(xiValuatorManager, physicalWidth, physicalHeight);

    double pixelWidth = touchSize.PixelWidth;
    double pixelHeight = touchSize.PixelHeight;
    double physicalWidthValue = touchSize.PhysicalCentimeterWidth;
    double physicalHeightValue = touchSize.PhysicalCentimeterHeight;

    logMessage += $" W={pixelWidth:0.00}px,{physicalWidthValue:0.00}cm H={pixelHeight:0.00}px,{physicalHeightValue:0.00}cm MajorValuatorMax={xiValuatorManager.TouchMajorValuatorClassInfo?.Max:0.00} MinorValuatorMax={xiValuatorManager.TouchMinorValuatorClassInfo?.Max:0.00}";

    Log(logMessage);
}

void InvalidateVisual()
{
    if (isSendExposeEvent)
    {
        return;
    }

    SendExposeEvent(display, handle, 0, 0, width, height);
    isSendExposeEvent = true;
}

void Draw()
{
    skCanvas.Clear(SKColors.White.WithAlpha(0x2C));

    foreach (var value in dictionary.Values)
    {
        if (xiValuatorManager.TouchMajorValuatorClassInfo != null)
        {
            var touchSize = value.GetTouchSize(xiValuatorManager, physicalWidth, physicalHeight);

            double pixelWidth = touchSize.PixelWidth;
            double pixelHeight = touchSize.PixelHeight;

            skCanvas.DrawRect((float) (value.X - pixelWidth / 2), (float) (value.Y - pixelHeight / 2), (float) pixelWidth, (float) pixelHeight, skPaint);
        }

        skPaint.IsLinearText = false;
        var text = $"""
                    Id={value.Id};X={value.X:0.00} Y={value.Y:0.00};W={value.TouchMajor:0.00} H={value.TouchMinor:0.00}
                    """;
        if (value.TouchStatus == TouchStatus.Up)
        {
            text = "[Up];" + text;
        }

        skPaint.Style = SKPaintStyle.Fill;
        skCanvas.DrawText(text, (float) value.X, (float) value.Y, skPaint);
        skPaint.Style = SKPaintStyle.Stroke;
    }
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

