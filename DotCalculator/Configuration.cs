﻿using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Numerics;

namespace DotCalculator;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    
    // General
    public bool Enabled;
    public bool ShowSelfDebuffsOnEnemies;
    public bool ShowDebuffsOnSelf;
    public bool ShowDebuffsOnOthers;
    public bool HidePermanentStatuses;
    public int UpdateIntervalMillis;

    // NodeGroup
    public int MaximumStatuses;
    public int GroupX;
    public int GroupY;
    public int NodeSpacing;
    public float Scale;
    public bool FillFromRight;

    // Node
    public int IconX;
    public int IconY;
    public int IconWidth;
    public int IconHeight;
    public int DurationX;
    public int DurationY;
    public int FontSize;
    public int DurationPadding;
    public Vector4 DurationTextColor;
    public Vector4 DurationEdgeColor;
    
    // FlyText
    public bool FlyTextEnabled = true;

    public Configuration()
    {
        SetToDefaults();
    }

    public void SetToDefaults()
    {
        // General
        Enabled = true;
        ShowSelfDebuffsOnEnemies = true;
        ShowDebuffsOnSelf = false;
        ShowDebuffsOnOthers = false;
        HidePermanentStatuses = true;
        UpdateIntervalMillis = 100;

        // NodeGroup
        MaximumStatuses = 1;
        GroupX = 27;
        GroupY = 30;
        NodeSpacing = 3;
        Scale = 1;
        FillFromRight = false;

        // Node
        IconX = 0;
        IconY = 0;
        IconWidth = 24;
        IconHeight = 32;
        DurationX = 0;
        DurationY = 23;
        FontSize = 14;
        DurationPadding = 2;
        DurationTextColor = new Vector4(1, 1, 1, 1);
        DurationEdgeColor = new Vector4(0, 0, 0, 1);
    }
    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
