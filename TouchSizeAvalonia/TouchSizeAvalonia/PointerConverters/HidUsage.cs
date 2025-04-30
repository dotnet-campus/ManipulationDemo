namespace TouchSizeAvalonia.PointerConverters;

/// <summary>
///
/// 
/// WISP pre-parsed these, WM_POINTER stack must do it itself
/// 
/// See Stylus\biblio.txt - 1
/// <see cref="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf"/> 
/// </summary>
/// Copy from https://github.com/dotnet/wpf
internal enum HidUsage
{
    X = 0x30,
    Y = 0x31,
    Z = 0x32,
    TipPressure = 0x30,
    BarrelPressure = 0x31,
    XTilt = 0x3D,
    YTilt = 0x3E,
    Azimuth = 0x3F,
    Altitude = 0x40,
    Twist = 0x41,
    TipSwitch = 0x42,
    SecondaryTipSwitch = 0x43,
    BarrelSwitch = 0x44,
    TouchConfidence = 0x47,
    Width = 0x48,
    Height = 0x49,
    TransducerSerialNumber = 0x5B,
}