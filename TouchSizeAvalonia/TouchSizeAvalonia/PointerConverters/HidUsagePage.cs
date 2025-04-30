namespace TouchSizeAvalonia.PointerConverters;

/// <summary>
///
/// WM_POINTER stack must parse out HID spec usage pages
/// <see cref="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf"/>
/// </summary>
/// Copy from https://github.com/dotnet/wpf
internal enum HidUsagePage : ushort
{
    Undefined = 0x00,
    Generic = 0x01,
    Simulation = 0x02,
    Vr = 0x03,
    Sport = 0x04,
    Game = 0x05,
    Keyboard = 0x07,
    Led = 0x08,
    Button = 0x09,
    Ordinal = 0x0a,
    Telephony = 0x0b,
    Consumer = 0x0c,
    Digitizer = 0x0d,
    Unicode = 0x10,
    Alphanumeric = 0x14,
    BarcodeScanner = 0x8C,
    WeighingDevice = 0x8D,
    MagneticStripeReader = 0x8E,
    CameraControl = 0x90,
    MicrosoftBluetoothHandsfree = 0xfff3,
}