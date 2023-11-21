using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HRESULT = System.Int32;

namespace ManipulationDemo;

[ComImport, Guid("764DE8AA-1867-47C1-8F6A-122445ABD89A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ITabletManager
{
    int GetDefaultTablet(out ITablet ppTablet);
    int GetTabletCount(out ulong pcTablets);
    int GetTablet(ulong iTablet, out ITablet ppTablet);
}

[ComImport, Guid("1CB2EFC3-ABC7-4172-8FCB-3BC9CB93E29F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ITablet //: IUnknown
{
    HRESULT GetDefaultContextSettings();
    HRESULT CreateContext();
    HRESULT GetName([MarshalAs(UnmanagedType.LPWStr)] out string ppwszName);
    //HRESULT GetMaxInputRect(out RECT prcInput);
    //HRESULT GetHardwareCaps(out uint pdwCaps);
    //HRESULT GetPropertyMetrics([In] ref Guid rguid, out PROPERTY_METRICS pPM);
    //HRESULT GetPlugAndPlayId([MarshalAs(UnmanagedType.LPWStr)] out string ppwszPPId);
    //HRESULT GetCursorCount(out uint pcCurs);
    // HRESULT GetCursor(uint iCur, out ITabletCursor ppCur);
}
