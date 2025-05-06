using System;
using System.Runtime.Versioning;
using System.Text;

using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Input.Pointer;

using static Windows.Win32.PInvoke;

namespace TouchSizeAvalonia.PointerConverters;

[SupportedOSPlatform("windows8.0")]
internal static class PointerConverter
{
    public static unsafe RawPointerPoint ToRawPointerPoint(uint pointerId)
    {
        GetPointerTouchInfo(pointerId, out POINTER_TOUCH_INFO info);
        POINTER_INFO pointerInfo = info.pointerInfo;

        global::Windows.Win32.Foundation.RECT pointerDeviceRect = default;
        global::Windows.Win32.Foundation.RECT displayRect = default;

        GetPointerDeviceRects(pointerInfo.sourceDevice, &pointerDeviceRect, &displayRect);

        uint propertyCount = 0;
        GetPointerDeviceProperties(pointerInfo.sourceDevice, &propertyCount, null);
        POINTER_DEVICE_PROPERTY* pointerDevicePropertyArray = stackalloc POINTER_DEVICE_PROPERTY[(int) propertyCount];
        GetPointerDeviceProperties(pointerInfo.sourceDevice, &propertyCount, pointerDevicePropertyArray);
        var pointerDevicePropertySpan =
            new Span<POINTER_DEVICE_PROPERTY>(pointerDevicePropertyArray, (int) propertyCount);

        GetPointerCursorId(pointerId, out uint cursorId);

        var touchInfo = new StringBuilder();
        touchInfo.Append($"[{DateTime.Now}] ");
        touchInfo.AppendLine($"PointerId={pointerId} CursorId={cursorId} PointerDeviceRect={RectToString(pointerDeviceRect)} DisplayRect={RectToString(displayRect)} PropertyCount={propertyCount} SourceDevice={pointerInfo.sourceDevice}");

        var xPropertyIndex = -1;
        var yPropertyIndex = -1;
        var contactIdentifierPropertyIndex = -1;
        var widthPropertyIndex = -1;
        var heightPropertyIndex = -1;

        for (var i = 0; i < pointerDevicePropertySpan.Length; i++)
        {
            POINTER_DEVICE_PROPERTY pointerDeviceProperty = pointerDevicePropertySpan[i];
            var usagePageId = pointerDeviceProperty.usagePageId;
            var usageId = pointerDeviceProperty.usageId;
            // 单位
            var unit = pointerDeviceProperty.unit;
            // 单位指数。 它与 Unit 字段一起定义了设备报告中数据的物理单位。具体来说：
            // - Unit：定义了数据的基本单位，例如厘米、英寸、弧度等。
            // - UnitExponent：表示单位的数量级（即 10 的幂次）。它用于缩放单位值，使其适应不同的范围
            var unitExponent = pointerDeviceProperty.unitExponent;
            touchInfo.Append(
                $"{UsagePageAndIdConverter.ConvertToString(usagePageId, usageId)} Unit={StylusPointPropertyUnitHelper.FromPointerUnit(unit)}({unit}) UnitExponent={unitExponent}")
                .Append($"  LogicalMin={pointerDeviceProperty.logicalMin} LogicalMax={pointerDeviceProperty.logicalMax}")
                .Append($"  PhysicalMin={pointerDeviceProperty.physicalMin} PhysicalMax={pointerDeviceProperty.physicalMax}")
                .AppendLine();

            if (usagePageId == (ushort) HidUsagePage.Generic)
            {
                if (usageId == (ushort) HidUsage.X)
                {
                    xPropertyIndex = i;
                }
                else if (usageId == (ushort) HidUsage.Y)
                {
                    yPropertyIndex = i;
                }
            }
            else if (usagePageId == (ushort) HidUsagePage.Digitizer)
            {
                if (usageId == (ushort) DigitizersUsageId.Width)
                {
                    widthPropertyIndex = i;
                }
                else if (usageId == (ushort) DigitizersUsageId.Height)
                {
                    heightPropertyIndex = i;
                }
                else if (usageId == (ushort) DigitizersUsageId.ContactIdentifier)
                {
                    contactIdentifierPropertyIndex = i;
                }
            }
        }

        var historyCount = pointerInfo.historyCount;
        int[] rawPointerData = new int[propertyCount * historyCount];

        fixed (int* pValue = rawPointerData)
        {
            GetRawPointerDeviceData(pointerId, historyCount, propertyCount, pointerDevicePropertyArray, pValue);
        }

        var rawPointerPoint = new RawPointerPoint();

        for (int i = 0; i < historyCount; i++)
        {
            var baseIndex = i * propertyCount;

            if (xPropertyIndex >= 0 && yPropertyIndex >= 0)
            {
                var xValue = rawPointerData[baseIndex + xPropertyIndex];
                var yValue = rawPointerData[baseIndex + yPropertyIndex];
                var xProperty = pointerDevicePropertySpan[xPropertyIndex];
                var yProperty = pointerDevicePropertySpan[yPropertyIndex];

                var xForScreen = ((double) xValue - xProperty.logicalMin) /
                    (xProperty.logicalMax - xProperty.logicalMin) * displayRect.Width;
                var yForScreen = ((double) yValue - yProperty.logicalMin) /
                    (yProperty.logicalMax - yProperty.logicalMin) * displayRect.Height;

                rawPointerPoint = rawPointerPoint with
                {
                    X = xForScreen,
                    Y = yForScreen,
                };
            }

            if (contactIdentifierPropertyIndex >= 0)
            {
                // 这里的 Id 关联会出现 id 重复的问题，似乎是在上层处理的
                var contactIdentifierValue = rawPointerData[baseIndex + contactIdentifierPropertyIndex];

                rawPointerPoint = rawPointerPoint with
                {
                    Id = contactIdentifierValue
                };
            }

            if (widthPropertyIndex >= 0 && heightPropertyIndex >= 0)
            {
                var widthValue = rawPointerData[baseIndex + widthPropertyIndex];
                var heightValue = rawPointerData[baseIndex + heightPropertyIndex];

                var widthProperty = pointerDevicePropertySpan[widthPropertyIndex];
                var heightProperty = pointerDevicePropertySpan[heightPropertyIndex];

                var widthScale = ((double) widthValue - widthProperty.logicalMin) /
                                              (widthProperty.logicalMax - widthProperty.logicalMin);

                var heightScale = ((double) heightValue - heightProperty.logicalMin) / (heightProperty.logicalMax - heightProperty.logicalMin);

                var widthPixel = widthScale * displayRect.Width;
                var heightPixel = heightScale * displayRect.Height;

                rawPointerPoint = rawPointerPoint with
                {
                    PixelWidth = widthPixel,
                    PixelHeight = heightPixel,
                };

                if (StylusPointPropertyUnitHelper.FromPointerUnit(widthProperty.unit) ==
                    StylusPointPropertyUnit.Centimeters)
                {
                    var unitExponent = (int) widthProperty.unitExponent;
                    if (unitExponent < -8 || unitExponent > 7)
                    {
                        unitExponent = -2;
                    }

                    var widthPhysical = widthScale * (widthProperty.physicalMax - widthProperty.physicalMin) * Math.Pow(10, unitExponent);
                    var heightPhysical = heightScale * (heightProperty.physicalMax - heightProperty.physicalMin) * Math.Pow(10, unitExponent);

                    rawPointerPoint = rawPointerPoint with
                    {
                        PhysicalWidth = widthPhysical,
                        PhysicalHeight = heightPhysical,
                    };
                }
            }

            // 换成取最后一个点好了
            //if (rawPointerPoint != default)
            //{
            //    // 默认调试只取一个点好了
            //    break;
            //}
        }

        touchInfo.AppendLine($"PointerPoint PointerId={pointerInfo.pointerId} XY={pointerInfo.ptPixelLocationRaw.X},{pointerInfo.ptPixelLocationRaw.Y} rc ContactXY={info.rcContactRaw.X},{info.rcContactRaw.Y} ContactWH={info.rcContactRaw.Width},{info.rcContactRaw.Height}");
        touchInfo.AppendLine($"RawPointerPoint Id={rawPointerPoint.Id} XY={rawPointerPoint.X:0.00},{rawPointerPoint.Y:0.00} PixelWH={rawPointerPoint.PixelWidth:0.00},{rawPointerPoint.PixelHeight:0.00} PhysicalWH={rawPointerPoint.PhysicalWidth:0.00},{rawPointerPoint.PhysicalHeight:0.00}cm HistoryCount={historyCount}");

        rawPointerPoint = rawPointerPoint with
        {
            Info = touchInfo.ToString()
        };

        return rawPointerPoint;

        static string RectToWHString(global::Windows.Win32.Foundation.RECT rect)
        {
            return $"[WH:{rect.Width},{rect.Height}]";
        }

        static string RectToString(global::Windows.Win32.Foundation.RECT rect)
        {
            return $"[XY:{rect.left},{rect.top};WH:{rect.Width},{rect.Height}]";
        }
    }
}