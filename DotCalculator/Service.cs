using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace DotCalculator;

public class Service
{
    [PluginService]
    public static IDalamudPluginInterface Interface { get; private set; } = null!;
    [PluginService]
    public static IClientState ClientState { get; private set; } = null!;
    [PluginService]
    public static ICommandManager CommandManager { get; private set; } = null!;
    
    [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
    
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static IFlyTextGui FlyTextGui { get; private set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null!;

    public static void Initialize(IDalamudPluginInterface pluginInterface)
        => pluginInterface.Create<Service>();
}
