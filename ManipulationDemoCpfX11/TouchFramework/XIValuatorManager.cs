using CPF.Linux;

namespace ManipulationDemoCpfX11.TouchFramework;

public unsafe class XIValuatorManager
{
    public XIValuatorManager(nint display, nint x11WindowXId)
    {
        Display = display;
        _x11WindowXId = x11WindowXId;
    }

    internal XIValuatorClassInfo? TouchMajorValuatorClassInfo { get; private set; } = null;

    internal XIValuatorClassInfo? TouchMinorValuatorClassInfo { get; private set; } = null;

    internal XIValuatorClassInfo? PressureValuatorClassInfo { get; private set; } = null;

    internal XIValuatorClassInfo? OrientationValuatorClassInfo { get; private set; } = null;

    private readonly nint _x11WindowXId;

    public IntPtr Display { get; }

    public void UpdateValuator()
    {
        Console.WriteLine($"Start UpdateValuator ============");

        var touchMajorAtom = XLib.XInternAtom(Display, "Abs MT Touch Major", false);
        var touchMinorAtom = XLib.XInternAtom(Display, "Abs MT Touch Minor", false);
        var pressureAtom = XLib.XInternAtom(Display, "Abs MT Pressure", false);
        var orientationAtom = XLib.XInternAtom(Display, "Abs MT Orientation", false);

        Console.WriteLine($"ABS_MT_TOUCH_MAJOR={touchMajorAtom} Name={XLib.GetAtomName(Display, touchMajorAtom)} ABS_MT_TOUCH_MINOR={touchMinorAtom} Name={XLib.GetAtomName(Display, touchMinorAtom)} Abs_MT_Pressure={pressureAtom} Name={XLib.GetAtomName(Display, pressureAtom)} Abs_MT_Orientation={orientationAtom} Name={XLib.GetAtomName(Display, orientationAtom)}");

        var devices = (XIDeviceInfo*) XLib.XIQueryDevice(Display,
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
            var valuators = new List<XIValuatorClassInfo>();
            var scrollers = new List<XIScrollClassInfo>();

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

            XLib.XiSelectEvents(Display, _x11WindowXId,
                new Dictionary<int, List<XiEventType>> { [pointerDevice.Value.Deviceid] = multiTouchEventTypes });

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
                    Console.WriteLine(
                        $"TouchMajorAtom Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution}");

                    TouchMajorValuatorClassInfo = xiValuatorClassInfo;
                }
                else if (xiValuatorClassInfo.Label == touchMinorAtom)
                {
                    Console.WriteLine(
                        $"TouchMinorAtom Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution}");

                    TouchMinorValuatorClassInfo = xiValuatorClassInfo;
                }
                else if (xiValuatorClassInfo.Label == pressureAtom)
                {
                    Console.WriteLine(
                        $"PressureAtom Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution}");

                    PressureValuatorClassInfo = xiValuatorClassInfo;
                }
                else if (xiValuatorClassInfo.Label == orientationAtom)
                {
                    Console.WriteLine(
                        $"OrientationAtom Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution}");
                    OrientationValuatorClassInfo = xiValuatorClassInfo;
                }
                else
                {
                    Console.WriteLine(
                        $"XiValuatorClassInfo Label={xiValuatorClassInfo.Label}({XLib.GetAtomName(Display, xiValuatorClassInfo.Label)} Value={xiValuatorClassInfo.Value}; Max={xiValuatorClassInfo.Max:0.00}; Min={xiValuatorClassInfo.Min:0.00}; Resolution={xiValuatorClassInfo.Resolution})");
                }
            }

            if (TouchMajorValuatorClassInfo is null)
            {
                Console.WriteLine("Can't find TouchMajorAtom 丢失触摸宽度高度");
            }
        }
        else
        {
            Console.WriteLine("pointerDevice==null");
        }

        Console.WriteLine($"End UpdateValuator ============");
    }
}