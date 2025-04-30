using System.Collections.Generic;

namespace TouchSizeAvalonia.PointerConverters;

internal static class StylusPointPropertyUnitHelper
{
    // Copy from https://github.com/dotnet/wpf

    /// <summary>
    /// Convert WM_POINTER units to WPF units
    /// </summary>
    /// <param name="pointerUnit"></param>
    /// <returns></returns>
    internal static StylusPointPropertyUnit? FromPointerUnit(uint pointerUnit)
    {
        StylusPointPropertyUnit unit = StylusPointPropertyUnit.None;

        if (_pointerUnitMap.TryGetValue(pointerUnit & UNIT_MASK, out unit))
        {
            return unit;
        }

        return (StylusPointPropertyUnit?) null;
    }

    /// <summary>
    /// Mapping for WM_POINTER based unit, taken from legacy WISP code
    /// </summary>
    private static Dictionary<uint, StylusPointPropertyUnit> _pointerUnitMap = new Dictionary<uint, StylusPointPropertyUnit>()
    {
        { 1, StylusPointPropertyUnit.Centimeters },
        { 2, StylusPointPropertyUnit.Radians },
        { 3, StylusPointPropertyUnit.Inches },
        { 4, StylusPointPropertyUnit.Degrees },
    };

    /// <summary>
    /// Mask to extract units from raw WM_POINTER data
    /// <see cref="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf"/> 
    /// </summary>
    private const uint UNIT_MASK = 0x000F;
}