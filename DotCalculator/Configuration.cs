using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Numerics;
using Dalamud.Game.Gui.FlyText;

namespace DotCalculator;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    
    
    // FlyText
    public bool FlyTextEnabled = true;
    
    //Chat
    public bool PrintToChatEnabled = false;

    public Configuration()
    {
        
    }
    
    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
