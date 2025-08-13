using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static CPF.Linux.XLib;

namespace ManipulationDemoCpfX11.TouchFramework;

public record TouchInfo(int Id, double X, double Y, double TouchMajor, double TouchMinor, TouchStatus TouchStatus)
{
    public TouchSize GetTouchSize(XIValuatorManager valuatorManager, int edidPhysicalWidth, int edidPhysicalHeight)
    {
        var value = this;

        var display = valuatorManager.Display;
        var screen = XDefaultScreen(display);
        var xDisplayWidth = XDisplayWidth(display, screen);
        var xDisplayHeight = XDisplayHeight(display, screen);

        double pixelWidth = 0;
        double pixelHeight = 0;
        double physicalWidthValue = double.NaN;
        double physicalHeightValue = double.NaN;

        if (valuatorManager.TouchMajorValuatorClassInfo is not null)
        {
            var touchMajorScale = value.TouchMajor / valuatorManager.TouchMajorValuatorClassInfo.Value.Max;
            pixelWidth = touchMajorScale * xDisplayWidth;

            if (edidPhysicalWidth > 0)
            {
                physicalWidthValue = touchMajorScale * edidPhysicalWidth;
            }
        }

        if (valuatorManager.TouchMinorValuatorClassInfo is null)
        {
            pixelHeight = pixelWidth;
        }
        else
        {
            var touchMinorScale = value.TouchMinor / valuatorManager.TouchMinorValuatorClassInfo.Value.Max;
            pixelHeight = touchMinorScale * xDisplayHeight;

            if (edidPhysicalHeight > 0)
            {
                physicalHeightValue = touchMinorScale * edidPhysicalHeight;
            }
        }

        return new TouchSize(pixelWidth, pixelHeight, physicalWidthValue, physicalHeightValue);
    }
}

public readonly record struct TouchSize(
    double PixelWidth,
    double PixelHeight,
    double PhysicalCentimeterWidth,
    double PhysicalCentimeterHeight)
{
}

public enum TouchStatus
{
    Down,
    Move,
    Up,
}