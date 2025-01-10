﻿using System.Collections.Generic;
using System.ComponentModel.Design;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DotCalculator.StatusNode;
using DotCalculator.Windows;
using Lumina.Excel.Sheets;
namespace DotCalculator;

public sealed class Plugin : IDalamudPlugin
{
    
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    private const string CommandName = "/pmycommand";

    public Configuration Config { get; init; }

    public readonly WindowSystem WindowSystem = new("DotCalculator");
    internal ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    
    public StatusNodeManager StatusNodeManager { get; private set; } = null!;
    public static AddonNamePlateHooks Hooks { get; private set; } = null!;
    
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
            HelpMessage = "A useful message to display in /xlhelp"
        });
        screenLogHooks = new ScreenLogHooks(this);
        
        StatusNodeManager = new StatusNodeManager(this);
        
        Hooks = new AddonNamePlateHooks(this);
        Service.ClientState.TerritoryChanged += OnTerritoryChange;
        
        
        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        Service.ClientState.TerritoryChanged -= OnTerritoryChange;
        
        ConfigWindow.Dispose();
        screenLogHooks.Dispose();
        //MainWindow.Dispose();
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
        ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
