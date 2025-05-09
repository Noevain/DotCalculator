﻿using System.Collections.Generic;
using System.ComponentModel.Design;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DotCalculator.Windows;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
namespace DotCalculator;

public sealed class Plugin : IDalamudPlugin
{
    
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    private const string CommandName = "/dotcalculator";

    public Configuration Config { get; init; }

    public readonly WindowSystem WindowSystem = new("DotCalculator");
    internal ConfigWindow ConfigWindow { get; init; }
    public Calculator calculator { get; init; } = null!;
    
    public NameplateHandler nameplateHandler { get; init; } = null!;
    internal bool InPvp;

    internal ScreenLogHooks screenLogHooks { get; }
    
    public Plugin()
    {
        Service.Initialize(PluginInterface);
        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);
        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Display config options for DotCalculator"
        });
        screenLogHooks = new ScreenLogHooks(this);
        calculator = new Calculator(this);
        nameplateHandler = new NameplateHandler(this);
        Service.ClientState.TerritoryChanged += OnTerritoryChange;
        
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        
        
    }



    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        Service.ClientState.TerritoryChanged -= OnTerritoryChange;
        ConfigWindow.Dispose();
        screenLogHooks.Dispose();
        nameplateHandler.Dispose();
        Service.CommandManager.RemoveHandler(CommandName);
    }
    
    private void OnTerritoryChange(ushort e)
    {
        try
        {
            TerritoryType territory = Service.DataManager.GetExcelSheet<TerritoryType>().GetRow(e);
            InPvp = territory.IsPvpZone;
        }
        catch (KeyNotFoundException)
        {
            Service.Log.Warning("Could not get territory for current zone");
        }
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleConfigUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
