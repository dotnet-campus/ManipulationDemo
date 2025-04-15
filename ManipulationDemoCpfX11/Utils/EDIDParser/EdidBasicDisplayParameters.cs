﻿namespace ManipulationDemoCpfX11.Utils;

public readonly struct EdidBasicDisplayParameters
{
    /// <summary>
    /// See Table 3.9 - Video Input Definition
    /// </summary>
    public byte VideoInputDefinition { get; init; }

    /// <summary>
    /// 物理屏幕宽度
    /// </summary>
    public EdidInfo.Cm MonitorPhysicalWidth { get; init; }

    /// <summary>
    /// 物理屏幕高度
    /// </summary>
    public EdidInfo.Cm MonitorPhysicalHeight { get; init; }

    public byte DisplayTransferCharacteristicGamma { get; init; }
    public byte FeatureSupport { get; init; }
}